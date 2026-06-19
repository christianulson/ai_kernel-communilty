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
        .plugin(tauri_plugin_single_instance::init(|app, _argv, _cwd| {
            let _ = app.get_webview_window("main").map(|w| w.set_focus());
        }))
        .plugin(tauri_plugin_clipboard_manager::init())
        .plugin(tauri_plugin_drag_drop::init())
        .plugin(tauri_plugin_deep_link::init({
            move |app, request| {
                let _ = app.emit("deep-link-received", request.to_string());
            }
        }))
        .plugin(tauri_plugin_autostart::init(
            tauri_plugin_autostart::MacosLauncher::LaunchAgent,
            Some(vec!["--flag1"]),
        ))
        .plugin(tauri_plugin_window_state::Builder::new().build())
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
            commands::toggle_always_on_top,
            commands::copy_to_clipboard,
            commands::get_deep_link,
            commands::get_detailed_system_info,
            commands::backup_data,
            commands::restore_data,
            commands::execute_cli,
            commands::execute_cli_with_workdir,
            updater::check_for_updates,
            updater::install_update,
        ])
        .run(tauri::generate_context!())
        .expect("error while running tauri application");
}
