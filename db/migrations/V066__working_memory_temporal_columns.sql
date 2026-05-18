-- V066: Add temporal linking columns to working_memory
ALTER TABLE working_memory
    ADD COLUMN moment_id VARCHAR(64) NULL,
    ADD COLUMN moment_sequence BIGINT NULL,
    ADD COLUMN observed_at DATETIME(6) NULL,
    ADD INDEX idx_working_memory_moment (moment_id),
    ADD INDEX idx_working_memory_moment_sequence (moment_sequence);
