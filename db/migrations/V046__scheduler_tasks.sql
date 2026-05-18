CREATE TABLE IF NOT EXISTS scheduler_tasks (
  action_id VARCHAR(128) NOT NULL PRIMARY KEY,
  description TEXT NOT NULL,
  scheduled_at DATETIME NOT NULL,
  action_json TEXT NOT NULL,
  domain VARCHAR(128) NULL,
  recurrence_json TEXT NULL,
  status VARCHAR(32) NOT NULL,
  updated_at DATETIME NOT NULL
);

CREATE INDEX idx_scheduler_tasks_due ON scheduler_tasks(status, scheduled_at);
