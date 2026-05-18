CREATE TABLE IF NOT EXISTS tenant_metric_entries (
    id              BIGINT AUTO_INCREMENT PRIMARY KEY,
    tenant_id       VARCHAR(128)    NOT NULL,
    latency_ms      BIGINT          NOT NULL DEFAULT 0,
    success         INTEGER      NOT NULL DEFAULT 1,
    cost_usd        REAL          NOT NULL DEFAULT 0,
    episode_count   INT             NOT NULL DEFAULT 0,
    storage_bytes   BIGINT          NOT NULL DEFAULT 0,
    recorded_at     DATETIME(6)     NOT NULL);
CREATE INDEX idx_metric_tenant ON tenant_metric_entries(tenant_id);
CREATE INDEX idx_metric_recorded ON tenant_metric_entries(recorded_at);

CREATE TABLE IF NOT EXISTS tenant_metrics_current (
    tenant_id          VARCHAR(128)    NOT NULL PRIMARY KEY,
    plan               VARCHAR(32)     NOT NULL DEFAULT 'Free',
    requests_per_minute REAL         NOT NULL DEFAULT 0,
    avg_latency_ms     REAL          NOT NULL DEFAULT 0,
    p95_latency_ms     REAL          NOT NULL DEFAULT 0,
    p99_latency_ms     REAL          NOT NULL DEFAULT 0,
    success_rate       REAL          NOT NULL DEFAULT 100,
    uptime_percent     REAL          NOT NULL DEFAULT 100,
    cost_per_hour_usd  REAL          NOT NULL DEFAULT 0,
    errors_per_hour    INT             NOT NULL DEFAULT 0,
    active_episodes    INT             NOT NULL DEFAULT 0,
    storage_mb         BIGINT          NOT NULL DEFAULT 0,
    last_updated       DATETIME(6)     NOT NULL
);
