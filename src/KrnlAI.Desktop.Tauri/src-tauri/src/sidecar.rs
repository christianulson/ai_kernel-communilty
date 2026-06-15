use std::process::{Child, Command};
use std::sync::Mutex;

pub struct SidecarManager {
    inner: Mutex<SidecarState>,
}

struct SidecarState {
    process: Option<Child>,
    port: u16,
    running: bool,
}

impl SidecarManager {
    pub fn new() -> Self {
        Self {
            inner: Mutex::new(SidecarState {
                process: None,
                port: 5001,
                running: false,
            }),
        }
    }

    pub async fn start(&self) -> Result<(), Box<dyn std::error::Error>> {
        let mut state = self.inner.lock().map_err(|e| e.to_string())?;

        let sidecar_path = find_sidecar_binary()?;
        let child = Command::new(&sidecar_path)
            .args(["--port", &state.port.to_string()])
            .env("Sidecar__Mode", "Community")
            .stdout(std::process::Stdio::piped())
            .stderr(std::process::Stdio::piped())
            .spawn()
            .map_err(|e| format!("Failed to start sidecar: {}", e))?;

        state.process = Some(child);
        state.running = true;

        log::info!("Sidecar started on port {}", state.port);
        Ok(())
    }

    pub async fn stop(&self) -> Result<(), Box<dyn std::error::Error>> {
        let mut state = self.inner.lock().map_err(|e| e.to_string())?;
        if let Some(mut child) = state.process.take() {
            child.kill().ok();
        }
        state.running = false;
        log::info!("Sidecar stopped");
        Ok(())
    }

    pub fn is_running(&self) -> bool {
        self.inner.lock().map(|s| s.running).unwrap_or(false)
    }

    pub fn get_pid(&self) -> Option<u32> {
        self.inner.lock().ok().and_then(|s| s.process.as_ref().map(|p| p.id()))
    }

    pub fn get_port(&self) -> u16 {
        self.inner.lock().map(|s| s.port).unwrap_or(5001)
    }
}

pub fn find_sidecar_binary() -> Result<String, Box<dyn std::error::Error>> {
    let exe_dir = std::env::current_exe()
        .ok()
        .and_then(|p| p.parent().map(|d| d.to_path_buf()))
        .unwrap_or_else(|| std::env::current_dir().unwrap_or_default());

    let sidecar_exe = if cfg!(target_os = "windows") { "KrnlAI.Sidecar.exe" } else { "KrnlAI.Sidecar" };

    let candidates = [
        exe_dir.join(sidecar_exe),
        exe_dir.join("binaries").join(sidecar_exe),
        std::path::PathBuf::from("../KrnlAI.Sidecar/bin/Release/net10.0/win-x64/publish").join(sidecar_exe),
        std::path::PathBuf::from("../KrnlAI.Sidecar/bin/Debug/net10.0").join(sidecar_exe),
        std::path::PathBuf::from("../KrnlAI.Sidecar/bin/Debug/net10.0/win-x64").join(sidecar_exe),
    ];

    for candidate in &candidates {
        if candidate.exists() && candidate.metadata().map(|m| m.len() > 0).unwrap_or(false) {
            return Ok(candidate.to_string_lossy().to_string());
        }
    }

    Err("Sidecar binary not found or empty. Build it with:\n  cd Community && dotnet publish src/KrnlAI.Sidecar -r win-x64 --self-contained\nThen copy the binary to:\n  Community/src/KrnlAI.Desktop.Tauri/src-tauri/binaries/".into())
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn test_new_manager_starts_stopped() {
        let manager = SidecarManager::new();
        assert!(!manager.is_running());
        assert_eq!(manager.get_port(), 5001);
        assert!(manager.get_pid().is_none());
    }

    #[test]
    fn test_stop_without_start_does_not_panic() {
        let rt = tokio::runtime::Runtime::new().unwrap();
        let manager = SidecarManager::new();
        let result = rt.block_on(manager.stop());
        assert!(result.is_ok());
    }

    #[test]
    fn test_find_binary_returns_error_when_not_found() {
        // Temporarily change current dir to a temp dir so binary won't be found
        let tmp = std::env::temp_dir();
        let original = std::env::current_dir().ok();
        std::env::set_current_dir(&tmp).ok();

        let result = find_sidecar_binary();

        // Restore original dir
        if let Some(dir) = original {
            std::env::set_current_dir(dir).ok();
        }

        assert!(result.is_err());
        assert!(result.unwrap_err().to_string().contains("Sidecar binary not found"));
    }
}
