ALTER TABLE analog_shadow_observations
    ADD COLUMN signals_json TEXT NULL;

-- UPDATE analog_shadow_observations
-- SET signals_json = JSON_OBJECT(
--     'unipolarSignals', JSON_OBJECT(
--         'risk', analog_risk,
--         'uncertainty', analog_uncertainty,
--         'utility', analog_utility,
--         'confidence', analog_confidence,
--         'digitalGateScore', digital_gate_score,
--         'analogCompositeScore', analog_composite_score
--     ),
--     'bipolarSignals', JSON_OBJECT()
-- )
-- WHERE signals_json IS NULL;

-- SQLite does not support MODIFY COLUMN. Recreate the table instead.
-- Original: ALTER TABLE analog_shadow_observations
    MODIFY COLUMN signals_json TEXT NOT NULL;
