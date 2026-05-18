-- V028: Procedural memory persistence
CREATE TABLE IF NOT EXISTS procedural_memory (
    procedure_id VARCHAR(64) PRIMARY KEY,
    name VARCHAR(256) NOT NULL,
    domain VARCHAR(64) NOT NULL,
    trigger_pattern VARCHAR(512) NOT NULL,
    steps_json TEXT NOT NULL,
    success_rate REAL NOT NULL DEFAULT 0.0,
    usage_count INT NOT NULL DEFAULT 0,
    last_used_at DATETIME(6) DEFAULT CURRENT_TIMESTAMP,
    created_at DATETIME(6) DEFAULT CURRENT_TIMESTAMP);
CREATE INDEX idx_procedural_domain ON procedural_memory(domain);
CREATE INDEX idx_procedural_trigger ON procedural_memory(trigger_pattern);

CREATE TABLE IF NOT EXISTS procedural_executions (
    execution_id VARCHAR(64) PRIMARY KEY,
    procedure_id VARCHAR(64) NOT NULL,
    goal_id VARCHAR(64) NOT NULL,
    executed_at DATETIME(6) NOT NULL,
    succeeded INTEGER NOT NULL,
    failure_reason TEXT,
    steps_executed_json TEXT NOT NULL,
    created_at DATETIME(6) DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (procedure_id) REFERENCES procedural_memory(procedure_id) ON DELETE CASCADE
);
CREATE INDEX idx_proc_exec_procedure ON procedural_executions(procedure_id);
CREATE INDEX idx_proc_exec_goal ON procedural_executions(goal_id);
