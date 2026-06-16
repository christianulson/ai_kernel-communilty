mod commands;
mod hotkey;
mod notifications;
mod sidecar;
mod tray;
mod updater;

pub fn run() {
    let _ = env_logger::try_init();

    tauri::Builder::default()
        .plugin(tauri_plugin_shell::init())
        .plugin(tauri_plugin_notification::init())
        .plugin(tauri_plugin_fs::init())
        .plugin(tauri_plugin_process::init())
        .plugin(tauri_plugin_global_shortcut::Builder::new().build())
        .plugin(tauri_plugin_updater::Builder::new().build())
        .plugin(tauri_plugin_deep_link::init())
        .manage(sidecar::SidecarManager::new())
        .setup(|app| {
            tray::TrayManager::setup(app)?;
            hotkey::setup_hotkeys(app.handle())?;
            Ok(())
        })
        .invoke_handler(tauri::generate_handler![
            commands::check_health,
            commands::get_system_info,
            commands::start_sidecar,
            commands::stop_sidecar,
            commands::get_sidecar_status,
            commands::get_app_version,
            commands::get_os_theme,
            commands::open_external,
            commands::show_save_dialog,
            commands::show_open_dialog,
            updater::check_for_updates,
            updater::install_update,
        ])
        .run(tauri::generate_context!())
        .expect("error while running tauri application");
}
