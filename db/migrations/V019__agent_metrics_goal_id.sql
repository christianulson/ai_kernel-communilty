ALTER TABLE agent_metrics
    ADD COLUMN goal_id VARCHAR(80) NULL,
    ADD KEY idx_agent_metrics_goal_id (goal_id);
