-- Tabela de resolução de entidades para memória episódica V2
-- Suporta busca por entidade e linking entre memórias
CREATE TABLE IF NOT EXISTS episodic_memory_entity_resolution (
    id BLOB PRIMARY KEY,
    entity_name VARCHAR(255) NOT NULL,
    normalized_name VARCHAR(255) NOT NULL,
    entity_type VARCHAR(64) NOT NULL DEFAULT 'concept',
    frequency INT NOT NULL DEFAULT 1,
    first_seen_at DATETIME(6) NOT NULL,
    last_seen_at DATETIME(6) NOT NULL);
CREATE INDEX idx_normalized ON episodic_memory_entity_resolution(normalized_name);
CREATE INDEX idx_type ON episodic_memory_entity_resolution(entity_type);
