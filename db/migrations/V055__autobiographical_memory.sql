CREATE TABLE IF NOT EXISTS autobiographical_events (
    sequence    BIGINT AUTO_INCREMENT PRIMARY KEY,
    kind        VARCHAR(32)     NOT NULL,
    title       VARCHAR(256)    NOT NULL,
    details     TEXT            NOT NULL,
    domain      VARCHAR(128)    NOT NULL,
    personality VARCHAR(32)     NOT NULL,
    recorded_at DATETIME(6)     NOT NULL);
CREATE INDEX idx_ae_kind ON autobiographical_events(kind);
CREATE INDEX idx_ae_domain ON autobiographical_events(domain);
CREATE INDEX idx_ae_recorded ON autobiographical_events(recorded_at);

CREATE TABLE IF NOT EXISTS self_narratives (
    id          BIGINT AUTO_INCREMENT PRIMARY KEY,
    narrative   TEXT            NOT NULL,
    created_at  DATETIME(6)     NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS identity_events (
    sequence    BIGINT AUTO_INCREMENT PRIMARY KEY,
    event_type  VARCHAR(64)     NOT NULL,
    description TEXT            NOT NULL,
    event_timestamp DATETIME(6) NOT NULL);
CREATE INDEX idx_ie_type ON identity_events(event_type);
CREATE INDEX idx_ie_timestamp ON identity_events(event_timestamp);
