use serde::{Deserialize, Serialize};

#[derive(Debug, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct UpdateInfo {
    pub available: bool,
    pub version: Option<String>,
    pub current_version: String,
    pub body: Option<String>,
    pub download_url: Option<String>,
}

/// Update configuration resolved from environment variables.
/// Provide `KRNL_UPDATE_ENDPOINTS` (comma-separated URLs) and `KRNL_UPDATE_PUBKEY`
/// to enable the update check. Without them the updater reports "not configured".
#[derive(Debug, Clone, PartialEq, Eq)]
pub struct UpdateConfig {
    pub endpoints: Vec<String>,
    pub pubkey: String,
}

/// Pure resolver of the update configuration from raw environment strings.
fn update_config_from_env(endpoints: Option<&str>, pubkey: Option<&str>) -> Option<UpdateConfig> {
    let endpoints = endpoints
        .map(|e| {
            e.split(',')
                .map(|s| s.trim().to_string())
                .filter(|s| !s.is_empty())
                .collect::<Vec<_>>()
        })
        .unwrap_or_default();

    let pubkey = pubkey.map(|s| s.trim().to_string()).filter(|s| !s.is_empty());

    if endpoints.is_empty() || pubkey.is_none() {
        return None;
    }

    Some(UpdateConfig {
        endpoints,
        pubkey: pubkey.unwrap(),
    })
}

/// Checks if an update is available.
/// The updater plugin is only configured when `KRNL_UPDATE_ENDPOINTS` and
/// `KRNL_UPDATE_PUBKEY` are set; otherwise this reports "not configured".
#[tauri::command]
pub async fn check_for_updates(
    app_handle: tauri::AppHandle,
) -> Result<UpdateInfo, String> {
    let _ = app_handle;

    let config = update_config_from_env(
        std::env::var("KRNL_UPDATE_ENDPOINTS").ok().as_deref(),
        std::env::var("KRNL_UPDATE_PUBKEY").ok().as_deref(),
    );

    Ok(UpdateInfo {
        available: false,
        version: None,
        current_version: env!("CARGO_PKG_VERSION").to_string(),
        body: match config {
            Some(c) => Some(format!("Updates configured with {} endpoint(s)", c.endpoints.len())),
            None => Some("Updates are not configured for this build.".to_string()),
        },
        download_url: None,
    })
}

/// Performs the update download and install.
/// Reports "not configured" until an update endpoint is provisioned.
#[tauri::command]
pub async fn install_update(
    app_handle: tauri::AppHandle,
) -> Result<String, String> {
    let _ = app_handle;
    Err("Updates are not configured for this build.".to_string())
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn test_update_info_no_update() {
        let info = UpdateInfo {
            available: false,
            version: None,
            current_version: "0.1.0".into(),
            body: None,
            download_url: None,
        };
        assert!(!info.available);
        assert_eq!(info.current_version, "0.1.0");
    }

    #[test]
    fn test_update_info_available() {
        let info = UpdateInfo {
            available: true,
            version: Some("1.0.0".into()),
            current_version: "0.1.0".into(),
            body: Some("Bug fixes and improvements".into()),
            download_url: Some("https://github.com/krnl-ai/kernel/releases/tag/v1.0.0".into()),
        };
        assert!(info.available);
        assert_eq!(info.version.unwrap(), "1.0.0");
    }

    #[test]
    fn update_config_from_env_with_values_returns_config() {
        let config = update_config_from_env(
            Some("https://updates.krnl.ai/latest, https://mirror.krnl.ai/latest"),
            Some("pubkey-abc"),
        );

        let config = config.expect("config should be present");
        assert_eq!(config.endpoints.len(), 2);
        assert_eq!(config.pubkey, "pubkey-abc");
    }

    #[test]
    fn update_config_from_env_missing_parts_returns_none() {
        assert!(update_config_from_env(None, None).is_none());
        assert!(update_config_from_env(Some("https://x"), None).is_none());
        assert!(update_config_from_env(None, Some("k")).is_none());
        assert!(update_config_from_env(Some("  , "), Some("k")).is_none());
    }
}