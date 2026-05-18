pub async fn send_notification(
    app: &tauri::AppHandle,
    title: &str,
    body: &str,
) -> Result<(), Box<dyn std::error::Error>> {
    use tauri_plugin_notification::NotificationExt;

    app.notification()
        .builder()
        .title(title)
        .body(body)
        .show()
        .map_err(|e| format!("Notification error: {}", e))?;

    Ok(())
}
