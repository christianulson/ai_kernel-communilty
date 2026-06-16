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
