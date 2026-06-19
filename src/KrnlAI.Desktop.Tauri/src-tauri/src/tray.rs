use tauri::{
    menu::{Menu, MenuItem, PredefinedMenuItem},
    tray::TrayIconBuilder,
    App, Emitter, Manager,
};

use crate::notifications;

pub struct TrayManager;

impl TrayManager {
    pub fn setup(app: &App) -> Result<(), Box<dyn std::error::Error>> {
        let open = MenuItem::with_id(app, "open", "Open", true, None::<&str>)?;
        let listen = MenuItem::with_id(app, "toggle-listening", "Listen", true, None::<&str>)?;
        let search = MenuItem::with_id(app, "search", "Search", true, None::<&str>)?;
        let always_on_top = MenuItem::with_id(app, "toggle-always-on-top", "Always on Top", true, None::<&str>)?;
        let command_palette = MenuItem::with_id(app, "command-palette", "Command Palette", true, None::<&str>)?;
        let settings = MenuItem::with_id(app, "settings", "Settings", true, None::<&str>)?;
        let separator = PredefinedMenuItem::separator(app)?;
        let check_updates = MenuItem::with_id(app, "check-updates", "Check for Updates", true, None::<&str>)?;
        let quit = MenuItem::with_id(app, "quit", "Quit", true, None::<&str>)?;

        let menu = Menu::with_items(app, &[&open, &listen, &search, &always_on_top, &command_palette, &settings, &separator, &check_updates, &quit])?;

        TrayIconBuilder::new()
            .menu(&menu)
            .on_menu_event(move |app, event| {
                match event.id.as_ref() {
                    "open" => {
                        if let Some(window) = app.get_webview_window("main") {
                            let _ = window.show();
                            let _ = window.set_focus();
                        }
                    }
                    "toggle-listening" => {
                        if let Some(window) = app.get_webview_window("main") {
                            let _ = window.emit("toggle-listening", ());
                            let _ = window.show();
                            let _ = window.set_focus();
                        }
                    }
                    "search" => {
                        if let Some(window) = app.get_webview_window("main") {
                            let _ = window.emit("open-search", ());
                            let _ = window.show();
                            let _ = window.set_focus();
                        }
                    }
                    "toggle-always-on-top" => {
                        let handle = app.app_handle().clone();
                        tauri::async_runtime::spawn(async move {
                            let _ = notifications::send_notification(
                                &handle, "Krnl-AI", "Toggling always on top..."
                            ).await;
                        });
                        if let Some(window) = app.get_webview_window("main") {
                            let _ = window.emit("toggle-always-on-top", ());
                        }
                    }
                    "command-palette" => {
                        if let Some(window) = app.get_webview_window("main") {
                            let _ = window.emit("open-command-palette", ());
                            let _ = window.show();
                            let _ = window.set_focus();
                        }
                    }
                    "settings" => {
                        let handle = app.app_handle().clone();
                        tauri::async_runtime::spawn(async move {
                            let _ = notifications::send_notification(
                                &handle, "Krnl-AI", "Opening settings..."
                            ).await;
                        });
                        if let Some(window) = app.get_webview_window("main") {
                            let _ = window.emit("navigate", "/settings");
                            let _ = window.show();
                            let _ = window.set_focus();
                        }
                    }
                    "check-updates" => {
                        let handle = app.app_handle().clone();
                        tauri::async_runtime::spawn(async move {
                            let _ = notifications::send_notification(
                                &handle, "Krnl-AI", "Checking for updates..."
                            ).await;
                        });
                        if let Some(window) = app.get_webview_window("main") {
                            let _ = window.emit("check-for-updates", ());
                        }
                    }
                    "quit" => {
                        let handle = app.app_handle().clone();
                        tauri::async_runtime::spawn(async move {
                            let _ = notifications::send_notification(
                                &handle, "Krnl-AI", "Shutting down..."
                            ).await;
                        });
                        app.exit(0);
                    }
                    _ => {}
                }
            })
            .build(app)?;

        Ok(())
    }
}
