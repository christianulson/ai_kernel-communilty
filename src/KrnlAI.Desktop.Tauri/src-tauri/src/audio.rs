use serde::{Deserialize, Serialize};
use std::sync::{
    atomic::{AtomicBool, Ordering},
    Arc, Mutex,
};
use tauri::Emitter;

#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct AudioConfig {
    pub sample_rate: u32,
    pub channels: u16,
}

pub struct AudioCapture {
    running: Arc<AtomicBool>,
    config: Mutex<Option<AudioConfig>>,
}

impl AudioCapture {
    pub fn new() -> Self {
        Self {
            running: Arc::new(AtomicBool::new(false)),
            config: Mutex::new(None),
        }
    }

    pub fn is_running(&self) -> bool {
        self.running.load(Ordering::SeqCst)
    }

    pub fn start_capture(&self, app_handle: tauri::AppHandle) -> Result<(), String> {
        #[cfg(not(feature = "audio"))]
        {
            let _ = app_handle;
            return Err("Audio feature not enabled. Rebuild with --features audio.".to_string());
        }

        #[cfg(feature = "audio")]
        self.start_capture_impl(app_handle)
    }

    pub fn stop_capture(&self) -> Result<(), String> {
        if !self.running.load(Ordering::SeqCst) {
            return Err("Audio capture is not running".to_string());
        }
        self.running.store(false, Ordering::SeqCst);
        Ok(())
    }

    pub fn play_audio(&self, data: Vec<u8>) -> Result<(), String> {
        #[cfg(not(feature = "audio"))]
        {
            let _ = data;
            return Err("Audio feature not enabled. Rebuild with --features audio.".to_string());
        }

        #[cfg(feature = "audio")]
        self.play_audio_impl(data)
    }
}

// ── Real implementation behind `audio` feature ────────────────────────────────

#[cfg(feature = "audio")]
impl AudioCapture {
    fn start_capture_impl(&self, app_handle: tauri::AppHandle) -> Result<(), String> {
        use cpal::traits::{DeviceTrait, HostTrait, StreamTrait};

        let host = cpal::default_host();
        let device = host
            .default_input_device()
            .ok_or_else(|| "No audio input device available".to_string())?;

        let config = device
            .default_input_config()
            .map_err(|e| format!("Failed to get default input config: {}", e))?;

        let sample_rate = config.sample_rate().0;
        let channels = config.channels();

        *self.config.lock().unwrap() = Some(AudioConfig {
            sample_rate,
            channels,
        });

        let _ = app_handle.emit(
            "audio-config",
            AudioConfig {
                sample_rate,
                channels,
            },
        );

        self.running.store(true, Ordering::SeqCst);
        let running_outer = self.running.clone();

        let err_fn = |err: cpal::StreamError| {
            eprintln!("Audio capture stream error: {}", err);
        };

        // Spawn a thread where the stream will live (cpal::Stream is not Send on Windows)
        std::thread::spawn(move || {
            let running = running_outer.clone();
            let running_f32 = running_outer.clone();
            let running_i16 = running_outer.clone();
            let running_u16 = running_outer.clone();
            let app_f32 = app_handle.clone();
            let app_i16 = app_handle.clone();
            let app_u16 = app_handle.clone();

            let stream_result = match config.sample_format() {
                cpal::SampleFormat::F32 => device.build_input_stream(
                    &config.config(),
                    move |data: &[f32], _: &cpal::InputCallbackInfo| {
                        if !running_f32.load(Ordering::SeqCst) {
                            return;
                        }
                        let mut bytes = Vec::with_capacity(data.len() * 2);
                        for &sample in data {
                            let clamped = sample.clamp(-1.0, 1.0);
                            let i16_sample = (clamped * 32767.0) as i16;
                            bytes.extend_from_slice(&i16_sample.to_le_bytes());
                        }
                        let _ = app_f32.emit("audio-data", &bytes);
                    },
                    err_fn,
                    None,
                ),
                cpal::SampleFormat::I16 => device.build_input_stream(
                    &config.config(),
                    move |data: &[i16], _: &cpal::InputCallbackInfo| {
                        if !running_i16.load(Ordering::SeqCst) {
                            return;
                        }
                        let mut bytes = Vec::with_capacity(data.len() * 2);
                        for &sample in data {
                            bytes.extend_from_slice(&sample.to_le_bytes());
                        }
                        let _ = app_i16.emit("audio-data", &bytes);
                    },
                    err_fn,
                    None,
                ),
                cpal::SampleFormat::U16 => device.build_input_stream(
                    &config.config(),
                    move |data: &[u16], _: &cpal::InputCallbackInfo| {
                        if !running_u16.load(Ordering::SeqCst) {
                            return;
                        }
                        let mut bytes = Vec::with_capacity(data.len() * 2);
                        for &sample in data {
                            let i16_sample = (sample as i32 - 32768) as i16;
                            bytes.extend_from_slice(&i16_sample.to_le_bytes());
                        }
                        let _ = app_u16.emit("audio-data", &bytes);
                    },
                    err_fn,
                    None,
                ),
                _ => {
                    eprintln!("Unsupported input sample format: {:?}", config.sample_format());
                    return;
                }
            };

            match stream_result {
                Ok(stream) => {
                    if let Err(e) = stream.play() {
                        eprintln!("Failed to start audio stream: {}", e);
                        return;
                    }
                    // Keep stream alive on this thread until stopped
                    while running.load(Ordering::SeqCst) {
                        std::thread::sleep(std::time::Duration::from_millis(100));
                    }
                    // Stream is dropped here
                }
                Err(e) => {
                    eprintln!("Failed to create audio input stream: {}", e);
                }
            }
        });

        Ok(())
    }

