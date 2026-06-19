use base64::Engine;
use serde::{Deserialize, Serialize};
use std::sync::Arc;
use tauri::{Emitter, Manager, State};
use tauri_plugin_clipboard_manager::ClipboardExt;
use tokio::io::AsyncBufReadExt;
use tokio::sync::Mutex;
use crate::audio::AudioCapture;
use crate::camera::{CameraCapture, CameraInfo, FaceRect};
use crate::sidecar::SidecarManager;

#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct BiometricAuthResult {
    pub success: bool,
    pub token: Option<String>,
    pub error: Option<String>,
}

#[tauri::command]
pub async fn authenticate_with_biometric(
    _app: tauri::AppHandle,
    window: tauri::Window,
) -> Result<BiometricAuthResult, String> {
    let result = platform_authenticate().await?;
    let _ = window.emit("biometric-auth-result", &result);
    Ok(result)
}

async fn platform_authenticate() -> Result<BiometricAuthResult, String> {
    #[cfg(target_os = "windows")]
    {
        return windows_hello_authenticate().await;
    }

    #[cfg(target_os = "macos")]
    {
        return touch_id_authenticate().await;
    }

    #[cfg(target_os = "linux")]
    {
        return Ok(BiometricAuthResult {
            success: false,
            token: None,
            error: Some("Biometric authentication is not available on this platform".into()),
        });
    }

    #[cfg(not(any(target_os = "windows", target_os = "macos", target_os = "linux")))]
    {
        return Ok(BiometricAuthResult {
            success: false,
            token: None,
            error: Some("Unsupported platform".into()),
        });
    }
}

#[cfg(target_os = "windows")]
async fn windows_hello_authenticate() -> Result<BiometricAuthResult, String> {
    let script = r#"
Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing

try {
    $result = [Windows.Security.Credentials.UI.UserConsentVerifier]::VerifyUserAsync("Krnl-AI requires authentication").GetAwaiter().GetResult()
    if ($result -eq "Verified") {
        Write-Output "SUCCESS"
    } else {
        Write-Output "FAILED:$($result)"
    }
} catch {
    Write-Output "ERROR:$($_.Exception.Message)"
}
"#;

    let output = tokio::process::Command::new("powershell")
        .args(["-NoProfile", "-NonInteractive", "-Command", script])
        .output()
        .await
        .map_err(|e| format!("Failed to start PowerShell: {}", e))?;

    let stdout = String::from_utf8_lossy(&output.stdout).trim().to_string();
    let stderr = String::from_utf8_lossy(&output.stderr).trim().to_string();

    if stdout.starts_with("SUCCESS") {
        use rand::Rng;
        let mut bytes = [0u8; 32];
        rand::thread_rng().fill(&mut bytes);
        let token = base64::engine::general_purpose::STANDARD.encode(&bytes);
        Ok(BiometricAuthResult {
            success: true,
            token: Some(token),
            error: None,
        })
    } else if let Some(err) = stdout.strip_prefix("FAILED:") {
        Ok(BiometricAuthResult {
            success: false,
            token: None,
            error: Some(format!("Windows Hello authentication failed: {}", err)),
        })
    } else if let Some(err) = stdout.strip_prefix("ERROR:") {
        Ok(BiometricAuthResult {
            success: false,
            token: None,
            error: Some(format!("Windows Hello error: {}", err)),
        })
    } else {
        Ok(BiometricAuthResult {
            success: false,
            token: None,
            error: Some(format!("Windows Hello unavailable: {}", if stderr.is_empty() { "No biometric hardware found or not configured".to_string() } else { stderr })),
        })
    }
}

