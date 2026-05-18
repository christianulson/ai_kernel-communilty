CREATE TABLE IF NOT EXISTS learning_journal (
  journal_id VARCHAR(128) NOT NULL,
  entry_type VARCHAR(64) NOT NULL,
  episode_id VARCHAR(128) NULL,
  policy_domain VARCHAR(64) NULL,
  action_type VARCHAR(128) NULL,
  scenario_code VARCHAR(128) NULL,
  evidence_ids_json TEXT NOT NULL,
  payload_json TEXT NOT NULL,
  recorded_at DATETIME NOT NULL);
CREATE INDEX idx_learning_journal_recorded_at ON learning_journal(recorded_at);
CREATE INDEX idx_learning_journal_episode ON learning_journal(episode_id);
