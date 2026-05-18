-- V071: Cognitive snapshots for full-state rollback (Plano 3 - OpenCode)

CREATE TABLE IF NOT EXISTS cognitive_snapshots_v2 (
    snapshot_id VARCHAR(64) PRIMARY KEY,
    label VARCHAR(256) NOT NULL,
    scope INT NOT NULL,
    created_at DATETIME(3) NOT NULL,
    reason VARCHAR(128) NOT NULL,
    component_list TEXT NOT NULL,
    metadata_json TEXT,
    purge_after DATETIME(3));
CREATE INDEX idx_snapshots_created ON cognitive_snapshots_v2(created_at);
CREATE INDEX idx_snapshots_reason ON cognitive_snapshots_v2(reason);

CREATE TABLE IF NOT EXISTS snapshot_components (
    component_id VARCHAR(64) PRIMARY KEY,
    snapshot_id VARCHAR(64) NOT NULL,
    component_name VARCHAR(64) NOT NULL,
    content_json TEXT NOT NULL,
    content_type VARCHAR(64) NOT NULL,
    captured_at DATETIME(3) NOT NULL,
    CONSTRAINT fk_snapcomp_snapshot
        FOREIGN KEY (snapshot_id)
        REFERENCES cognitive_snapshots_v2(snapshot_id)
        ON DELETE CASCADE
);
CREATE INDEX idx_snapcomp_snapshot ON snapshot_components(snapshot_id);
