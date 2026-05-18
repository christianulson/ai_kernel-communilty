CREATE TABLE IF NOT EXISTS analog_shadow_alert_history (
    alert_history_id INTEGER PRIMARY KEY AUTOINCREMENT,
    evaluated_at DATETIME(6) NOT NULL,
    shadow_mode_enabled INTEGER NOT NULL,
    production_mode_enabled INTEGER NOT NULL,
    total_observations INT NOT NULL,
    divergence_count INT NOT NULL,
    decision_transition_count INT NOT NULL,
    decision_transition_rate REAL NOT NULL,
    stability_score REAL NOT NULL,
    enabled INTEGER NOT NULL,
    alerting INTEGER NOT NULL,
    severity VARCHAR(16) NOT NULL,
    live_divergence_rate REAL NOT NULL,
    live_stability_score REAL NOT NULL,
    rollup_divergence_rate REAL NOT NULL,
    rollup_stability_score REAL NOT NULL,
    observations_considered INT NOT NULL,
    rollup_count INT NOT NULL,
    findings_json TEXT NOT NULL);
CREATE INDEX idx_analog_shadow_alert_history_evaluated_at ON analog_shadow_alert_history(evaluated_at);
CREATE INDEX idx_analog_shadow_alert_history_alerting ON analog_shadow_alert_history(alerting);
CREATE INDEX idx_analog_shadow_alert_history_severity ON analog_shadow_alert_history(severity);
