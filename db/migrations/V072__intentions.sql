-- V072: Prospective memory intentions (Plano 5 - Profundidade Temporal)
CREATE TABLE IF NOT EXISTS intentions (
    intention_id    VARCHAR(64) PRIMARY KEY,
    description     VARCHAR(512) NOT NULL,
    trigger_type    VARCHAR(16) NOT NULL,
    trigger_at      DATETIME(3),
    trigger_delay   BIGINT,
    trigger_event   VARCHAR(128),
    action_json     TEXT NOT NULL,
    domain          VARCHAR(64),
    priority        REAL NOT NULL,
    created_at      DATETIME(3) NOT NULL,
    completed_at    DATETIME(3),
    status          VARCHAR(16) NOT NULL DEFAULT 'pending');
CREATE INDEX idx_intentions_status ON intentions(status);
CREATE INDEX idx_intentions_trigger_at ON intentions(trigger_at);
CREATE INDEX idx_intentions_domain ON intentions(domain);
