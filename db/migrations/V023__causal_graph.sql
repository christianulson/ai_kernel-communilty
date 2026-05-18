-- V023: Causal graph persistence
CREATE TABLE IF NOT EXISTS causal_nodes (
    node_id VARCHAR(64) PRIMARY KEY,
    type VARCHAR(32) NOT NULL,
    label VARCHAR(256) NOT NULL,
    properties_json TEXT,
    created_at DATETIME(6) DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME(6) DEFAULT CURRENT_TIMESTAMP );
CREATE INDEX idx_causal_nodes_type ON causal_nodes(type);
CREATE INDEX idx_causal_nodes_label ON causal_nodes(label);

CREATE TABLE IF NOT EXISTS causal_edges (
    from_node_id VARCHAR(64) NOT NULL,
    to_node_id VARCHAR(64) NOT NULL,
    relation VARCHAR(32) NOT NULL,
    confidence REAL NOT NULL DEFAULT 0.5,
    observation_count INT NOT NULL DEFAULT 1,
    created_at DATETIME(6) DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME(6) DEFAULT CURRENT_TIMESTAMP ,
    PRIMARY KEY (from_node_id, to_node_id),
    FOREIGN KEY (from_node_id) REFERENCES causal_nodes(node_id) ON DELETE CASCADE,
    FOREIGN KEY (to_node_id) REFERENCES causal_nodes(node_id) ON DELETE CASCADE
);
CREATE INDEX idx_causal_edges_from ON causal_edges(from_node_id);
CREATE INDEX idx_causal_edges_to ON causal_edges(to_node_id);
CREATE INDEX idx_causal_edges_confidence ON causal_edges(confidence);