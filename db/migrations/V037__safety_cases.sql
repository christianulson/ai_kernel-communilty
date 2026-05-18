CREATE TABLE IF NOT EXISTS safety_cases (
    case_id VARCHAR(128) NOT NULL,
    user_id VARCHAR(128) NOT NULL,
    goal_id VARCHAR(128) NOT NULL,
    goal TEXT NOT NULL,
    status VARCHAR(64) NOT NULL,
    risk_score REAL NOT NULL DEFAULT 0,
    expected_success_probability REAL NOT NULL DEFAULT 0,
    concerns_json TEXT NOT NULL,
    safety_case_json TEXT NOT NULL,
    created_at DATETIME(6) NOT NULL);
CREATE INDEX idx_safety_cases_user_goal ON safety_cases(user_id, goal_id);
CREATE INDEX idx_safety_cases_created_at ON safety_cases(created_at);
CREATE INDEX idx_safety_cases_status ON safety_cases(status);
