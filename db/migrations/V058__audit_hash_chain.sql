ALTER TABLE audit_entries
    ADD COLUMN previous_hash VARCHAR(64) NULL,
    ADD COLUMN hash VARCHAR(64) NOT NULL DEFAULT '',
    ADD INDEX idx_audit_hash (hash);
