-- FTS5 equivalent for semantic_chunks.text:
CREATE VIRTUAL TABLE semantic_chunks_fts USING fts5(content=text, content=semantic_chunks, content_rowid='rowid');
