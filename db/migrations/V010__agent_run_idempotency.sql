CREATE TABLE IF NOT EXISTS agent_run_idempotency (
  user_id         VARCHAR(64)   NOT NULL,
  idempotency_key VARCHAR(128)  NOT NULL,
  fingerprint     CHAR(64)      NOT NULL,
  response_json   TEXT      NOT NULL,
  created_at      DATETIME(6)   NOT NULL,
  expires_at      DATETIME(6)   NOT NULL,

  PRIMARY KEY (user_id, idempotency_key));
CREATE INDEX idx_agent_run_idempotency_exp ON agent_run_idempotency(expires_at);
