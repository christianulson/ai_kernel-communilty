CREATE TABLE IF NOT EXISTS usage_records (
    id          VARCHAR(64)     NOT NULL PRIMARY KEY,
    user_id     VARCHAR(128)    NOT NULL,
    plan_id     VARCHAR(64)     NOT NULL,
    resource    VARCHAR(64)     NOT NULL,
    quantity    REAL          NOT NULL,
    recorded_at DATETIME(6)     NOT NULL);
CREATE INDEX idx_usage_user ON usage_records(user_id);
CREATE INDEX idx_usage_resource ON usage_records(resource);
CREATE INDEX idx_usage_recorded_at ON usage_records(recorded_at);
