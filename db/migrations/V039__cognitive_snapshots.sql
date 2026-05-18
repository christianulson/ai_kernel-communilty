CREATE TABLE IF NOT EXISTS cognitive_snapshots (
    snapshot_id VARCHAR(128) NOT NULL,
    version_id VARCHAR(64) NOT NULL,
    captured_at DATETIME(6) NOT NULL,
    parameter_values TEXT NOT NULL,
    metrics_snapshot TEXT NOT NULL,
    active_goal_ids TEXT NOT NULL,
    recent_episode_ids TEXT NOT NULL,
    world_model_hash VARCHAR(64) NULL,

    CONSTRAINT fk_snapshot_version
        FOREIGN KEY (version_id)
        REFERENCES brain_versions(version_id)
        ON DELETE CASCADE
);
CREATE INDEX idx_cognitive_snapshots_version ON cognitive_snapshots(version_id);
CREATE INDEX idx_cognitive_snapshots_captured_at ON cognitive_snapshots(captured_at);