#[cfg(target_os = "macos")]
async fn touch_id_authenticate() -> Result<BiometricAuthResult, String> {
    let output = tokio::process::Command::new("security")
        .args(["authorize-touchid"])
        .output()
        .await
        .map_err(|e| format!("Failed to start security CLI: {}", e))?;

    if output.status.success() {
        use rand::Rng;
        let mut bytes = [0u8; 32];
        rand::thread_rng().fill(&mut bytes);
        let token = base64::engine::general_purpose::STANDARD.encode(&bytes);
        Ok(BiometricAuthResult {
            success: true,
            token: Some(token),
            error: None,
        })
    } else {
        let stderr = String::from_utf8_lossy(&output.stderr).trim().to_string();
        if stderr.contains("unavailable") || stderr.contains("not available") {
            Ok(BiometricAuthResult {
                success: false,
                token: None,
                error: Some("Touch ID is not available on this device".into()),
            })
        } else {
            Ok(BiometricAuthResult {
                success: false,
                token: None,
                error: Some(if stderr.is_empty() { "Touch ID authentication failed or was cancelled".into() } else { stderr }),
            })
        }
    }
}

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
pub fn toggle_always_on_top(
    window: tauri::Window,
    app: tauri::AppHandle,
) -> Result<bool, String> {
    let is_on_top = window.is_always_on_top().map_err(|e| e.to_string())?;
    window
        .set_always_on_top(!is_on_top)
        .map_err(|e| e.to_string())?;
    if let Some(tray_items) = app.try_state::<crate::tray::TrayMenuItems>() {
        if let Some(ref item) = *tray_items.always_on_top.lock().unwrap() {
            let _ = item.set_checked(!is_on_top);
        }
    }
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
pub async fn copy_to_clipboard(text: String, app: tauri::AppHandle) -> Result<(), String> {
    app.clipboard().write_text(text).map_err(|e| e.to_string())
}

#[tauri::command]
pub fn get_deep_link(_app: tauri::AppHandle) -> Option<String> {
    None
}

#[tauri::command]
pub async fn get_detailed_system_info() -> Result<serde_json::Value, String> {
    let info = sysinfo::System::new_all();
    Ok(serde_json::json!({
        "total_memory": info.total_memory(),
        "used_memory": info.used_memory(),
        "total_swap": info.total_swap(),
        "used_swap": info.used_swap(),
        "cpu_count": info.cpus().len(),
        "cpu_brand": info.cpus().first().map(|c| c.brand().to_string()),
    }))
}

#[tauri::command]
pub async fn backup_data(sidecar: tauri::State<'_, SidecarManager>) -> Result<String, String> {
    let port = sidecar.get_port();
    let url = format!("http://127.0.0.1:{}/api/backup/create", port);
    let client = reqwest::Client::new();
    let resp = client.post(&url).send().await.map_err(|e| e.to_string())?;
    let text = resp.text().await.map_err(|e| e.to_string())?;
    Ok(text)
}

#[tauri::command]
pub async fn restore_data(path: String, sidecar: tauri::State<'_, SidecarManager>) -> Result<String, String> {
    let port = sidecar.get_port();
    let url = format!("http://127.0.0.1:{}/api/backup/restore", port);
    let client = reqwest::Client::new();
    let resp = client.post(&url)
        .json(&serde_json::json!({ "path": path }))
        .send().await.map_err(|e| e.to_string())?;
    let text = resp.text().await.map_err(|e| e.to_string())?;
    Ok(text)
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

// ── Terminal Process Manager ─────────────────────────────────────────────────

pub struct TerminalProcessManager {
    child: Arc<Mutex<Option<tokio::process::Child>>>,
}

impl TerminalProcessManager {
    pub fn new() -> Self {
        Self { child: Arc::new(Mutex::new(None)) }
    }
}

#[tauri::command]
pub async fn execute_cli_stream(
    app_handle: tauri::AppHandle,
    command: String,
    workdir: Option<String>,
    manager: State<'_, TerminalProcessManager>,
) -> Result<(), String> {
    // Cancel any existing process first
    {
        let mut child_lock = manager.child.lock().await;
        if let Some(mut child) = child_lock.take() {
            let _ = child.kill().await;
            let _ = child.wait().await;
        }
    }

    let mut cmd = if cfg!(target_os = "windows") {
        let mut c = tokio::process::Command::new("cmd");
        c.arg("/C");
        c.arg(&command);
        c
    } else {
        let mut c = tokio::process::Command::new("sh");
        c.arg("-c");
        c.arg(&command);
        c
    };

    if let Some(ref wd) = workdir {
        cmd.current_dir(wd);
    }
    cmd.stdout(std::process::Stdio::piped());
    cmd.stderr(std::process::Stdio::piped());

    let mut child = cmd.spawn().map_err(|e| format!("Failed to spawn process: {}", e))?;

    let stdout = child.stdout.take().ok_or_else(|| "Failed to capture stdout".to_string())?;
    let stderr = child.stderr.take().ok_or_else(|| "Failed to capture stderr".to_string())?;

    // Store child for cancellation
    {
        let mut child_lock = manager.child.lock().await;
        *child_lock = Some(child);
    }

    let ah = app_handle.clone();
    let stdout_task = tokio::spawn(async move {
        let reader = tokio::io::BufReader::new(stdout);
        let mut lines = reader.lines();
        while let Ok(Some(line)) = lines.next_line().await {
            let _ = ah.emit("terminal-output", &line);
        }
    });

    let ah = app_handle.clone();
    let stderr_task = tokio::spawn(async move {
        let reader = tokio::io::BufReader::new(stderr);
        let mut lines = reader.lines();
        while let Ok(Some(line)) = lines.next_line().await {
            let _ = ah.emit("terminal-error", &line);
        }
    });

    // Take child back and wait for completion
    let mut child = manager.child.lock().await.take();
    let exit_code = match child.as_mut() {
        Some(c) => c.wait().await.map(|s| s.code()).ok().flatten().unwrap_or(-1),
        None => -1,
    };
    let _ = app_handle.emit("terminal-exit", exit_code);

    // Wait for output readers to finish
    let _ = stdout_task.await;
    let _ = stderr_task.await;

    Ok(())
}

#[tauri::command]
pub async fn cancel_cli_execution(
    manager: State<'_, TerminalProcessManager>,
) -> Result<(), String> {
    let mut child_lock = manager.child.lock().await;
    if let Some(mut child) = child_lock.take() {
        let _ = child.kill().await;
        let _ = child.wait().await;
    }
    Ok(())
}

// ── Audio Commands ──────────────────────────────────────────────────────────

#[tauri::command]
pub async fn start_audio_capture(
    app_handle: tauri::AppHandle,
    audio: State<'_, AudioCapture>,
) -> Result<(), String> {
    audio.start_capture(app_handle)
}

#[tauri::command]
pub async fn stop_audio_capture(audio: State<'_, AudioCapture>) -> Result<(), String> {
    audio.stop_capture()
}

#[tauri::command]
pub async fn play_audio(
    data: Vec<u8>,
    audio: State<'_, AudioCapture>,
) -> Result<(), String> {
    audio.play_audio(data)
}

#[tauri::command]
pub fn get_audio_status(audio: State<'_, AudioCapture>) -> Result<bool, String> {
    Ok(audio.is_running())
}

// ── Camera Commands ─────────────────────────────────────────────────────────

#[tauri::command]
pub async fn start_camera(
    app_handle: tauri::AppHandle,
    camera: State<'_, CameraCapture>,
) -> Result<(), String> {
    camera.start_camera(app_handle)
}

#[tauri::command]
pub async fn stop_camera(camera: State<'_, CameraCapture>) -> Result<(), String> {
    camera.stop_camera()
}

#[tauri::command]
pub fn get_camera_status(camera: State<'_, CameraCapture>) -> Result<bool, String> {
    Ok(camera.is_running())
}

#[tauri::command]
pub fn detect_faces(image_data: Vec<u8>) -> Result<Vec<FaceRect>, String> {
    crate::camera::detect_faces(image_data)
}

#[tauri::command]
pub fn get_available_cameras() -> Result<Vec<CameraInfo>, String> {
    crate::camera::list_cameras()
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
