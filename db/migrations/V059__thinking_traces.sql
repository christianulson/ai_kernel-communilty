-- V040__thinking_traces.sql
-- Persiste sessões de cadeia de pensamento para auditoria e consulta posterior
-- Dependência: V038__tool_dry_run_journal.sql

CREATE TABLE IF NOT EXISTS thinking_trace_sessions (
    id              VARCHAR(64)     NOT NULL PRIMARY KEY ,
    user_id         VARCHAR(128)    NULL        ,
    context_id      VARCHAR(255)    NULL        ,
    trace_level     INTEGER         NOT NULL DEFAULT 2 ,
    started_at      DATETIME(3)     NOT NULL    ,
    completed_at    DATETIME(3)     NULL        ,
    final_decision  TEXT            NULL        ,
    final_reasoning TEXT            NULL        ,
    created_at      DATETIME(3)     NOT NULL DEFAULT CURRENT_TIMESTAMP);
CREATE INDEX idx_thinking_traces_user ON thinking_trace_sessions(user_id);
CREATE INDEX idx_thinking_traces_context ON thinking_trace_sessions(context_id);
CREATE INDEX idx_thinking_traces_started ON thinking_trace_sessions(started_at);
CREATE INDEX idx_thinking_traces_completed ON thinking_trace_sessions(completed_at);

CREATE TABLE IF NOT EXISTS thinking_trace_steps (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    session_id          VARCHAR(64)     NOT NULL ,
    step_id             VARCHAR(64)     NOT NULL ,
    parent_step_id      VARCHAR(64)     NULL    ,
    agent               VARCHAR(128)    NOT NULL ,
    action              VARCHAR(255)    NOT NULL ,
    reasoning           TEXT            NULL    ,
    alternatives        TEXT            NULL    ,
    chosen_alternative  VARCHAR(255)    NULL    ,
    confidence          REAL          NOT NULL DEFAULT 0.0 ,
    evidence_summary    TEXT            NULL    ,
    trace_level         INTEGER         NOT NULL DEFAULT 2 ,
    step_timestamp      DATETIME(3)     NOT NULL ,
    created_at          DATETIME(3)     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_trace_steps_session FOREIGN KEY (session_id) REFERENCES thinking_trace_sessions(id) ON DELETE CASCADE);
CREATE INDEX idx_trace_steps_session ON thinking_trace_steps(session_id);
CREATE INDEX idx_trace_steps_step ON thinking_trace_steps(step_id);
CREATE INDEX idx_trace_steps_parent ON thinking_trace_steps(parent_step_id);
CREATE INDEX idx_trace_steps_agent ON thinking_trace_steps(agent);
CREATE INDEX idx_trace_steps_timestamp ON thinking_trace_steps(step_timestamp);
