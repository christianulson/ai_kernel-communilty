ALTER TABLE episodes
    ADD COLUMN goal_id VARCHAR(80) NULL,
    ADD KEY idx_episode_goal_id (goal_id);
