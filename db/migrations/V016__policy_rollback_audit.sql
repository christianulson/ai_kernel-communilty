CREATE TABLE kernel_policy_rollbacks (
  rollback_id INTEGER PRIMARY KEY AUTOINCREMENT,
  domain VARCHAR(32) NOT NULL,
  action_type VARCHAR(64) NOT NULL,
  scenario_code VARCHAR(64) NOT NULL,
  target_version BIGINT NOT NULL,
  performed_by VARCHAR(120) NOT NULL,
  reason VARCHAR(500) NULL,
  created_at DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP);
CREATE INDEX idx_policy_rollbacks_key ON kernel_policy_rollbacks(domain, action_type, scenario_code, rollback_id);
