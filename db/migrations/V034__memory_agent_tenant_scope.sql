ALTER TABLE semantic_documents
  ADD COLUMN tenant_id VARCHAR(64) NULL,
  ADD COLUMN agent_id VARCHAR(64) NULL,
  ADD COLUMN memory_scope VARCHAR(16) NOT NULL DEFAULT 'shared',
  ADD INDEX idx_semantic_documents_scope (tenant_id, agent_id, memory_scope);

ALTER TABLE semantic_chunks
  ADD COLUMN tenant_id VARCHAR(64) NULL,
  ADD COLUMN agent_id VARCHAR(64) NULL,
  ADD COLUMN memory_scope VARCHAR(16) NOT NULL DEFAULT 'shared',
  ADD INDEX idx_semantic_chunks_scope (tenant_id, agent_id, memory_scope);

ALTER TABLE episode_memory_chunks
  ADD COLUMN tenant_id VARCHAR(64) NULL,
  ADD COLUMN agent_id VARCHAR(64) NULL,
  ADD COLUMN memory_scope VARCHAR(16) NOT NULL DEFAULT 'agent',
  ADD INDEX idx_episode_memory_scope (tenant_id, user_id, agent_id, memory_scope);

ALTER TABLE procedural_memory
  ADD COLUMN tenant_id VARCHAR(64) NULL,
  ADD COLUMN agent_id VARCHAR(64) NULL,
  ADD COLUMN memory_scope VARCHAR(16) NOT NULL DEFAULT 'shared',
  ADD INDEX idx_procedural_memory_scope (tenant_id, agent_id, memory_scope);

ALTER TABLE procedural_executions
  ADD COLUMN tenant_id VARCHAR(64) NULL,
  ADD COLUMN agent_id VARCHAR(64) NULL,
  ADD INDEX idx_procedural_executions_scope (tenant_id, agent_id, procedure_id);

ALTER TABLE working_memory
  ADD COLUMN tenant_id VARCHAR(64) NULL,
  ADD COLUMN agent_id VARCHAR(64) NULL,
  ADD COLUMN memory_scope VARCHAR(16) NOT NULL DEFAULT 'agent',
  ADD INDEX idx_working_memory_scope (tenant_id, agent_id, memory_scope);
