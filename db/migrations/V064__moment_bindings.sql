-- V064: Cross-modal bindings captured inside cognitive moments
CREATE TABLE IF NOT EXISTS moment_bindings (
    binding_id VARCHAR(64) PRIMARY KEY,
    moment_id VARCHAR(64) NOT NULL,
    bound_signal_ids_json TEXT NOT NULL,
    channel_ids_json TEXT NOT NULL,
    confidence REAL NOT NULL DEFAULT 0,
    occurred_at DATETIME(6) NOT NULL,
    CONSTRAINT fk_moment_bindings_moment
        FOREIGN KEY (moment_id)
        REFERENCES moments(moment_id)
        ON DELETE CASCADE
);
CREATE INDEX idx_moment_bindings_moment ON moment_bindings(moment_id);
