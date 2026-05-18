CREATE TABLE IF NOT EXISTS episodic_memories_v2 (
    id BLOB PRIMARY KEY,
    tenant_id BLOB NOT NULL,
    user_id BLOB,
    agent_id BLOB,
    content TEXT NOT NULL,
    embedding_json TEXT,
    entities TEXT,
    keywords TEXT,
    timestamp_utc DATETIME(6) NOT NULL,
    expires_at DATETIME(6),
    is_deleted INTEGER DEFAULT FALSE,
    version INT DEFAULT 1
);
CREATE INDEX idx_tenant_time ON episodic_memories_v2(tenant_id, timestamp_utc);
-- CREATE INDEX idx_entities ON episodic_memories_v2((CAST(entities->'$[*].name' AS CHAR(255); (MySQL-specific CAST index, skipped for SQLite)
