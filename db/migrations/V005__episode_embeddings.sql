CREATE TABLE IF NOT EXISTS episode_memory_chunks (
  chunk_id        VARCHAR(64)  NOT NULL,
  episode_id      VARCHAR(64)  NOT NULL,
  user_id         VARCHAR(64)  NOT NULL,
  text            TEXT         NOT NULL,
  embedding_json  TEXT         NOT NULL,
  created_at      DATETIME(6)  NOT NULL,

  CONSTRAINT fk_emc_episode FOREIGN KEY (episode_id) REFERENCES episodes(episode_id)
    ON DELETE CASCADE
);
CREATE INDEX idx_emc_episode ON episode_memory_chunks(episode_id);
CREATE INDEX idx_emc_user ON episode_memory_chunks(user_id);
CREATE INDEX idx_emc_created ON episode_memory_chunks(created_at);
