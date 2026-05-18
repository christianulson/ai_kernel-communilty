-- V065: Links between memories and the cognitive moments where they were observed
CREATE TABLE IF NOT EXISTS temporal_memory_links (
    memory_id VARCHAR(128) NOT NULL,
    memory_kind VARCHAR(32) NOT NULL,
    moment_id VARCHAR(64) NOT NULL,
    moment_sequence BIGINT NOT NULL,
    observed_at DATETIME(6) NOT NULL,
    salience REAL NOT NULL DEFAULT 0,
    recency REAL NOT NULL DEFAULT 0,
    evidence_ids_json TEXT NOT NULL,
    metadata_json TEXT NOT NULL,
    PRIMARY KEY (memory_id, memory_kind),
    CONSTRAINT fk_temporal_memory_links_moment
        FOREIGN KEY (moment_id)
        REFERENCES moments(moment_id)
        ON DELETE CASCADE
);
CREATE INDEX idx_temporal_memory_moment ON temporal_memory_links(moment_id);
CREATE INDEX idx_temporal_memory_sequence ON temporal_memory_links(moment_sequence);
CREATE INDEX idx_temporal_memory_observed_at ON temporal_memory_links(observed_at);
