-- Generic key-value store for administrative and configuration data.
-- Each row stores a serialized JSON value keyed by category + key.
-- Supports caching via Redis for frequently accessed, rarely changed data.

CREATE TABLE IF NOT EXISTS key_value_store (
    category VARCHAR(64) NOT NULL,
    store_key VARCHAR(255) NOT NULL,
    value_json TEXT NOT NULL,
    created_at DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP ,
    PRIMARY KEY (category, store_key)
);
