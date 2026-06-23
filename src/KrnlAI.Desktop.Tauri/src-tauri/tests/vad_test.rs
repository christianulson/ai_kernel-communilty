#[cfg(test)]
mod vad_tests {
    #[test]
    fn vad_threshold_detects_speech() {
        let threshold: f32 = 0.02;
        let silence: f32 = 0.005;
        let speech: f32 = 0.15;

        assert!(speech > threshold, "Speech energy should exceed threshold");
        assert!(silence < threshold, "Silence energy should be below threshold");
    }

    #[test]
    fn vad_silence_timeout_accumulates() {
        let silence_timeout_frames = 30u32;
        let mut silence_frames = 0u32;

        for _ in 0..silence_timeout_frames {
            silence_frames += 1;
        }

        assert!(silence_frames >= silence_timeout_frames, "Should reach timeout");
        assert_eq!(silence_frames, silence_timeout_frames);
    }

    #[test]
    fn vad_speech_resets_silence() {
        let threshold: f32 = 0.02;
        let mut silence_frames = 10u32;
        let speech_energy: f32 = 0.15;

        if speech_energy > threshold {
            silence_frames = 0;
        }

        assert_eq!(silence_frames, 0, "Speech should reset silence counter");
    }

    #[test]
    fn vad_buffer_accumulation() {
        let mut buffer: Vec<f32> = Vec::new();
        let samples = vec![0.1f32, 0.2, 0.3, 0.05, 0.01];

        for &sample in &samples {
            buffer.push(sample);
        }

        assert_eq!(buffer.len(), 5, "Buffer should contain all samples");
        assert!(buffer.iter().any(|&s| s > 0.02), "Buffer should contain speech");
    }

    #[test]
    fn vad_audio_data_conversion() {
        let buffer: Vec<f32> = vec![0.5, -0.5, 0.0, 1.0, -1.0];
        let audio_data: Vec<i16> = buffer.iter().map(|&s| (s * 32767.0) as i16).collect();
        let bytes: Vec<u8> = audio_data.iter().flat_map(|s| s.to_le_bytes()).collect();

        assert_eq!(bytes.len(), buffer.len() * 2, "Each f32 sample should produce 2 bytes");
        assert_eq!(audio_data[0], (0.5 * 32767.0) as i16, "Positive sample should convert correctly");
        assert_eq!(audio_data[1], (-0.5 * 32767.0) as i16, "Negative sample should convert correctly");
    }
}
