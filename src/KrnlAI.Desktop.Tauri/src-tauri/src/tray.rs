use std::sync::Arc;
use tauri::{
    menu::{CheckMenuItem, Menu, MenuItem, PredefinedMenuItem},
    tray::TrayIconBuilder,
    App, Emitter, Manager,
};
use tauri_plugin_autostart::ManagerExt;

use crate::notifications;

fn autostart_label() -> &'static str {
    #[cfg(target_os = "macos")]
    { "Launch at Login" }
    #[cfg(target_os = "linux")]
    { "Start with Session" }
    #[cfg(windows)]
    { "Start with Windows" }
}

pub struct TrayManager;

pub struct TrayMenuItems {
    pub always_on_top: Arc<std::sync::Mutex<Option<CheckMenuItem<tauri::Wry>>>>,
    pub autostart: Arc<std::sync::Mutex<Option<CheckMenuItem<tauri::Wry>>>>,
}

impl TrayManager {
    pub fn setup(app: &App) -> Result<(), Box<dyn std::error::Error>> {
        let items = TrayMenuItems {
            always_on_top: Arc::new(std::sync::Mutex::new(None)),
            autostart: Arc::new(std::sync::Mutex::new(None)),
        };
        let items_clone = TrayMenuItems {
            always_on_top: items.always_on_top.clone(),
            autostart: items.autostart.clone(),
        };
        app.manage(items);

        let is_autostart = app.autolaunch().is_enabled().unwrap_or(false);
        let app_version = env!("CARGO_PKG_VERSION");

        let open = MenuItem::with_id(app, "open", "Open", true, None::<&str>)?;
        let listen = MenuItem::with_id(app, "toggle-listening", "Listen", true, None::<&str>)?;
        let search = MenuItem::with_id(app, "search", "Search", true, None::<&str>)?;
        let always_on_top = CheckMenuItem::with_id(
            app,
            "toggle-always-on-top",
            "Always on Top",
            true,
            false,
            None::<&str>,
        )?;
        let autostart_item = CheckMenuItem::with_id(
            app,
            "toggle-autostart",
            autostart_label(),
            true,
            is_autostart,
            None::<&str>,
        )?;
        let command_palette = MenuItem::with_id(
            app, "command-palette", "Command Palette", true, None::<&str>,
        )?;
        let settings = MenuItem::with_id(app, "settings", "Settings", true, None::<&str>)?;
        let separator = PredefinedMenuItem::separator(app)?;
        let check_updates = MenuItem::with_id(
            app, "check-updates", "Check for Updates", true, None::<&str>,
        )?;
        let quit = MenuItem::with_id(app, "quit", "Quit", true, None::<&str>)?;

        let menu = Menu::with_items(
            app,
            &[
                &open,
                &listen,
                &search,
                &always_on_top,
                &autostart_item,
                &command_palette,
                &settings,
                &separator,
                &check_updates,
                &quit,
            ],
        )?;

        *items_clone.always_on_top.lock().unwrap() = Some(always_on_top);
        *items_clone.autostart.lock().unwrap() = Some(autostart_item);

        TrayIconBuilder::new()
            .menu(&menu)
            .tooltip(format!("Krnl-AI Desktop v{}", app_version))
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
                        if let Some(window) = app.get_webview_window("main") {
                            let is_on_top = window.is_always_on_top().unwrap_or(false);
                            let _ = window.set_always_on_top(!is_on_top);
                            let tray_items = app.state::<TrayMenuItems>();
                            let guard = tray_items.always_on_top.lock().unwrap();
                            if let Some(ref item) = *guard {
                                let _ = item.set_checked(!is_on_top);
                            }
                            let _ = window.emit("toggle-always-on-top", !is_on_top);
                        }
                    }
                    "toggle-autostart" => {
                        let tray_items = app.state::<TrayMenuItems>();
                        let guard = tray_items.autostart.lock().unwrap();
                        if let Some(ref item) = *guard {
                            let is_checked = item.is_checked().unwrap_or(false);
                            if is_checked {
                                let _ = app.autolaunch().disable();
                                let _ = item.set_checked(false);
                            } else {
                                let _ = app.autolaunch().enable();
                                let _ = item.set_checked(true);
                            }
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
                                &handle,
                                "Krnl-AI",
                                "Opening settings...",
                            )
                            .await;
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
                                &handle,
                                "Krnl-AI",
                                "Checking for updates...",
                            )
                            .await;
                        });
                        if let Some(window) = app.get_webview_window("main") {
                            let _ = window.emit("check-for-updates", ());
                        }
                    }
                    "quit" => {
                        let handle = app.app_handle().clone();
                        tauri::async_runtime::spawn(async move {
                            let _ = notifications::send_notification(
                                &handle,
                                "Krnl-AI",
                                "Shutting down...",
                            )
                            .await;
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
