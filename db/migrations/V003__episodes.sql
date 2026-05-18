CREATE TABLE IF NOT EXISTS episodes (
    episode_id        VARCHAR(64)  NOT NULL,
    user_id           VARCHAR(64)  NOT NULL,
    goal              TEXT         NOT NULL,

    plan_json         TEXT         NOT NULL,
    steps_json        TEXT         NOT NULL,

    status            VARCHAR(32)  NOT NULL, -- completed | aborted | failed
    summary           TEXT         NOT NULL,

    created_at        DATETIME(6)  NOT NULL,
    finished_at       DATETIME(6)  NULL);
CREATE INDEX idx_episode_user ON episodes(user_id);
CREATE INDEX idx_episode_created ON episodes(created_at);
