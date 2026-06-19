use serde::{Deserialize, Serialize};
use tauri::State;
use crate::sidecar::SidecarManager;

#[derive(Debug, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct HealthStatus {
    pub status: String,
    pub version: String,
    pub sidecar_running: bool,
}

#[derive(Debug, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct SystemInfo {
    pub os: String,
    pub arch: String,
    pub cpu_cores: String,
    pub memory_gb: String,
}

#[derive(Debug, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct SidecarStatus {
    pub running: bool,
    pub pid: Option<u32>,
    pub port: u16,
}

#[derive(Debug, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct ThemeResult {
    pub theme: String,
}

#[tauri::command]
pub async fn check_health(sidecar: State<'_, SidecarManager>) -> Result<HealthStatus, String> {
    Ok(HealthStatus {
        status: "ok".to_string(),
        version: env!("CARGO_PKG_VERSION").to_string(),
        sidecar_running: sidecar.is_running(),
    })
}

#[tauri::command]
pub async fn get_system_info() -> Result<SystemInfo, String> {
    let memory = memory_in_gb();
    Ok(SystemInfo {
        os: std::env::consts::OS.to_string(),
        arch: std::env::consts::ARCH.to_string(),
        cpu_cores: std::thread::available_parallelism()
            .map(|n| n.get().to_string())
            .unwrap_or_else(|_| "unknown".to_string()),
        memory_gb: memory,
    })
}

fn memory_in_gb() -> String {
    use sysinfo::System;
    let mut sys = System::new();
    sys.refresh_memory();
    let total = sys.total_memory();
    if total == 0 {
        return "unknown".to_string();
    }
    format!("{:.1}", total as f64 / (1024.0 * 1024.0 * 1024.0))
}

#[tauri::command]
pub async fn start_sidecar(sidecar: State<'_, SidecarManager>) -> Result<SidecarStatus, String> {
    sidecar.start().await.map_err(|e| e.to_string())?;
    Ok(SidecarStatus { running: true, pid: sidecar.get_pid(), port: sidecar.get_port() })
}

#[tauri::command]
pub async fn stop_sidecar(sidecar: State<'_, SidecarManager>) -> Result<(), String> {
    sidecar.stop().await.map_err(|e| e.to_string())
}

#[tauri::command]
pub async fn get_sidecar_status(sidecar: State<'_, SidecarManager>) -> Result<SidecarStatus, String> {
    Ok(SidecarStatus { running: sidecar.is_running(), pid: sidecar.get_pid(), port: sidecar.get_port() })
}

#[tauri::command]
pub fn get_app_version() -> String {
    env!("CARGO_PKG_VERSION").to_string()
}

#[tauri::command]
pub fn get_os_theme() -> Result<ThemeResult, String> {
    let theme = get_system_theme();
    Ok(ThemeResult { theme })
}

fn get_system_theme() -> String {
    #[cfg(target_os = "windows")]
    {
        if let Ok(output) = std::process::Command::new("powershell")
            .args([
                "-Command",
                "Get-ItemProperty -Path 'HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize' -Name AppsUseLightTheme | Select-Object -ExpandProperty AppsUseLightTheme",
            ])
            .output()
        {
            let stdout = String::from_utf8_lossy(&output.stdout).trim().to_string();
            if stdout == "0" { return "dark".to_string(); }
            if stdout == "1" { return "light".to_string(); }
        }
    }

    #[cfg(target_os = "macos")]
    {
        if let Ok(output) = std::process::Command::new("defaults")
            .args(["read", "-g", "AppleInterfaceStyle"])
            .output()
        {
            let stdout = String::from_utf8_lossy(&output.stdout).trim().to_string();
            if stdout.eq_ignore_ascii_case("dark") { return "dark".to_string(); }
        }
        // If the key doesn't exist, it's Light mode
        return "light".to_string();
    }

    #[cfg(target_os = "linux")]
    {
        // Try GNOME first
        if let Ok(output) = std::process::Command::new("gsettings")
            .args(["get", "org.gnome.desktop.interface", "color-scheme"])
            .output()
        {
            let stdout = String::from_utf8_lossy(&output.stdout).trim().to_string();
            if stdout.contains("dark") { return "dark".to_string(); }
            return "light".to_string();
        }
        // Fallback: check GTK settings.ini
        if let Ok(home) = std::env::var("HOME") {
            let gtk_config = std::path::Path::new(&home).join(".config/gtk-3.0/settings.ini");
            if let Ok(content) = std::fs::read_to_string(gtk_config) {
                if content.contains("gtk-application-prefer-dark-theme=1") {
                    return "dark".to_string();
                }
            }
        }
    }

    "light".to_string()
}

#[tauri::command]
pub fn toggle_always_on_top(window: tauri::Window) -> Result<bool, String> {
    let is_on_top = window.is_always_on_top().map_err(|e| e.to_string())?;
    window.set_always_on_top(!is_on_top).map_err(|e| e.to_string())?;
    Ok(!is_on_top)
}

