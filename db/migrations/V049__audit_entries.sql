CREATE TABLE IF NOT EXISTS audit_entries (
    id          VARCHAR(64)     NOT NULL PRIMARY KEY,
    action      VARCHAR(128)    NOT NULL,
    performed_by VARCHAR(256)   NOT NULL,
    detail      TEXT            NOT NULL,
    recorded_at DATETIME(6)     NOT NULL);
CREATE INDEX idx_audit_action ON audit_entries(action);
CREATE INDEX idx_audit_recorded_at ON audit_entries(recorded_at);
