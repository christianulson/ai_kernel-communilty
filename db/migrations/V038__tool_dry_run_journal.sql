CREATE TABLE IF NOT EXISTS tool_dry_run_journal (
    record_id VARCHAR(80) NOT NULL,
    goal TEXT NOT NULL,
    step_index INT NOT NULL DEFAULT 0,
    tool_name VARCHAR(128) NOT NULL,
    risk VARCHAR(32) NOT NULL,
    fingerprint VARCHAR(128) NOT NULL,
    success INTEGER NOT NULL DEFAULT 0,
    error VARCHAR(128) NULL,
    contract_version VARCHAR(64) NULL,
    recorded_at DATETIME(6) NOT NULL);
CREATE INDEX idx_tool_dry_run_tool_recorded ON tool_dry_run_journal(tool_name, recorded_at);
CREATE INDEX idx_tool_dry_run_success ON tool_dry_run_journal(success);
