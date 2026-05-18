CREATE TABLE IF NOT EXISTS anonymized_patterns (
    pattern_type        VARCHAR(64)     NOT NULL,
    subject             VARCHAR(256)    NOT NULL,
    relation            VARCHAR(128)    NOT NULL,
    object              VARCHAR(256)    NOT NULL,
    aggregate_confidence REAL         NOT NULL DEFAULT 0.0,
    observation_count   INT             NOT NULL DEFAULT 1,
    domain              VARCHAR(128)    NOT NULL,
    created_at          DATETIME(6)     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (pattern_type, domain, subject(128), relation, object(128)));
CREATE INDEX idx_pattern_domain ON anonymized_patterns(domain);
CREATE INDEX idx_pattern_confidence ON anonymized_patterns(aggregate_confidence);
CREATE INDEX idx_pattern_observations ON anonymized_patterns(observation_count);

CREATE TABLE IF NOT EXISTS transfer_applications (
    id          BIGINT AUTO_INCREMENT PRIMARY KEY,
    domain      VARCHAR(128)    NOT NULL,
    success     INTEGER      NOT NULL DEFAULT 1,
    applied_at  DATETIME(6)     NOT NULL DEFAULT CURRENT_TIMESTAMP);
CREATE INDEX idx_transfer_domain ON transfer_applications(domain);
CREATE INDEX idx_transfer_applied ON transfer_applications(applied_at);
