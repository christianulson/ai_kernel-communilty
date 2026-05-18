-- V025: Working memory persistence
CREATE TABLE IF NOT EXISTS working_memory (
    slot_id VARCHAR(64) PRIMARY KEY,
    context_key VARCHAR(64) NOT NULL,
    content_json TEXT NOT NULL,
    relevance REAL NOT NULL DEFAULT 0.5,
    activation REAL NOT NULL DEFAULT 1.0,
    created_at DATETIME(6) NOT NULL,
    last_accessed_at DATETIME(6) NOT NULL,
    expires_at DATETIME(6) NULL);
CREATE INDEX idx_working_memory_context_key ON working_memory(context_key);
CREATE INDEX idx_working_memory_relevance ON working_memory(relevance);
CREATE INDEX idx_working_memory_last_accessed ON working_memory(last_accessed_at);
CREATE INDEX idx_working_memory_expires ON working_memory(expires_at);