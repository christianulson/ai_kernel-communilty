CREATE TABLE IF NOT EXISTS agent_instances (
    instance_id             VARCHAR(64)     NOT NULL PRIMARY KEY,
    role_name               VARCHAR(128)    NOT NULL,
    display_name            VARCHAR(256)    NOT NULL,
    status                  VARCHAR(32)     NOT NULL DEFAULT 'idle',
    created_at              DATETIME(6)     NOT NULL,
    last_active_at          DATETIME(6)     NOT NULL,
    total_tasks_assigned    INT             NOT NULL DEFAULT 0,
    tasks_completed         INT             NOT NULL DEFAULT 0,
    success_rate            REAL          NOT NULL DEFAULT 1.0,
    current_task_ids        TEXT            NOT NULL DEFAULT ('[]'),
    performance_metrics     TEXT            NOT NULL DEFAULT ('{}'),
    capabilities            TEXT            NULL,
    allowed_tools           TEXT            NULL,
    default_memory_scope    VARCHAR(128)    NULL,
    max_risk_level          VARCHAR(32)     NULL);
CREATE INDEX idx_agent_status ON agent_instances(status);

CREATE TABLE IF NOT EXISTS agent_task_assignments (
    assignment_id       VARCHAR(64)     NOT NULL PRIMARY KEY,
    agent_instance_id   VARCHAR(64)     NOT NULL,
    task_id             VARCHAR(64)     NOT NULL,
    task_description    TEXT            NOT NULL,
    status              VARCHAR(32)     NOT NULL,
    assigned_at         DATETIME(6)     NOT NULL,
    completed_at        DATETIME(6)     NULL,
    result_summary      TEXT            NULL,
    FOREIGN KEY (agent_instance_id) REFERENCES agent_instances(instance_id) ON DELETE CASCADE
);
CREATE INDEX idx_task_agent ON agent_task_assignments(agent_instance_id);
CREATE INDEX idx_task_assigned ON agent_task_assignments(assigned_at);

CREATE TABLE IF NOT EXISTS agent_collaborations (
    collaboration_id    VARCHAR(64)     NOT NULL PRIMARY KEY,
    primary_agent_id    VARCHAR(64)     NOT NULL,
    supporting_agent_ids TEXT           NOT NULL DEFAULT ('[]'),
    goal                TEXT            NOT NULL,
    strategy            VARCHAR(64)     NOT NULL,
    started_at          DATETIME(6)     NOT NULL,
    completed_at        DATETIME(6)     NULL,
    success             INTEGER      NOT NULL DEFAULT 0,
    FOREIGN KEY (primary_agent_id) REFERENCES agent_instances(instance_id) ON DELETE CASCADE
);
CREATE INDEX idx_collab_primary ON agent_collaborations(primary_agent_id);
