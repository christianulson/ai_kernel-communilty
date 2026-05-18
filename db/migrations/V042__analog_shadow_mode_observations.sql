CREATE TABLE IF NOT EXISTS analog_shadow_observations (
    signal_id VARCHAR(64) NOT NULL,
    channel_id VARCHAR(64) NOT NULL,
    digital_gate_score REAL NOT NULL,
    digital_should_process INTEGER NOT NULL,
    analog_risk REAL NOT NULL,
    analog_uncertainty REAL NOT NULL,
    analog_utility REAL NOT NULL,
    analog_confidence REAL NOT NULL,
    analog_decision VARCHAR(16) NOT NULL,
    analog_composite_score REAL NOT NULL,
    analog_should_process INTEGER NOT NULL,
    score_delta REAL NOT NULL,
    decision_diverged INTEGER NOT NULL,
    reasons_json TEXT NOT NULL,
    observed_at DATETIME(6) NOT NULL);
CREATE INDEX idx_analog_shadow_observations_observed_at ON analog_shadow_observations(observed_at);
CREATE INDEX idx_analog_shadow_observations_channel ON analog_shadow_observations(channel_id);
CREATE INDEX idx_analog_shadow_observations_decision ON analog_shadow_observations(analog_decision);
