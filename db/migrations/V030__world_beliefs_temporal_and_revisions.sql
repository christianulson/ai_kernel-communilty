ALTER TABLE world_beliefs
  ADD COLUMN valid_from DATETIME(6) NULL,
  ADD COLUMN valid_until DATETIME(6) NULL,
  ADD KEY idx_world_beliefs_validity (user_id, valid_from, valid_until);

CREATE TABLE IF NOT EXISTS world_belief_revisions (
  revision_id              VARCHAR(64)  NOT NULL,
  belief_id                VARCHAR(512) NOT NULL,
  user_id                  VARCHAR(64)  NOT NULL,
  subject                  VARCHAR(128) NOT NULL,
  predicate                VARCHAR(128) NOT NULL,
  object                   VARCHAR(256) NOT NULL,
  previous_confidence      REAL       NOT NULL,
  new_confidence          REAL       NOT NULL,
  reason                   VARCHAR(128) NOT NULL,
  revised_at               DATETIME(6)  NOT NULL,
  evidence_episode_ids_json TEXT DEFAULT NULL);
CREATE INDEX idx_world_belief_revisions_belief ON world_belief_revisions(user_id, subject, predicate, object);
CREATE INDEX idx_world_belief_revisions_revised_at ON world_belief_revisions(revised_at);