    fn play_audio_impl(&self, data: Vec<u8>) -> Result<(), String> {
        use cpal::traits::{DeviceTrait, HostTrait, StreamTrait};

        if data.len() % 2 != 0 {
            return Err("Audio data length must be even (16-bit samples)".to_string());
        }

        let host = cpal::default_host();
        let device = host
            .default_output_device()
            .ok_or_else(|| "No audio output device available".to_string())?;

        let config = device
            .default_output_config()
            .map_err(|e| format!("Failed to get default output config: {}", e))?;

        let samples: Vec<i16> = data
            .chunks_exact(2)
            .map(|c| i16::from_le_bytes([c[0], c[1]]))
            .collect();

        let sample_count = samples.len();
        let cursor = Arc::new(Mutex::new(0usize));

        let err_fn = |err: cpal::StreamError| {
            eprintln!("Audio playback stream error: {}", err);
        };

        let stream = match config.sample_format() {
            cpal::SampleFormat::F32 => {
                let cursor = cursor.clone();
                device.build_output_stream(
                    &config.config(),
                    move |output: &mut [f32], _: &cpal::OutputCallbackInfo| {
                        let mut pos = cursor.lock().unwrap();
                        for sample in output.iter_mut() {
                            if *pos < sample_count {
                                *sample = samples[*pos] as f32 / 32767.0;
                                *pos += 1;
                            } else {
                                *sample = 0.0;
                            }
                        }
                    },
                    err_fn,
                    None,
                )
            }
            cpal::SampleFormat::I16 => {
                let cursor = cursor.clone();
                device.build_output_stream(
                    &config.config(),
                    move |output: &mut [i16], _: &cpal::OutputCallbackInfo| {
                        let mut pos = cursor.lock().unwrap();
                        let to_copy = output.len().min(sample_count.saturating_sub(*pos));
                        if to_copy > 0 {
                            output[..to_copy].copy_from_slice(&samples[*pos..*pos + to_copy]);
                            *pos += to_copy;
                        }
                        for sample in output[to_copy..].iter_mut() {
                            *sample = 0;
                        }
                    },
                    err_fn,
                    None,
                )
            }
            cpal::SampleFormat::U16 => {
                let cursor = cursor.clone();
                device.build_output_stream(
                    &config.config(),
                    move |output: &mut [u16], _: &cpal::OutputCallbackInfo| {
                        let mut pos = cursor.lock().unwrap();
                        for sample in output.iter_mut() {
                            if *pos < sample_count {
                                *sample = (samples[*pos] as i32 + 32768) as u16;
                                *pos += 1;
                            } else {
                                *sample = 32768;
                            }
                        }
                    },
                    err_fn,
                    None,
                )
            }
            _ => return Err(format!("Unsupported output sample format: {:?}", config.sample_format())),
        }
        .map_err(|e| format!("Failed to create audio output stream: {}", e))?;

        stream
            .play()
            .map_err(|e| format!("Failed to start playback stream: {}", e))?;

        let playback_ms = if config.sample_rate().0 > 0 {
            let sample_rate_hz = config.sample_rate().0 as u64;
            let channels = config.channels() as u64;
            (sample_count as u64 * 1000) / (sample_rate_hz * channels)
        } else {
            100
        };

        // Block on this thread to keep the stream alive during playback.
        // The stream is dropped when the function returns.
        std::thread::sleep(std::time::Duration::from_millis(playback_ms + 50));

        Ok(())
    }
}

impl Drop for AudioCapture {
    fn drop(&mut self) {
        self.running.store(false, Ordering::SeqCst);
    }
}
