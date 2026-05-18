CREATE TABLE IF NOT EXISTS analog_shadow_observation_rollups (
    bucket_start_utc DATETIME(6) NOT NULL,
    channel_id VARCHAR(64) NOT NULL,
    analog_decision VARCHAR(32) NOT NULL,
    total_observations INT NOT NULL DEFAULT 0,
    divergence_count INT NOT NULL DEFAULT 0,
    digital_should_process_count INT NOT NULL DEFAULT 0,
    digital_block_count INT NOT NULL DEFAULT 0,
    digital_gate_score_sum REAL NOT NULL DEFAULT 0,
    analog_composite_score_sum REAL NOT NULL DEFAULT 0,
    score_delta_sum REAL NOT NULL DEFAULT 0,
    first_observed_at DATETIME(6) NOT NULL,
    last_observed_at DATETIME(6) NOT NULL,
    PRIMARY KEY (bucket_start_utc, channel_id, analog_decision));
CREATE INDEX idx_analog_shadow_rollups_bucket ON analog_shadow_observation_rollups(bucket_start_utc);
CREATE INDEX idx_analog_shadow_rollups_channel ON analog_shadow_observation_rollups(channel_id);
CREATE INDEX idx_analog_shadow_rollups_decision ON analog_shadow_observation_rollups(analog_decision);
