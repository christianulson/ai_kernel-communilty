use serde::{Deserialize, Serialize};
use tauri::State;

use crate::sidecar::SidecarManager;

#[derive(Debug, Serialize, Deserialize)]
pub struct HealthStatus {
    pub status: String,
    pub version: String,
    pub sidecar_running: bool,
}

#[derive(Debug, Serialize, Deserialize)]
pub struct SystemInfo {
    pub os: String,
    pub arch: String,
    pub cpu_cores: String,
    pub memory_gb: String,
}

#[derive(Debug, Serialize, Deserialize)]
pub struct SidecarStatus {
    pub running: bool,
    pub pid: Option<u32>,
    pub port: u16,
}

#[tauri::command]
pub async fn check_health(
    sidecar: State<'_, SidecarManager>,
) -> Result<HealthStatus, String> {
    Ok(HealthStatus {
        status: "ok".to_string(),
        version: env!("CARGO_PKG_VERSION").to_string(),
        sidecar_running: sidecar.is_running(),
    })
}

#[tauri::command]
pub async fn get_system_info() -> Result<SystemInfo, String> {
    Ok(SystemInfo {
        os: std::env::consts::OS.to_string(),
        arch: std::env::consts::ARCH.to_string(),
        cpu_cores: std::thread::available_parallelism()
            .map(|n| n.get().to_string())
            .unwrap_or_else(|_| "unknown".to_string()),
        memory_gb: "unknown".to_string(),
    })
}

#[tauri::command]
pub async fn start_sidecar(
    sidecar: State<'_, SidecarManager>,
) -> Result<SidecarStatus, String> {
    sidecar.start().await.map_err(|e| e.to_string())?;

    Ok(SidecarStatus {
        running: true,
        pid: sidecar.get_pid(),
        port: sidecar.get_port(),
    })
}

#[tauri::command]
pub async fn stop_sidecar(
    sidecar: State<'_, SidecarManager>,
) -> Result<(), String> {
    sidecar.stop().await.map_err(|e| e.to_string())
}

#[tauri::command]
pub async fn get_sidecar_status(
    sidecar: State<'_, SidecarManager>,
) -> Result<SidecarStatus, String> {
    Ok(SidecarStatus {
        running: sidecar.is_running(),
        pid: sidecar.get_pid(),
        port: sidecar.get_port(),
    })
}

#[tauri::command]
pub fn get_app_version() -> String {
    env!("CARGO_PKG_VERSION").to_string()
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn test_health_status_struct() {
        let status = HealthStatus {
            status: "ok".to_string(),
            version: "0.1.0".to_string(),
            sidecar_running: true,
        };
        assert_eq!(status.status, "ok");
        assert_eq!(status.version, "0.1.0");
        assert!(status.sidecar_running);
    }

    #[test]
    fn test_system_info_struct() {
        let info = SystemInfo {
            os: "windows".to_string(),
            arch: "x86_64".to_string(),
            cpu_cores: "8".to_string(),
            memory_gb: "16".to_string(),
        };
        assert_eq!(info.os, "windows");
        assert_eq!(info.arch, "x86_64");
        assert_eq!(info.cpu_cores, "8");
    }

    #[test]
    fn test_sidecar_status_struct() {
        let status = SidecarStatus {
            running: true,
            pid: Some(12345),
            port: 5001,
        };
        assert!(status.running);
        assert_eq!(status.pid, Some(12345));
        assert_eq!(status.port, 5001);
    }

    #[test]
    fn test_app_version_is_not_empty() {
        let version = get_app_version();
        assert!(!version.is_empty());
    }
}
