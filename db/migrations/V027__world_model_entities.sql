-- V027: World model entities and relations
CREATE TABLE IF NOT EXISTS world_entities (
    entity_id VARCHAR(64) NOT NULL,
    user_id VARCHAR(64) NOT NULL,
    type VARCHAR(32) NOT NULL,
    label VARCHAR(256) NOT NULL,
    properties_json TEXT,
    salience REAL NOT NULL DEFAULT 0.5,
    last_observed_at DATETIME(6) NOT NULL,
    PRIMARY KEY (entity_id, user_id));
CREATE INDEX idx_world_entities_user ON world_entities(user_id);
CREATE INDEX idx_world_entities_type ON world_entities(type);
CREATE INDEX idx_world_entities_salience ON world_entities(salience);

CREATE TABLE IF NOT EXISTS world_relations (
    from_entity_id VARCHAR(64) NOT NULL,
    to_entity_id VARCHAR(64) NOT NULL,
    user_id VARCHAR(64) NOT NULL,
    relation_type VARCHAR(32) NOT NULL,
    strength REAL NOT NULL DEFAULT 0.5,
    learned_at DATETIME(6) NOT NULL,
    PRIMARY KEY (from_entity_id, to_entity_id, user_id),
    FOREIGN KEY (from_entity_id, user_id) REFERENCES world_entities(entity_id, user_id) ON DELETE CASCADE,
    FOREIGN KEY (to_entity_id, user_id) REFERENCES world_entities(entity_id, user_id) ON DELETE CASCADE
);
CREATE INDEX idx_world_relations_from ON world_relations(from_entity_id);
CREATE INDEX idx_world_relations_to ON world_relations(to_entity_id);
CREATE INDEX idx_world_relations_user ON world_relations(user_id);