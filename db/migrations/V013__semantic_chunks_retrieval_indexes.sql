ALTER TABLE semantic_chunks
  ADD UNIQUE INDEX uq_semantic_chunks_doc_chunk_index (doc_id, chunk_index),
  ADD INDEX idx_semantic_chunks_created_at (created_at);
