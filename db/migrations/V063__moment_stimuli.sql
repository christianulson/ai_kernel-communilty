-- V063: Sensory stimuli captured inside cognitive moments
CREATE TABLE IF NOT EXISTS moment_stimuli (
    moment_id VARCHAR(64) NOT NULL,
    signal_id VARCHAR(64) NOT NULL,
    channel_id VARCHAR(64) NOT NULL,
    captured_at DATETIME(6) NOT NULL,
    offset_ms BIGINT NOT NULL DEFAULT 0,
    intensity REAL NOT NULL DEFAULT 0,
    novelty REAL NOT NULL DEFAULT 0,
    urgency REAL NOT NULL DEFAULT 0,
    salience REAL NOT NULL DEFAULT 0,
    summary TEXT NOT NULL,
    content_json TEXT NOT NULL,
    CONSTRAINT fk_moment_stimuli_moment
        FOREIGN KEY (moment_id)
        REFERENCES moments(moment_id)
        ON DELETE CASCADE
);
CREATE INDEX idx_moment_stimuli_moment ON moment_stimuli(moment_id);
CREATE INDEX idx_moment_stimuli_signal ON moment_stimuli(signal_id);
