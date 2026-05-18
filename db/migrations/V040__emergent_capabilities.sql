-- V040__emergent_capabilities.sql
-- Catálogo de capacidades emergentes detectadas pelo sistema
-- Dependência: V039__thinking_traces.sql

CREATE TABLE IF NOT EXISTS emergent_capabilities (
    id              VARCHAR(64)     NOT NULL PRIMARY KEY ,
    description     TEXT            NOT NULL ,
    category        INTEGER         NOT NULL ,
    confidence      REAL          NOT NULL DEFAULT 0.0 ,
    status          INTEGER         NOT NULL DEFAULT 0 ,
    evidence_json   TEXT            NOT NULL ,
    created_at      DATETIME(3)     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    reviewed_at     DATETIME(3)     NULL );
CREATE INDEX idx_emergent_category ON emergent_capabilities(category);
CREATE INDEX idx_emergent_status ON emergent_capabilities(status);
CREATE INDEX idx_emergent_confidence ON emergent_capabilities(confidence);
CREATE INDEX idx_emergent_created ON emergent_capabilities(created_at);

CREATE TABLE IF NOT EXISTS emergent_observations (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    task_type       VARCHAR(255)    NOT NULL ,
    observed_output TEXT            NOT NULL ,
    expected_output TEXT            NULL    ,
    confidence      REAL          NOT NULL DEFAULT 0.0 ,
    observed_at     DATETIME(3)     NOT NULL DEFAULT CURRENT_TIMESTAMP);
CREATE INDEX idx_emergent_obs_task ON emergent_observations(task_type);
CREATE INDEX idx_emergent_obs_time ON emergent_observations(observed_at);
