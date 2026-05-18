CREATE TABLE IF NOT EXISTS user_external_credentials (
    user_id         VARCHAR(64) NOT NULL,
    service_type    VARCHAR(32) NOT NULL,
    credentials_json TEXT NOT NULL,
    enabled         INTEGER NOT NULL DEFAULT 1,
    last_used_at    DATETIME(3),
    created_at      DATETIME(3) NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at      DATETIME(3) NOT NULL DEFAULT CURRENT_TIMESTAMP ,

    PRIMARY KEY (user_id, service_type));
CREATE INDEX idx_user_id ON user_external_credentials(user_id);
CREATE INDEX idx_service_type ON user_external_credentials(service_type);
