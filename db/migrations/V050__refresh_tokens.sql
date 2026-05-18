CREATE TABLE IF NOT EXISTS refresh_tokens (
    token_id    VARCHAR(64)     NOT NULL PRIMARY KEY,
    user_id     VARCHAR(128)    NOT NULL,
    email       VARCHAR(256)    NOT NULL,
    expires_at  DATETIME(6)     NOT NULL,
    is_consumed INTEGER      NOT NULL DEFAULT 0,
    created_at  DATETIME(6)     NOT NULL DEFAULT CURRENT_TIMESTAMP);
CREATE INDEX idx_refresh_user ON refresh_tokens(user_id);
CREATE INDEX idx_refresh_expires ON refresh_tokens(expires_at);
