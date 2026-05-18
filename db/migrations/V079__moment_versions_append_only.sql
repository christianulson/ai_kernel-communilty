CREATE TABLE IF NOT EXISTS moment_versions (
    moment_id VARCHAR(64) NOT NULL,
    version INT NOT NULL,
    sequence BIGINT NOT NULL,
    started_at DATETIME(6) NOT NULL,
    ended_at DATETIME(6) NOT NULL,
    active_goal_id VARCHAR(64) DEFAULT NULL,
    focus_domain VARCHAR(32) DEFAULT NULL,
    cognitive_load REAL NOT NULL DEFAULT 0,
    arousal REAL NOT NULL DEFAULT 0,
    valence REAL NOT NULL DEFAULT 0,
    data_json TEXT NOT NULL,
    created_at DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (moment_id, version));
CREATE INDEX idx_created_at ON moment_versions(created_at);
CREATE INDEX idx_moment_lookup ON moment_versions(moment_id, version);
