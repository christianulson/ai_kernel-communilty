-- Política de TTL para memória episódica V2 (forgetting curve)
-- Define regras de expiração por tenant/agente
CREATE TABLE IF NOT EXISTS episodic_memory_ttl_policy (
    id BLOB PRIMARY KEY,
    tenant_id BLOB NOT NULL,
    agent_id BLOB,
    memory_type VARCHAR(32) NOT NULL DEFAULT 'episodic',
    ttl_hours INT NOT NULL DEFAULT 720,
    max_entries INT NOT NULL DEFAULT 10000,
    created_at DATETIME(6) NOT NULL);
CREATE INDEX idx_tenant_agent ON episodic_memory_ttl_policy(tenant_id, agent_id);
