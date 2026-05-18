CREATE TABLE IF NOT EXISTS neural_signal_observations (
    observation_id VARCHAR(96) NOT NULL,
    correlation_id VARCHAR(96) NOT NULL,
    source_event_id VARCHAR(96) NOT NULL,
    model_id VARCHAR(96) NOT NULL,
    model_version VARCHAR(96) NOT NULL,
    schema_version VARCHAR(96) NOT NULL,
    channel_id VARCHAR(64) NOT NULL,
    latency_ms REAL NOT NULL,
    signals_json TEXT NOT NULL,
    explanation_codes_json TEXT NOT NULL,
    inference_created_at DATETIME(6) NOT NULL,
    observed_at DATETIME(6) NOT NULL);
CREATE INDEX idx_neural_signal_observations_observed_at ON neural_signal_observations(observed_at);
CREATE INDEX idx_neural_signal_observations_model ON neural_signal_observations(model_id, model_version);
CREATE INDEX idx_neural_signal_observations_channel ON neural_signal_observations(channel_id);
CREATE INDEX idx_neural_signal_observations_source ON neural_signal_observations(source_event_id);
