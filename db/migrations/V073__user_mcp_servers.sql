CREATE TABLE IF NOT EXISTS user_mcp_servers (
    user_id         VARCHAR(64) NOT NULL,
    server_id       VARCHAR(64) NOT NULL,
    enabled         INTEGER  NOT NULL DEFAULT 1,
    custom_headers  TEXT,
    last_used_at    DATETIME(3),
    created_at      DATETIME(3) NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at      DATETIME(3) NOT NULL DEFAULT CURRENT_TIMESTAMP ,

    PRIMARY KEY (user_id, server_id));
CREATE INDEX idx_user_id ON user_mcp_servers(user_id);
CREATE INDEX idx_enabled ON user_mcp_servers(enabled);
