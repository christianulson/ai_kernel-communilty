CREATE TABLE IF NOT EXISTS world_beliefs (
  user_id        VARCHAR(64)  NOT NULL,
  subject        VARCHAR(128) NOT NULL,
  predicate      VARCHAR(128) NOT NULL,
  object         VARCHAR(256) NOT NULL,
  confidence     REAL       NOT NULL,
  last_updated   DATETIME(6)  NOT NULL,
  source         VARCHAR(64)  NOT NULL DEFAULT 'agent',
  evidence_episode_ids_json TEXT DEFAULT NULL,
  contradicts_json TEXT DEFAULT NULL,

  PRIMARY KEY (user_id, subject, predicate, object));
CREATE INDEX idx_world_beliefs_updated ON world_beliefs(last_updated);
