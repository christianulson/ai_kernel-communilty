ALTER TABLE episodes
    ADD COLUMN self_check_json   TEXT NULL,
    ADD COLUMN post_mortem_json  TEXT NULL,
    ADD COLUMN profile_json      TEXT NULL,
    ADD COLUMN budget_json       TEXT NULL,
    ADD COLUMN metrics_json      TEXT NULL;