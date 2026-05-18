ALTER TABLE episodes
  ADD COLUMN tenant_plan VARCHAR(16) NOT NULL DEFAULT 'free',
  ADD INDEX idx_episodes_plan_status_finished_created (tenant_plan, status, finished_at, created_at),
  ADD INDEX idx_episodes_plan_user_status_finished_created (tenant_plan, user_id, status, finished_at, created_at);
