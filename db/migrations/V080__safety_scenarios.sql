CREATE TABLE IF NOT EXISTS safety_scenarios (
    scenario_id VARCHAR(64) NOT NULL PRIMARY KEY,
    category VARCHAR(32) NOT NULL,
    prompt TEXT NOT NULL,
    expected_behavior VARCHAR(32) NOT NULL,
    risk_level VARCHAR(16) NOT NULL,
    rules_to_test TEXT,
    created_at DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP 
);

CREATE TABLE IF NOT EXISTS safety_audit_results (
    audit_id VARCHAR(64) NOT NULL PRIMARY KEY,
    config_id VARCHAR(64) NOT NULL,
    executed_at DATETIME(6) NOT NULL,
    overall_score REAL NOT NULL,
    passed INT NOT NULL,
    failed INT NOT NULL,
    total INT NOT NULL,
    details TEXT,
    compliance TEXT);
CREATE INDEX idx_config_date ON safety_audit_results(config_id, executed_at);
