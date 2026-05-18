-- V070: Archive tables for soft-delete (Plano 15 - Memorial de Memorias)

CREATE TABLE IF NOT EXISTS working_memory_archive (
    slot_id VARCHAR(64) PRIMARY KEY,
    context_key VARCHAR(128) NOT NULL,
    content_json TEXT,
    relevance REAL NOT NULL,
    activation INT NOT NULL DEFAULT 0,
    created_at DATETIME(3) NOT NULL,
    last_accessed_at DATETIME(3) NOT NULL,
    expires_at DATETIME(3),
    moment_id VARCHAR(64),
    moment_sequence BIGINT,
    observed_at DATETIME(3),
    forgotten_at DATETIME(3) NOT NULL,
    forget_reason VARCHAR(128) NOT NULL,
    utility_at_death REAL,
    importance_at_death REAL,
    purge_after DATETIME(3) NOT NULL);
CREATE INDEX idx_wm_archive_purge ON working_memory_archive(purge_after);
CREATE INDEX idx_wm_archive_reason ON working_memory_archive(forget_reason);
CREATE INDEX idx_wm_archive_forgotten ON working_memory_archive(forgotten_at);

CREATE TABLE IF NOT EXISTS episodes_archive (
    episode_id VARCHAR(64) PRIMARY KEY,
    user_id VARCHAR(64) NOT NULL,
    summary TEXT,
    domain VARCHAR(64),
    score REAL,
    created_at DATETIME(3) NOT NULL,
    finished_at DATETIME(3),
    metadata_json TEXT,
    forgotten_at DATETIME(3) NOT NULL,
    forget_reason VARCHAR(128) NOT NULL,
    utility_at_death REAL,
    importance_at_death REAL,
    purge_after DATETIME(3) NOT NULL);
CREATE INDEX idx_ep_archive_purge ON episodes_archive(purge_after);
CREATE INDEX idx_ep_archive_reason ON episodes_archive(forget_reason);
CREATE INDEX idx_ep_archive_user ON episodes_archive(user_id);

CREATE TABLE IF NOT EXISTS semantic_chunks_archive (
    chunk_id VARCHAR(64) PRIMARY KEY,
    doc_id VARCHAR(64) NOT NULL,
    text TEXT NOT NULL,
    embedding_json TEXT,
    created_at DATETIME(3) NOT NULL,
    metadata_json TEXT,
    forgotten_at DATETIME(3) NOT NULL,
    forget_reason VARCHAR(128) NOT NULL,
    utility_at_death REAL,
    importance_at_death REAL,
    purge_after DATETIME(3) NOT NULL);
CREATE INDEX idx_sc_archive_purge ON semantic_chunks_archive(purge_after);
CREATE INDEX idx_sc_archive_doc ON semantic_chunks_archive(doc_id);

CREATE TABLE IF NOT EXISTS knowledge_nodes_archive (
    node_id VARCHAR(64) PRIMARY KEY,
    label VARCHAR(256) NOT NULL,
    domain VARCHAR(64),
    confidence REAL,
    last_seen_at DATETIME(3),
    metadata_json TEXT,
    forgotten_at DATETIME(3) NOT NULL,
    forget_reason VARCHAR(128) NOT NULL,
    utility_at_death REAL,
    importance_at_death REAL,
    purge_after DATETIME(3) NOT NULL);
CREATE INDEX idx_kn_archive_purge ON knowledge_nodes_archive(purge_after);
CREATE INDEX idx_kn_archive_domain ON knowledge_nodes_archive(domain);
