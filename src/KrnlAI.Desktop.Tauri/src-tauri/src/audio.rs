use serde::Serialize;
use std::sync::atomic::{AtomicBool, Ordering};
use std::sync::{Arc, Mutex};
use tauri::{AppHandle, Emitter};

#[derive(Clone, Serialize)]
pub struct AudioConfig {
    pub sample_rate: u32,
    pub channels: u16,
}

pub struct AudioCapture {
    pub running: Arc<AtomicBool>,
    pub config: Mutex<Option<AudioConfig>>,
}

impl AudioCapture {
    pub fn new() -> Self {
        Self {
            running: Arc::new(AtomicBool::new(false)),
            config: Mutex::new(None),
        }
    }
}

#[derive(Clone, Serialize)]
pub struct VadEvent {
    pub status: String,
    pub energy: f32,
    pub duration_ms: u64,
}

impl AudioCapture {
    pub fn start_capture(&self, app: AppHandle) -> Result<(), String> {
        self.running.store(true, Ordering::SeqCst);

        let running = self.running.clone();
        let app_clone = app.clone();

        app_clone
            .emit("audio-config", AudioConfig {
                sample_rate: 16000,
                channels: 1,
            })
            .map_err(|e| format!("Failed to emit audio-config: {e}"))?;

        std::thread::spawn(move || {
            #[cfg(feature = "audio")]
            {
                let (_stream, stream_handle) =
                    match cpal::default_input_stream(&cpal::default_input_device().unwrap()) {
                        Ok(s) => s,
                        Err(e) => {
                            let _ = app_clone.emit("audio-error", format!("{e}"));
                            return;
                        }
                    };

                let mut buffer: Vec<f32> = Vec::new();
                let mut speech_detected = false;
                let mut silence_frames = 0;
                let vad_threshold: f32 = 0.02;
                let silence_timeout_frames = 30;
                let mut frame_count = 0u64;

                let stream = stream_handle
                    .build_input_stream(
                        &cpal::StreamConfig {
                            channels: 1,
                            sample_rate: cpal::SampleRate(16000),
                            buffer_size: cpal::BufferSize::Fixed(512),
                        },
                        move |data: &[f32], _: &cpal::InputCallbackInfo| {
                            for &sample in data {
                                let energy = sample.abs();
                                buffer.push(sample);

                                if energy > vad_threshold {
                                    if !speech_detected {
                                        speech_detected = true;
                                        silence_frames = 0;
                                        let _ = app_clone.emit(
                                            "voice-start",
                                            VadEvent {
                                                status: "speech_start".into(),
                                                energy,
                                                duration_ms: (buffer.len() as u64 * 1000) / 16000,
                                            },
                                        );
                                    }
                                    silence_frames = 0;
                                } else if speech_detected {
                                    silence_frames += 1;
                                    if silence_frames >= silence_timeout_frames {
                                        let audio_data: Vec<i16> = buffer
                                            .iter()
                                            .map(|&s| (s * 32767.0) as i16)
                                            .collect();
                                        let bytes: Vec<u8> = audio_data
                                            .iter()
                                            .flat_map(|s| s.to_le_bytes())
                                            .collect();
                                        let _ = app_clone.emit(
                                            "voice-end",
                                            VadEvent {
                                                status: "speech_end".into(),
                                                energy: 0.0,
                                                duration_ms: (buffer.len() as u64 * 1000) / 16000,
                                            },
                                        );
                                        let _ = app_clone.emit("audio-data", bytes);
                                        buffer.clear();
                                        speech_detected = false;
                                        silence_frames = 0;
                                    }
                                }

                                frame_count += 1;
                                if frame_count % 1600 == 0 {
                                    let avg_energy: f32 =
                                        buffer.iter().rev().take(160).map(|s| s.abs()).sum::<f32>()
                                            / 160.0;
                                    let _ = app_clone.emit(
                                        "audio-level",
                                        VadEvent {
                                            status: "level".into(),
                                            energy: avg_energy,
                                            duration_ms: 0,
                                        },
                                    );
                                }
                            }
                        },
                        move |err| {
                            let _ = app_clone.emit("audio-error", format!("{err}"));
                        },
                        None,
                    )
                    .map_err(|e| format!("Failed to build input stream: {e}"));

                if let Ok(stream) = stream {
                    stream.play();
                    while running.load(Ordering::SeqCst) {
                        std::thread::sleep(std::time::Duration::from_millis(100));
                    }
                    drop(stream);
                }
            }

            #[cfg(not(feature = "audio"))]
            {
                let _ = &app_clone;
                let _ = &running;
            }
        });

        Ok(())
    }

    pub fn stop_capture(&self) {
        self.running.store(false, Ordering::SeqCst);
    }

    pub fn play_audio(&self, app: AppHandle, data: Vec<u8>) -> Result<(), String> {
        #[cfg(feature = "audio")]
        {
            let samples: Vec<i16> = data
                .chunks(2)
                .map(|c| i16::from_le_bytes([c[0], c[1]]))
                .collect();

            let (_stream, stream_handle) = cpal::default_output_stream(
                &cpal::default_output_device().ok_or("No output device")?,
            )
            .map_err(|e| format!("{e}"))?;

            let duration = samples.len() as u64 * 1000 / 16000;
            stream_handle
                .build_output_stream(
                    &cpal::StreamConfig {
                        channels: 1,
                        sample_rate: cpal::SampleRate(16000),
                        buffer_size: cpal::BufferSize::Fixed(512),
                    },
                    move |output: &mut [f32], _: &cpal::OutputCallbackInfo| {
                        for (out, &sample) in output.iter_mut().zip(samples.iter()) {
                            *out = sample as f32 / 32767.0;
                        }
                    },
                    move |err| {
                        let _ = app.emit("audio-error", format!("{err}"));
                    },
                    None,
                )
                .map_err(|e| format!("Failed to build output stream: {e}"))?
                .play();

            std::thread::sleep(std::time::Duration::from_millis(duration + 50));
            Ok(())
        }

        #[cfg(not(feature = "audio"))]
        {
            let _ = (app, data);
            Err("Audio feature not enabled".into())
        }
    }
}

impl Drop for AudioCapture {
    fn drop(&mut self) {
        self.running.store(false, Ordering::SeqCst);
    }
}
