CREATE TABLE IF NOT EXISTS agent_metrics (
  metric_id      VARCHAR(64)  NOT NULL,
  user_id        VARCHAR(64)  NOT NULL,
  episode_id     VARCHAR(64)  NULL,
  goal_hash      VARCHAR(64)  NOT NULL,

  success        INTEGER   NOT NULL,
  planned_steps  INT          NOT NULL,
  executed_steps INT          NOT NULL,
  risk_incidents INT          NOT NULL,
  loop_detected  INTEGER   NOT NULL,

  latency_ms     BIGINT       NOT NULL,
  estimated_cost REAL       NOT NULL,

  created_at     DATETIME(6)  NOT NULL);
CREATE INDEX idx_agent_metrics_user ON agent_metrics(user_id);
CREATE INDEX idx_agent_metrics_episode ON agent_metrics(episode_id);
CREATE INDEX idx_agent_metrics_created ON agent_metrics(created_at);
