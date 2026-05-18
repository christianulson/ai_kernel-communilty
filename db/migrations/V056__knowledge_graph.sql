CREATE TABLE IF NOT EXISTS knowledge_nodes (
    node_id     VARCHAR(128)    NOT NULL PRIMARY KEY,
    label       VARCHAR(256)    NOT NULL,
    node_type   VARCHAR(64)     NOT NULL,
    source      VARCHAR(128)    NOT NULL,
    confidence  REAL          NOT NULL DEFAULT 1.0,
    created_at  DATETIME(6)     NOT NULL,
    last_seen_at DATETIME(6)    NOT NULL,
    properties  TEXT            NOT NULL DEFAULT ('{}'));
CREATE INDEX idx_kn_type ON knowledge_nodes(node_type);
CREATE INDEX idx_kn_label ON knowledge_nodes(label);
CREATE INDEX idx_kn_confidence ON knowledge_nodes(confidence);
CREATE INDEX idx_kn_last_seen ON knowledge_nodes(last_seen_at);

CREATE TABLE IF NOT EXISTS knowledge_edges (
    edge_id         VARCHAR(128)    NOT NULL PRIMARY KEY,
    source_node_id  VARCHAR(128)    NOT NULL,
    target_node_id  VARCHAR(128)    NOT NULL,
    relation_type   VARCHAR(64)     NOT NULL,
    confidence      REAL          NOT NULL DEFAULT 1.0,
    source          VARCHAR(128)    NOT NULL,
    discovered_at   DATETIME(6)     NOT NULL,
    FOREIGN KEY (source_node_id) REFERENCES knowledge_nodes(node_id) ON DELETE CASCADE,
    FOREIGN KEY (target_node_id) REFERENCES knowledge_nodes(node_id) ON DELETE CASCADE
);
CREATE INDEX idx_ke_source ON knowledge_edges(source_node_id);
CREATE INDEX idx_ke_target ON knowledge_edges(target_node_id);
CREATE INDEX idx_ke_relation ON knowledge_edges(relation_type);
