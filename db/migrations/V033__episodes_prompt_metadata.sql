ALTER TABLE episodes
    ADD COLUMN prompt_capability VARCHAR(80) NULL,
    ADD COLUMN prompt_version VARCHAR(80) NULL,
    ADD COLUMN provider VARCHAR(80) NULL,
    ADD COLUMN model VARCHAR(160) NULL,
    ADD COLUMN prompt_metadata_json TEXT NULL,
    ADD INDEX idx_episodes_prompt_version (prompt_version),
    ADD INDEX idx_episodes_provider_model (provider, model);
