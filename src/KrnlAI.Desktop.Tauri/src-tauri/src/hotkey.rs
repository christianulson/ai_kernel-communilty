use tauri::{AppHandle, Emitter, Manager, Runtime};

pub fn setup_hotkeys<R: Runtime>(app: &AppHandle<R>) -> Result<(), Box<dyn std::error::Error>> {
    #[cfg(target_os = "windows")]
    {
        use tauri_plugin_global_shortcut::{Code, GlobalShortcutExt, Modifiers, Shortcut};

        let handle = app.clone();
        let shortcut = Shortcut::new(Some(Modifiers::ALT | Modifiers::CONTROL), Code::KeyK);

        app.global_shortcut().on_shortcut(shortcut, move |_app, _event| {
            if let Some(window) = handle.get_webview_window("main") {
                let _ = window.show();
                let _ = window.set_focus();
            }
        })?;

        app.global_shortcut().register(shortcut)?;
    }

    Ok(())
}
