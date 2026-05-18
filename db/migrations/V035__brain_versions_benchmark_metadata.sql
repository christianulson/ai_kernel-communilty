CREATE TABLE IF NOT EXISTS brain_versions (
    version_id VARCHAR(64) PRIMARY KEY,
    label VARCHAR(255) NOT NULL,
    config_snapshot_json TEXT NOT NULL,
    parent_version_id VARCHAR(64) NULL,
    created_at DATETIME(6) NOT NULL,
    rollback_reason TEXT NULL);
CREATE INDEX idx_brain_versions_created_at ON brain_versions(created_at);

ALTER TABLE brain_versions
    ADD COLUMN diff_summary TEXT NULL,
    ADD COLUMN benchmark_passed INTEGER NULL,
    ADD COLUMN benchmark_score REAL NULL;
