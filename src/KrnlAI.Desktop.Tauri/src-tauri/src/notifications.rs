use tauri::AppHandle;
use tauri_plugin_notification::NotificationExt;

#[derive(Clone, serde::Serialize)]
pub struct NotificationAction {
    pub id: String,
    pub label: String,
}

/// Send a native notification
pub async fn send_notification(
    app: &AppHandle,
    title: &str,
    body: &str,
) -> Result<(), String> {
    app.notification()
        .builder()
        .title(title)
        .body(body)
        .show()
        .map_err(|e| format!("Notification error: {}", e))?;

    Ok(())
}

/// Send a notification with diagnostic info
pub async fn send_diagnostics_notification(
    app: &AppHandle,
    status: &str,
    check_count: usize,
) -> Result<(), String> {
    send_notification(
        app,
        "Krnl-AI Diagnostics",
        &format!("{} — {} checks completed", status, check_count),
    ).await
}

/// Send a notification that the sidecar was restarted
pub async fn send_sidecar_restart_notification(
    app: &AppHandle,
) -> Result<(), String> {
    send_notification(
        app,
        "Krnl-AI Sidecar",
        "Sidecar was unresponsive and has been restarted automatically.",
    ).await
}

/// Send a notification about an available update
pub async fn send_update_notification(
    app: &AppHandle,
    version: &str,
) -> Result<(), String> {
    send_notification(
        app,
        "Krnl-AI Update Available",
        &format!("Version {} is ready to install.", version),
    ).await
}
