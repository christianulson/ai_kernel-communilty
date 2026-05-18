mod commands;
mod notifications;
mod sidecar;
mod tray;

pub fn run() {
    env_logger::init();

    tauri::Builder::default()
        .plugin(tauri_plugin_shell::init())
        .plugin(tauri_plugin_notification::init())
        .plugin(tauri_plugin_fs::init())
        .plugin(tauri_plugin_process::init())
        .manage(sidecar::SidecarManager::new())
        .setup(|app| {
            tray::TrayManager::setup(app)?;
            Ok(())
        })
        .invoke_handler(tauri::generate_handler![
            commands::check_health,
            commands::get_system_info,
            commands::start_sidecar,
            commands::stop_sidecar,
            commands::get_sidecar_status,
            commands::get_app_version,
        ])
        .run(tauri::generate_context!())
        .expect("error while running tauri application");
}
