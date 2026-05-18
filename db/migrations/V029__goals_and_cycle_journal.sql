-- V029: Goals and goal cycle journal
CREATE TABLE IF NOT EXISTS goals (
    goal_id VARCHAR(64) PRIMARY KEY,
    parent_goal_id VARCHAR(64) DEFAULT NULL,
    description VARCHAR(1024) NOT NULL,
    status VARCHAR(32) NOT NULL DEFAULT 'active',
    progress REAL NOT NULL DEFAULT 0.0,
    priority REAL NOT NULL DEFAULT 0.5,
    created_at DATETIME(6) NOT NULL,
    deadline DATETIME(6) DEFAULT NULL,
    sub_goal_ids_json TEXT,
    depends_on_goal_ids_json TEXT,
    success_metrics_json TEXT);
CREATE INDEX idx_goals_status ON goals(status);
CREATE INDEX idx_goals_priority ON goals(priority);
CREATE INDEX idx_goals_created ON goals(created_at);
CREATE INDEX idx_goals_deadline ON goals(deadline);

CREATE TABLE IF NOT EXISTS goal_cycle_journal (
    cycle_id VARCHAR(64) PRIMARY KEY,
    selected_at DATETIME(6) NOT NULL,
    has_goal INTEGER NOT NULL DEFAULT 0,
    selected_goal_id VARCHAR(64) DEFAULT NULL,
    previous_progress REAL DEFAULT NULL,
    new_progress REAL DEFAULT NULL,
    previous_status VARCHAR(32) DEFAULT NULL,
    new_status VARCHAR(32) NOT NULL,
    goals_evaluated INT NOT NULL DEFAULT 0,
    completed INTEGER NOT NULL DEFAULT 0);
CREATE INDEX idx_gcj_selected_at ON goal_cycle_journal(selected_at);
CREATE INDEX idx_gcj_selected_goal ON goal_cycle_journal(selected_goal_id);
