CREATE INDEX idx_policy_rollbacks_operator
  ON kernel_policy_rollbacks (performed_by, rollback_id);
