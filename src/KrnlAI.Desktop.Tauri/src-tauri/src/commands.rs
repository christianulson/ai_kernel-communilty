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

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn test_health_status_fields() {
        let h = HealthStatus { status: "ok".into(), version: "0.1.0".into(), sidecar_running: false };
        assert_eq!(h.status, "ok");
        assert_eq!(h.version, "0.1.0");
        assert!(!h.sidecar_running);
    }

    #[test]
    fn test_system_info_fields() {
        let s = SystemInfo { os: "linux".into(), arch: "x86_64".into(), cpu_cores: "4".into(), memory_gb: "16.0".into() };
        assert_eq!(s.os, "linux");
        assert_eq!(s.memory_gb, "16.0");
    }

    #[test]
    fn test_sidecar_status_fields() {
        let s = SidecarStatus { running: true, pid: Some(1234), port: 5001 };
        assert!(s.running);
        assert_eq!(s.pid, Some(1234));
        assert_eq!(s.port, 5001);
    }

    #[test]
    fn test_app_version_is_not_empty() {
        let v = env!("CARGO_PKG_VERSION");
        assert!(!v.is_empty());
    }

    #[test]
    fn test_memory_in_gb_returns_string() {
        let result = memory_in_gb();
        assert!(!result.is_empty());
        assert!(result.ends_with(".0") || result.contains('.') || result == "unknown");
    }
}
