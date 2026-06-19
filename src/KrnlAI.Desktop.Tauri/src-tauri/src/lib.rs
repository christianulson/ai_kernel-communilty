use tauri::Manager;

mod audio;
mod camera;
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
        .plugin(tauri_plugin_deep_link::init())
        .plugin(tauri_plugin_autostart::init(
            tauri_plugin_autostart::MacosLauncher::LaunchAgent,
            Some(vec!["--flag1"]),
        ))
        .plugin(tauri_plugin_window_state::Builder::new().build())
        // .plugin(tauri_plugin_authenticator::init()) // requires OpenSSL + Perl
        .manage(sidecar::SidecarManager::new())
        .manage(audio::AudioCapture::new())
        .manage(camera::CameraCapture::new())
        .manage(commands::TerminalProcessManager::new())
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
            commands::execute_cli_stream,
            commands::cancel_cli_execution,
            commands::authenticate_with_biometric,
            commands::start_audio_capture,
            commands::stop_audio_capture,
            commands::play_audio,
            commands::get_audio_status,
            commands::start_camera,
            commands::stop_camera,
            commands::get_camera_status,
            commands::detect_faces,
            commands::get_available_cameras,
            updater::check_for_updates,
            updater::install_update,
        ])
        .run(tauri::generate_context!())
        .expect("error while running tauri application");
}
