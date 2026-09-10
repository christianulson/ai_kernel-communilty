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

/// Checks if an update is available.
/// The updater plugin is not configured (no endpoint/pubkey yet), so this
/// reports "not configured" until an update server is provisioned.
#[tauri::command]
pub async fn check_for_updates(
    app_handle: tauri::AppHandle,
) -> Result<UpdateInfo, String> {
    let _ = app_handle;
    Ok(UpdateInfo {
        available: false,
        version: None,
        current_version: env!("CARGO_PKG_VERSION").to_string(),
        body: None,
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
}