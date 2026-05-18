CREATE INDEX idx_episodes_status_finished_created ON episodes(status, finished_at, created_at);
