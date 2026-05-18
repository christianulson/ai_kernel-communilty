CREATE TABLE IF NOT EXISTS semantic_chunk_lsh (
  bucket_key VARCHAR(24) NOT NULL,
  chunk_id   VARCHAR(64) NOT NULL,
  created_at DATETIME(6) NOT NULL,
  PRIMARY KEY (bucket_key, chunk_id),
  CONSTRAINT fk_semantic_chunk_lsh_chunk
    FOREIGN KEY (chunk_id) REFERENCES semantic_chunks(chunk_id)
    ON DELETE CASCADE
);
CREATE INDEX idx_semantic_chunk_lsh_chunk_id ON semantic_chunk_lsh(chunk_id);
CREATE INDEX idx_semantic_chunk_lsh_created_at ON semantic_chunk_lsh(created_at);

