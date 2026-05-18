CREATE TABLE IF NOT EXISTS cognitive_cycle_journal (
  cycle_id VARCHAR(64) NOT NULL,
  status VARCHAR(32) NOT NULL,
  completed_at DATETIME NULL,
  learned INTEGER NOT NULL DEFAULT 0,
  risk_score REAL NOT NULL DEFAULT 0,
  utility_score REAL NOT NULL DEFAULT 0,
  steps_json TEXT NOT NULL,
  stop_reasons_json TEXT NOT NULL,
  metadata_json TEXT NOT NULL);
CREATE INDEX idx_cognitive_cycle_completed_at ON cognitive_cycle_journal(completed_at);
CREATE INDEX idx_cognitive_cycle_status ON cognitive_cycle_journal(status);
