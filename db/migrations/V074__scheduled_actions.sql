CREATE TABLE IF NOT EXISTS scheduled_actions (
    action_id           VARCHAR(64) PRIMARY KEY,
    description         VARCHAR(512) NOT NULL,
    scheduled_at        DATETIME(3) NOT NULL,
    action_json         TEXT NOT NULL,
    domain              VARCHAR(64),
    recurrence_type     VARCHAR(16),
    recurrence_interval INT,
    recurrence_until    DATETIME(3),
    status              VARCHAR(16) NOT NULL DEFAULT 'pending',
    created_at          DATETIME(3) NOT NULL DEFAULT CURRENT_TIMESTAMP,
    executed_at         DATETIME(3));
CREATE INDEX idx_scheduled_at ON scheduled_actions(scheduled_at);
CREATE INDEX idx_status ON scheduled_actions(status);
