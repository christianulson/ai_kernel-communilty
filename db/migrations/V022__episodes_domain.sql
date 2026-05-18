-- Adiciona coluna domain aos episódios para rastreamento de domínio
ALTER TABLE episodes ADD COLUMN domain VARCHAR(32) NOT NULL DEFAULT 'Generic';
CREATE INDEX idx_episode_domain ON episodes(domain);