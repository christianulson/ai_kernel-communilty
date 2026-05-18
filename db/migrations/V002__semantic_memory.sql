CREATE TABLE IF NOT EXISTS semantic_documents (
  doc_id        VARCHAR(64) NOT NULL,
  title         VARCHAR(255) NOT NULL,
  source        VARCHAR(255) NULL,
  tags_json      TEXT NULL,
  created_at    DATETIME(6) NOT NULL);

CREATE TABLE IF NOT EXISTS semantic_chunks (
  chunk_id      VARCHAR(64) NOT NULL,
  doc_id        VARCHAR(64) NOT NULL,
  chunk_index   INT NOT NULL,
  text          TEXT NOT NULL,
  embedding_json TEXT NOT NULL, -- float[] serializado (MVP)
  created_at    DATETIME(6) NOT NULL,

  CONSTRAINT fk_sem_chunk_doc FOREIGN KEY (doc_id) REFERENCES semantic_documents(doc_id)
);
CREATE INDEX idx_doc ON semantic_chunks(doc_id);
