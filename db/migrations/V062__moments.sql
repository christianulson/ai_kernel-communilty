-- V062: Cognitive moment snapshots (Onda 5 - Memoria Temporal Situada)
CREATE TABLE IF NOT EXISTS moments (
    moment_id VARCHAR(64) PRIMARY KEY,
    sequence BIGINT NOT NULL UNIQUE,
    started_at DATETIME(6) NOT NULL,
    ended_at DATETIME(6) NOT NULL,
    active_goal_id VARCHAR(64) NULL,
    focus_domain VARCHAR(64) NULL,
    cognitive_load REAL NOT NULL DEFAULT 0,
    arousal REAL NOT NULL DEFAULT 0,
    valence REAL NOT NULL DEFAULT 0,
    working_memory_slot_ids_json TEXT NOT NULL,
    evidence_ids_json TEXT NOT NULL,
    metadata_json TEXT NOT NULL);
CREATE INDEX idx_moments_sequence ON moments(sequence);
CREATE INDEX idx_moments_started_at ON moments(started_at);
