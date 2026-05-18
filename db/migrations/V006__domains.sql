-- V006__domains.sql
-- Cadastro de domínios (data-driven) e aliases para inferência barata no Gateway.
-- IMPORTANTE: você disse que roda migrations manualmente e apenas uma vez.
-- Execute este arquivo UMA VEZ no schema do KernelDb.

CREATE TABLE IF NOT EXISTS domains (
  domain_id      VARCHAR(64)  NOT NULL,
  display_name   VARCHAR(128) NOT NULL,
  risk_level     VARCHAR(16)  NOT NULL DEFAULT 'medium',
  is_active      INTEGER   NOT NULL DEFAULT 1,
  created_at     TIMESTAMP    NOT NULL DEFAULT CURRENT_TIMESTAMP);

CREATE TABLE IF NOT EXISTS domain_aliases (
  alias          VARCHAR(128) NOT NULL,
  domain_id      VARCHAR(64)  NOT NULL,
  weight         INT          NOT NULL DEFAULT 1,
  created_at     TIMESTAMP    NOT NULL DEFAULT CURRENT_TIMESTAMP,

  CONSTRAINT fk_domain_aliases_domain FOREIGN KEY(domain_id) REFERENCES domains(domain_id)
);
CREATE INDEX ix_domain_aliases_domain ON domain_aliases(domain_id);

-- Seeds mínimos (idempotentes)
INSERT OR IGNORE INTO domains(domain_id, display_name, risk_level) VALUES
  ('general',      'Geral',        'low'),
  ('payments',     'Pagamentos',   'high'),
  ('integrations', 'Integrações',  'medium'),
  ('operations',   'Operações',    'medium'),
  ('identity',     'Identidade',   'high');

-- Aliases (PT/EN) para inferência do domínio. Ajuste livremente.
INSERT OR IGNORE INTO domain_aliases(alias, domain_id, weight) VALUES
  ('pix', 'payments', 3),
  ('boleto', 'payments', 3),
  ('cobrança', 'payments', 3),
  ('pagamento', 'payments', 3),
  ('payment', 'payments', 2),
  ('billing', 'payments', 2),

  ('api', 'integrations', 2),
  ('webhook', 'integrations', 3),
  ('integração', 'integrations', 3),
  ('integration', 'integrations', 2),

  ('ops', 'operations', 2),
  ('incidente', 'operations', 3),
  ('produção', 'operations', 2),
  ('deploy', 'operations', 2),
  ('oncall', 'operations', 2),

  ('login', 'identity', 3),
  ('auth', 'identity', 3),
  ('jwt', 'identity', 2),
  ('usuário', 'identity', 2),
  ('identity', 'identity', 2);
