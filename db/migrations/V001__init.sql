CREATE TABLE kernel_facts (
  fact_id VARCHAR(64) PRIMARY KEY,
  domain VARCHAR(32) NOT NULL,
  ts DATETIME NOT NULL,
  source VARCHAR(64) NOT NULL,
  type VARCHAR(64) NOT NULL,
  tags_json TEXT NOT NULL,
  metrics_json TEXT NOT NULL,
  raw_json TEXT NULL);
CREATE INDEX ix_facts_domain_ts ON kernel_facts(domain, ts);

CREATE TABLE kernel_signals (
  signal_id VARCHAR(64) PRIMARY KEY,
  domain VARCHAR(32) NOT NULL,
  window_start DATETIME NOT NULL,
  window_end DATETIME NOT NULL,
  name VARCHAR(64) NOT NULL,
  value REAL NOT NULL,
  baseline REAL NOT NULL,
  delta REAL NOT NULL,
  zscore REAL NULL);
CREATE INDEX ix_signals ON kernel_signals(domain, name, window_end);

CREATE TABLE kernel_decisions (
  decision_id VARCHAR(64) PRIMARY KEY,
  domain VARCHAR(32) NOT NULL,
  ts DATETIME NOT NULL,
  severity VARCHAR(16) NOT NULL,
  summary TEXT NOT NULL,
  action_type VARCHAR(64) NOT NULL,
  params_json TEXT NOT NULL,
  risk_score REAL NOT NULL,
  confidence REAL NOT NULL,
  status VARCHAR(16) NOT NULL,
  evidence_fact_ids TEXT NOT NULL,
  evidence_signal_ids TEXT NOT NULL,
  hypothesis_ids TEXT NOT NULL);
CREATE INDEX ix_decisions_domain_ts ON kernel_decisions(domain, ts);
CREATE INDEX ix_decisions_status ON kernel_decisions(status);

CREATE TABLE kernel_actions (
  action_id VARCHAR(64) PRIMARY KEY,
  decision_id VARCHAR(64) NOT NULL,
  ts DATETIME NOT NULL,
  requested_by_user_id VARCHAR(64) NOT NULL,
  status VARCHAR(16) NOT NULL,
  error TEXT NULL);
CREATE INDEX ix_actions_decision ON kernel_actions(decision_id);

CREATE TABLE kernel_outcomes (
  outcome_id VARCHAR(64) PRIMARY KEY,
  decision_id VARCHAR(64) NOT NULL,
  measured_at DATETIME NOT NULL,
  status VARCHAR(16) NOT NULL,
  metrics_before TEXT NOT NULL,
  metrics_after TEXT NOT NULL);
CREATE INDEX ix_outcomes_decision ON kernel_outcomes(decision_id);

CREATE TABLE kernel_policies (
  domain VARCHAR(32) NOT NULL,
  action_type VARCHAR(64) NOT NULL,
  scenario_code VARCHAR(64) NOT NULL,
  success_rate REAL NOT NULL,
  samples INT NOT NULL,
  PRIMARY KEY(domain, action_type, scenario_code)
);
