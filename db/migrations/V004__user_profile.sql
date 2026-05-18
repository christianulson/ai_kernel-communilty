CREATE TABLE IF NOT EXISTS user_profiles (
  user_id         VARCHAR(64) NOT NULL,
  display_name    VARCHAR(128) NOT NULL,
  preferences_json TEXT NOT NULL,
  updated_at      DATETIME(6) NOT NULL);
