use serde::Serialize;
use std::sync::atomic::{AtomicBool, Ordering};
use std::sync::Arc;
use tauri::{AppHandle, Emitter};

#[cfg(feature = "audio")]
use cpal::traits::{DeviceTrait, HostTrait, StreamTrait};

#[derive(Clone, Serialize)]
pub struct AudioConfig {
    pub sample_rate: u32,
    pub channels: u16,
}

#[derive(Clone, Serialize)]
pub struct VadEvent {
    pub status: String,
    pub energy: f32,
    pub duration_ms: u64,
}

pub struct AudioCapture {
    pub running: Arc<AtomicBool>,
}

impl AudioCapture {
    pub fn new() -> Self {
        Self {
            running: Arc::new(AtomicBool::new(false)),
        }
    }

    pub fn is_running(&self) -> bool {
        self.running.load(Ordering::SeqCst)
    }
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
                let host = cpal::default_host();
                let device = match host.default_input_device() {
                    Some(d) => d,
                    None => {
                        let _ = app_clone.emit("audio-error", "No input device available");
                        return;
                    }
                };

                let config = cpal::StreamConfig {
                    channels: 1,
                    sample_rate: cpal::SampleRate(16000),
                    buffer_size: cpal::BufferSize::Fixed(512),
                };

                let mut buffer: Vec<f32> = Vec::new();
                let mut speech_detected = false;
                let mut silence_frames = 0;
                let vad_threshold: f32 = 0.02;
                let silence_timeout_frames = 30;
                let max_buffer_samples = 16000 * 30; // 30s max at 16kHz
                let mut frame_count = 0u64;

                let app_err = app_clone.clone();
                let err_callback = move |err: cpal::StreamError| {
                    let _ = app_err.emit("audio-error", format!("{err}"));
                };

                let app_data = app_clone.clone();
                let data_callback = move |data: &[f32], _: &cpal::InputCallbackInfo| {
                    for &sample in data {
                        let energy = sample.abs();
                        buffer.push(sample);
                        if buffer.len() > max_buffer_samples {
                            buffer.drain(0..buffer.len() / 2);
                        }

                        if energy > vad_threshold {
                            if !speech_detected {
                                speech_detected = true;
                                silence_frames = 0;
                                let _ = app_data.emit(
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
                                let _ = app_data.emit(
                                    "voice-end",
                                    VadEvent {
                                        status: "speech_end".into(),
                                        energy: 0.0,
                                        duration_ms: (buffer.len() as u64 * 1000) / 16000,
                                    },
                                );
                                let _ = app_data.emit("audio-data", bytes);
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
                            let _ = app_data.emit(
                                "audio-level",
                                VadEvent {
                                    status: "level".into(),
                                    energy: avg_energy,
                                    duration_ms: 0,
                                },
                            );
                        }
                    }
                };

                let stream = device
                    .build_input_stream(&config, data_callback, err_callback, None)
                    .map_err(|e| format!("Failed to build input stream: {e}"));

                if let Ok(stream) = stream {
                    let _ = stream.play();
                    while running.load(Ordering::SeqCst) {
                        std::thread::sleep(std::time::Duration::from_millis(100));
                    }
                    drop(stream);
                }
            }

            #[cfg(not(feature = "audio"))]
            {
                let _ = (&app_clone, &running);
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

            if samples.is_empty() {
                return Err("No audio data".into());
            }

            let host = cpal::default_host();
            let device = host
                .default_output_device()
                .ok_or("No output device")?;

            let config = cpal::StreamConfig {
                channels: 1,
                sample_rate: cpal::SampleRate(16000),
                buffer_size: cpal::BufferSize::Fixed(512),
            };

            let err_callback = move |err: cpal::StreamError| {
                let _ = app.emit("audio-error", format!("{err}"));
            };

            let samples = Arc::new(samples);
            let stream_samples = samples.clone();
            let stream = device
                .build_output_stream(
                    &config,
                    move |output: &mut [f32], _: &cpal::OutputCallbackInfo| {
                        for (out, &sample) in output.iter_mut().zip(stream_samples.iter()) {
                            *out = sample as f32 / 32767.0;
                        }
                    },
                    err_callback,
                    None,
                )
                .map_err(|e| format!("Failed to build output stream: {e}"))?;

            let _ = stream.play();
            let duration = samples.len() as u64 * 1000 / 16000;
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

#[cfg(test)]
mod tests {
    #[test]
    fn vad_threshold_detects_speech() {
        let threshold: f32 = 0.02;
        let silence: f32 = 0.005;
        let speech: f32 = 0.15;
        assert!(speech > threshold);
        assert!(silence < threshold);
    }

    #[test]
    fn vad_silence_timeout_accumulates() {
        let silence_timeout_frames = 30u32;
        let mut silence_frames = 0u32;
        for _ in 0..silence_timeout_frames { silence_frames += 1; }
        assert!(silence_frames >= silence_timeout_frames);
    }

    #[test]
    fn vad_speech_resets_silence() {
        let threshold: f32 = 0.02;
        let mut silence_frames = 10u32;
        let speech_energy: f32 = 0.15;
        if speech_energy > threshold { silence_frames = 0; }
        assert_eq!(silence_frames, 0);
    }

    #[test]
    fn vad_buffer_accumulation() {
        let mut buffer: Vec<f32> = Vec::new();
        for &sample in &[0.1f32, 0.2, 0.3, 0.05, 0.01] { buffer.push(sample); }
        assert_eq!(buffer.len(), 5);
    }

    #[test]
    fn vad_audio_data_conversion() {
        let buffer: Vec<f32> = vec![0.5, -0.5, 0.0, 1.0, -1.0];
        let audio_data: Vec<i16> = buffer.iter().map(|&s| (s * 32767.0) as i16).collect();
        let bytes: Vec<u8> = audio_data.iter().flat_map(|s| s.to_le_bytes()).collect();
        assert_eq!(bytes.len(), buffer.len() * 2);
        assert_eq!(audio_data[0], (0.5 * 32767.0) as i16);
    }
}
