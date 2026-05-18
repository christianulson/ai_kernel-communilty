CREATE TABLE IF NOT EXISTS neural_model_registry (
    model_id VARCHAR(96) NOT NULL,
    model_version VARCHAR(96) NOT NULL,
    use_case VARCHAR(64) NOT NULL,
    runtime VARCHAR(64) NOT NULL,
    artifact_path VARCHAR(512) NOT NULL,
    artifact_hash VARCHAR(128) NOT NULL,
    schema_version VARCHAR(96) NOT NULL,
    status VARCHAR(16) NOT NULL,
    rollback_target_version VARCHAR(96) NULL,
    approved_by VARCHAR(128) NULL,
    approval_reason VARCHAR(512) NULL,
    created_at DATETIME(6) NOT NULL,
    updated_at DATETIME(6) NOT NULL,
    activated_at DATETIME(6) NULL,
    PRIMARY KEY (model_id, model_version));
CREATE INDEX idx_neural_model_registry_status ON neural_model_registry(model_id, status);
CREATE INDEX idx_neural_model_registry_use_case ON neural_model_registry(use_case, status);
CREATE INDEX idx_neural_model_registry_artifact_hash ON neural_model_registry(artifact_hash);
