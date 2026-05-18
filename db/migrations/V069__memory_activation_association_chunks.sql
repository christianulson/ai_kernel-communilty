-- V069: Memory activation log, association links, and chunk store (Plano 14)

CREATE TABLE IF NOT EXISTS memory_activation_log (
    memory_id VARCHAR(64) NOT NULL,
    accessed_at DATETIME(3) NOT NULL,
    memory_type VARCHAR(32) NOT NULL,
    PRIMARY KEY (memory_id, accessed_at));
CREATE INDEX idx_activation_accessed_at ON memory_activation_log(accessed_at);

CREATE TABLE IF NOT EXISTS association_links (
    source_id VARCHAR(64) NOT NULL,
    target_id VARCHAR(64) NOT NULL,
    weight REAL NOT NULL,
    co_occurrence_count INT NOT NULL,
    last_updated DATETIME(3) NOT NULL,
    PRIMARY KEY (source_id, target_id));
CREATE INDEX idx_association_target ON association_links(target_id);
CREATE INDEX idx_association_weight ON association_links(weight);

CREATE TABLE IF NOT EXISTS chunks (
    chunk_id VARCHAR(64) PRIMARY KEY,
    context_hash VARCHAR(64) NOT NULL,
    signature_domain VARCHAR(64),
    signature_pattern VARCHAR(256),
    condition_json TEXT,
    action_json TEXT,
    confidence REAL NOT NULL,
    hit_count INT NOT NULL,
    success_count INT NOT NULL,
    created_at DATETIME(3) NOT NULL,
    last_used_at DATETIME(3) NOT NULL);
CREATE INDEX idx_chunks_context_hash ON chunks(context_hash);
CREATE INDEX idx_chunks_confidence ON chunks(confidence);
