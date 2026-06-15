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
        let settings = MenuItem::with_id(app, "settings", "Settings", true, None::<&str>)?;
        let separator = PredefinedMenuItem::separator(app)?;
        let quit = MenuItem::with_id(app, "quit", "Quit", true, None::<&str>)?;

        let menu = Menu::with_items(app, &[&open, &settings, &separator, &quit])?;

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
