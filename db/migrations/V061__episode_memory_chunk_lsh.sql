CREATE TABLE IF NOT EXISTS episode_memory_chunk_lsh (
  bucket_key VARCHAR(24) NOT NULL,
  chunk_id   VARCHAR(64) NOT NULL,
  created_at DATETIME(6) NOT NULL,
  PRIMARY KEY (bucket_key, chunk_id),
  CONSTRAINT fk_emc_lsh_chunk
    FOREIGN KEY (chunk_id) REFERENCES episode_memory_chunks(chunk_id)
    ON DELETE CASCADE
);
CREATE INDEX idx_emc_lsh_chunk_id ON episode_memory_chunk_lsh(chunk_id);
CREATE INDEX idx_emc_lsh_created_at ON episode_memory_chunk_lsh(created_at);
