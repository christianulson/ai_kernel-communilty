CREATE TABLE IF NOT EXISTS evolution_journal (
    journal_id BIGINT AUTO_INCREMENT PRIMARY KEY,
    cycle_id VARCHAR(64) NOT NULL,
    experiment_id VARCHAR(64) NOT NULL,
    opportunity_id VARCHAR(64) NOT NULL,
    experiment_type VARCHAR(64) NOT NULL,
    target_module VARCHAR(128) NOT NULL,
    baseline_config_json TEXT NOT NULL,
    variant_config_json TEXT NOT NULL,
    baseline_fitness REAL NULL,
    variant_fitness REAL NULL,
    improvement_percent REAL NULL,
    outcome VARCHAR(32) NOT NULL,
    blocked_reason TEXT NULL,
    safety_gate_result VARCHAR(64) NULL,
    created_at DATETIME(6) NOT NULL);
CREATE INDEX idx_evolution_journal_cycle ON evolution_journal(cycle_id);
CREATE INDEX idx_evolution_journal_experiment ON evolution_journal(experiment_id);
CREATE INDEX idx_evolution_journal_module ON evolution_journal(target_module);
CREATE INDEX idx_evolution_journal_created ON evolution_journal(created_at);
CREATE INDEX idx_evolution_journal_outcome ON evolution_journal(outcome);
