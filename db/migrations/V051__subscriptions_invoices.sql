CREATE TABLE IF NOT EXISTS subscriptions (
    id                  VARCHAR(64)     NOT NULL PRIMARY KEY,
    user_id             VARCHAR(128)    NOT NULL,
    plan_id             VARCHAR(64)     NOT NULL,
    status              VARCHAR(32)     NOT NULL,
    current_period_start DATETIME(6)    NOT NULL,
    current_period_end   DATETIME(6)    NOT NULL,
    cancel_at_period_end INTEGER     NOT NULL DEFAULT 0,
    created_at          DATETIME(6)     NOT NULL DEFAULT CURRENT_TIMESTAMP);
CREATE INDEX idx_sub_user ON subscriptions(user_id);
CREATE INDEX idx_sub_status ON subscriptions(status);

CREATE TABLE IF NOT EXISTS invoices (
    id              VARCHAR(64)     NOT NULL PRIMARY KEY,
    subscription_id VARCHAR(64)     NOT NULL,
    amount_cents    INT             NOT NULL,
    currency        VARCHAR(8)      NOT NULL,
    status          VARCHAR(32)     NOT NULL,
    due_date        DATETIME(6)     NOT NULL,
    paid_at         DATETIME(6)     NULL,
    created_at      DATETIME(6)     NOT NULL DEFAULT CURRENT_TIMESTAMP);
CREATE INDEX idx_inv_subscription ON invoices(subscription_id);
CREATE INDEX idx_inv_status ON invoices(status);

CREATE TABLE IF NOT EXISTS invoice_lines (
    invoice_id      VARCHAR(64)     NOT NULL,
    description     TEXT            NOT NULL,
    amount_cents    INT             NOT NULL,
    quantity        INT             NOT NULL,
    PRIMARY KEY (invoice_id, description(128)),
    FOREIGN KEY (invoice_id) REFERENCES invoices(id) ON DELETE CASCADE
);
