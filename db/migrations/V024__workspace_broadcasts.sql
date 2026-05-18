-- V024: Global workspace persistence
CREATE TABLE IF NOT EXISTS workspace_broadcasts (
    broadcast_id VARCHAR(64) PRIMARY KEY,
    source VARCHAR(32) NOT NULL,
    content_type VARCHAR(64) NOT NULL,
    content_json TEXT,
    urgency REAL NOT NULL DEFAULT 0.5,
    relevance REAL NOT NULL DEFAULT 0.5,
    broadcast_at DATETIME(6) NOT NULL);
CREATE INDEX idx_workspace_broadcasts_source ON workspace_broadcasts(source);
CREATE INDEX idx_workspace_broadcasts_urgency ON workspace_broadcasts(urgency);
CREATE INDEX idx_workspace_broadcasts_relevance ON workspace_broadcasts(relevance);
CREATE INDEX idx_workspace_broadcasts_at ON workspace_broadcasts(broadcast_at);