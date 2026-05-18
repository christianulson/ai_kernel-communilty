-- V067: Moment classifications and importance scores (Onda 5 - Classificacao de Momento)
CREATE TABLE IF NOT EXISTS moment_classifications (
    moment_id VARCHAR(64) PRIMARY KEY,
    category VARCHAR(32) NOT NULL,
    confidence REAL NOT NULL,
    importance REAL NOT NULL,
    goal_relevance REAL NOT NULL,
    novelty_peak REAL NOT NULL,
    urgency_peak REAL NOT NULL,
    cognitive_intensity REAL NOT NULL,
    binding_richness REAL NOT NULL,
    interaction_weight REAL NOT NULL,
    anomaly_score REAL NOT NULL,
    valence_magnitude REAL NOT NULL,
    narrative_role VARCHAR(16) NOT NULL,
    tags_json TEXT,
    metadata_json TEXT,
    CONSTRAINT fk_moment_classifications_moment
        FOREIGN KEY (moment_id)
        REFERENCES moments(moment_id)
        ON DELETE CASCADE
);
CREATE INDEX idx_moment_classifications_importance ON moment_classifications(importance);
CREATE INDEX idx_moment_classifications_category ON moment_classifications(category);
CREATE INDEX idx_moment_classifications_narrative_role ON moment_classifications(narrative_role);
