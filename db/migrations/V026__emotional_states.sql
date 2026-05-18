-- V026: Emotional state persistence (Valence-Arousal-Dominance model)
CREATE TABLE IF NOT EXISTS emotional_states (
    user_id VARCHAR(64) PRIMARY KEY,
    valence REAL NOT NULL DEFAULT 0.0,
    arousal REAL NOT NULL DEFAULT 0.2,
    motivation REAL NOT NULL DEFAULT 0.5,
    updated_at DATETIME(6) NOT NULL);
CREATE INDEX idx_emotional_states_updated ON emotional_states(updated_at);