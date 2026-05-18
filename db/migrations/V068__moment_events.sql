-- V068: Detected moment events (aggregations of related cognitive moments)
CREATE TABLE IF NOT EXISTS moment_events (
    event_id VARCHAR(64) PRIMARY KEY,
    type VARCHAR(32) NOT NULL,
    summary VARCHAR(512),
    moment_ids_json TEXT NOT NULL,
    started_at DATETIME(3) NOT NULL,
    ended_at DATETIME(3) NOT NULL,
    peak_importance REAL NOT NULL,
    avg_importance REAL NOT NULL,
    active_goal_id VARCHAR(64),
    focus_domain VARCHAR(64),
    dominant_category VARCHAR(32),
    metadata_json TEXT);
CREATE INDEX idx_moment_events_started_at ON moment_events(started_at);
CREATE INDEX idx_moment_events_peak_importance ON moment_events(peak_importance);

CREATE TABLE IF NOT EXISTS moment_event_members (
    event_id VARCHAR(64),
    moment_id VARCHAR(64),
    sequence BIGINT NOT NULL,
    PRIMARY KEY (event_id, moment_id),
    CONSTRAINT fk_moment_event_members_event
        FOREIGN KEY (event_id)
        REFERENCES moment_events(event_id)
        ON DELETE CASCADE
);
CREATE INDEX idx_moment_event_members_sequence ON moment_event_members(sequence);
