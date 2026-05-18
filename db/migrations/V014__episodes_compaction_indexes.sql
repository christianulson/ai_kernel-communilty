CREATE INDEX idx_episodes_user_status_finished_created ON episodes(user_id, status, finished_at, created_at);
