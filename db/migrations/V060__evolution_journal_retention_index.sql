CREATE INDEX idx_evolution_journal_cleanup ON evolution_journal(created_at);

ALTER TABLE evolution_journal
    ADD COLUMN retention_until DATETIME(6) NULL;