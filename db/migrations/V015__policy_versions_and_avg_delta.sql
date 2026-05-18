ALTER TABLE kernel_policies
  ADD COLUMN avg_delta_improvement REAL NOT NULL DEFAULT 0;

CREATE TABLE kernel_policy_versions (
  domain VARCHAR(32) NOT NULL,
  action_type VARCHAR(64) NOT NULL,
  scenario_code VARCHAR(64) NOT NULL,
  version_no INTEGER PRIMARY KEY AUTOINCREMENT,
  success_rate REAL NOT NULL,
  avg_delta_improvement REAL NOT NULL,
  samples INT NOT NULL,
  created_at DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP);
CREATE INDEX idx_policy_versions_key ON kernel_policy_versions(domain, action_type, scenario_code, version_no);
