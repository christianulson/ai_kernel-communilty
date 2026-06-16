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

/// Checks if an update is available by calling the Tauri updater plugin.
/// Returns structured update info for the frontend to display.
#[tauri::command]
pub async fn check_for_updates(
    app_handle: tauri::AppHandle,
) -> Result<UpdateInfo, String> {
    let current = env!("CARGO_PKG_VERSION").to_string();

    let updater = tauri_plugin_updater::UpdaterExt::updater(&app_handle)
        .map_err(|e| format!("Updater not available: {}", e))?;

    match updater.check().await {
        Ok(Some(update)) => Ok(UpdateInfo {
            available: true,
            version: Some(update.version.clone()),
            current_version: current,
            body: update.body.clone(),
            download_url: None,
        }),
        Ok(None) => Ok(UpdateInfo {
            available: false,
            version: None,
            current_version: current,
            body: None,
            download_url: None,
        }),
        Err(e) => Err(format!("Update check failed: {}", e)),
    }
}

/// Performs the actual update download and install.
/// The Tauri updater handles download, verification, and installation.
#[tauri::command]
pub async fn install_update(
    app_handle: tauri::AppHandle,
) -> Result<String, String> {
    let updater = tauri_plugin_updater::UpdaterExt::updater(&app_handle)
        .map_err(|e| format!("Updater not available: {}", e))?;

    match updater.check().await {
        Ok(Some(update)) => {
            update
                .download_and_install(
                    |_chunk_length, _content_length| {},
                    || {},
                )
                .await
                .map_err(|e| format!("Download failed: {}", e))?;
            Ok("Update installed. Restart the application to apply.".to_string())
        }
        Ok(None) => Err("No update available.".to_string()),
        Err(e) => Err(format!("Update check failed: {}", e)),
    }
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
