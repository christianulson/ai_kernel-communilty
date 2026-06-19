use tauri::{AppHandle, Emitter, Manager, Runtime};
use tauri_plugin_global_shortcut::{Code, GlobalShortcutExt, Modifiers, Shortcut, ShortcutState};

pub fn setup_hotkeys<R: Runtime>(app: &AppHandle<R>) -> Result<(), Box<dyn std::error::Error>> {
    let handle = app.clone();

    let shortcuts = [
        Shortcut::new(Some(Modifiers::ALT | Modifiers::CONTROL), Code::KeyK),
        Shortcut::new(Some(Modifiers::CONTROL | Modifiers::SHIFT), Code::KeyK),
        Shortcut::new(Some(Modifiers::CONTROL | Modifiers::SHIFT), Code::KeyT),
        Shortcut::new(Some(Modifiers::CONTROL), Code::KeyK),
        Shortcut::new(Some(Modifiers::CONTROL | Modifiers::SHIFT), Code::KeyP),
    ];

    app.global_shortcut().on_shortcuts(shortcuts, move |_app, shortcut, event| {
        if event.state != ShortcutState::Pressed {
            return;
        }

        if let Some(window) = handle.get_webview_window("main") {
            match (shortcut.mods, shortcut.key) {
                (mods, Code::KeyK) if mods == (Modifiers::ALT | Modifiers::CONTROL) => {
                    let _ = window.show();
                    let _ = window.set_focus();
                }
                (mods, Code::KeyK) if mods == (Modifiers::CONTROL | Modifiers::SHIFT) => {
                    let _ = window.emit("toggle-listening", ());
                }
                (mods, Code::KeyT) if mods == (Modifiers::CONTROL | Modifiers::SHIFT) => {
                    let _ = window.emit("toggle-always-on-top", ());
                }
                (mods, Code::KeyK) if mods == Modifiers::CONTROL => {
                    let _ = window.emit("open-search", ());
                }
                (mods, Code::KeyP) if mods == (Modifiers::CONTROL | Modifiers::SHIFT) => {
                    let _ = window.emit("open-command-palette", ());
                }
                _ => {}
            }
        }
    })?;

    Ok(())
}