#[tauri::command]
pub fn open_external(url: String) -> Result<(), String> {
    open::that(&url).map_err(|e| format!("Failed to open URL: {}", e))
}

#[tauri::command]
pub fn show_save_dialog(default_name: String) -> Result<Option<String>, String> {
    let file = rfd::FileDialog::new()
        .set_file_name(&default_name)
        .save_file();
    Ok(file.map(|f| f.to_string_lossy().to_string()))
}

#[tauri::command]
pub fn show_open_dialog() -> Result<Option<String>, String> {
    let file = rfd::FileDialog::new().pick_file();
    Ok(file.map(|f| f.to_string_lossy().to_string()))
}

#[tauri::command]
async fn copy_to_clipboard(text: String, app: tauri::AppHandle) -> Result<(), String> {
    use tauri_plugin_clipboard_manager::ClipboardExt;
    app.clipboard().write_text(text).map_err(|e| e.to_string())
}

#[tauri::command]
fn get_deep_link(_app: tauri::AppHandle) -> Option<String> {
    None
}

#[tauri::command]
async fn get_detailed_system_info() -> Result<serde_json::Value, String> {
    let info = sysinfo::System::new_all();
    Ok(serde_json::json!({
        "total_memory": info.total_memory(),
        "used_memory": info.used_memory(),
        "total_swap": info.total_swap(),
        "used_swap": info.used_swap(),
        "cpu_count": info.cpus().len(),
        "cpu_brand": info.cpus().first().map(|c| c.brand().to_string()),
        "hostname": info.host_name(),
        "kernel_version": info.kernel_version(),
    }))
}

#[tauri::command]
pub async fn backup_data(app: tauri::AppHandle) -> Result<String, String> {
    let sidecar_port = get_sidecar_port(&app).await;
    let url = format!("http://127.0.0.1:{}/api/backup/create", sidecar_port);
    let client = reqwest::Client::new();
    let resp = client.post(&url).send().await.map_err(|e| e.to_string())?;
    let text = resp.text().await.map_err(|e| e.to_string())?;
    Ok(text)
}

#[tauri::command]
pub async fn restore_data(path: String, app: tauri::AppHandle) -> Result<String, String> {
    let sidecar_port = get_sidecar_port(&app).await;
    let url = format!("http://127.0.0.1:{}/api/backup/restore", sidecar_port);
    let client = reqwest::Client::new();
    let resp = client.post(&url)
        .json(&serde_json::json!({ "path": path }))
        .send().await.map_err(|e| e.to_string())?;
    let text = resp.text().await.map_err(|e| e.to_string())?;
    Ok(text)
}

async fn get_sidecar_port(app: &tauri::AppHandle) -> u16 {
    let manager = app.state::<SidecarManager>();
    manager.get_port()
}

#[tauri::command]
pub async fn execute_cli(command: String, args: Vec<String>) -> Result<String, String> {
    let output = std::process::Command::new(&command)
        .args(&args)
        .output()
        .map_err(|e| format!("Failed to execute command: {}", e))?;

    if output.status.success() {
        Ok(String::from_utf8_lossy(&output.stdout).to_string())
    } else {
        Err(String::from_utf8_lossy(&output.stderr).to_string())
    }
}

#[tauri::command]
pub async fn execute_cli_with_workdir(command: String, args: Vec<String>, workdir: String) -> Result<String, String> {
    let output = std::process::Command::new(&command)
        .args(&args)
        .current_dir(&workdir)
        .output()
        .map_err(|e| format!("Failed to execute command: {}", e))?;

    if output.status.success() {
        Ok(String::from_utf8_lossy(&output.stdout).to_string())
    } else {
        Err(String::from_utf8_lossy(&output.stderr).to_string())
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn test_health_status_fields() {
        let h = HealthStatus { status: "ok".into(), version: "0.1.0".into(), sidecar_running: false };
        assert_eq!(h.status, "ok");
        assert!(!h.sidecar_running);
    }

    #[test]
    fn test_system_info_fields() {
        let s = SystemInfo { os: "linux".into(), arch: "x86_64".into(), cpu_cores: "4".into(), memory_gb: "16.0".into() };
        assert_eq!(s.cpu_cores, "4");
    }

    #[test]
    fn test_sidecar_status_fields() {
        let s = SidecarStatus { running: true, pid: Some(1234), port: 5001 };
        assert!(s.running);
        assert_eq!(s.port, 5001);
    }

    #[test]
    fn test_app_version_is_not_empty() {
        let v = env!("CARGO_PKG_VERSION");
        assert!(!v.is_empty());
    }

    #[test]
    fn test_theme_result_struct() {
        let t = ThemeResult { theme: "dark".into() };
        assert_eq!(t.theme, "dark");
    }
}
