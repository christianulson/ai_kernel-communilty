#![allow(dead_code)]
use std::sync::Arc;
use std::sync::atomic::{AtomicBool, Ordering};
use std::time::Duration;
use tauri::{AppHandle, Emitter, Manager};
use crate::sidecar::SidecarManager;

pub struct SidecarWatchdog {
    running: Arc<AtomicBool>,
}

impl SidecarWatchdog {
    pub fn new() -> Self {
        Self {
            running: Arc::new(AtomicBool::new(false)),
        }
    }

    pub fn start(&self, app: AppHandle) {
        self.running.store(true, Ordering::SeqCst);
        let running = self.running.clone();

        std::thread::spawn(move || {
            let mut consecutive_failures = 0u32;
            const MAX_FAILURES: u32 = 3;
            const CHECK_INTERVAL: Duration = Duration::from_secs(30);
            const RESTART_COOLDOWN: Duration = Duration::from_secs(60);

            while running.load(Ordering::SeqCst) {
                std::thread::sleep(CHECK_INTERVAL);

                if let Some(sidecar) = app.try_state::<SidecarManager>() {
                    let sidecar = sidecar.inner();
                    let is_running = sidecar.is_running();

                    if is_running {
                        let health_url = format!("http://127.0.0.1:{}/health", sidecar.get_port());
                        match reqwest::blocking::get(&health_url) {
                            Ok(resp) if resp.status().is_success() => {
                                consecutive_failures = 0;
                            }
                            _ => {
                                consecutive_failures += 1;
                                let _ = app.emit("sidecar-health-warn", format!(
                                    "Health check failed ({}/{}). Sidecar may be unresponsive.",
                                    consecutive_failures, MAX_FAILURES
                                ));
                            }
                        }

                        if consecutive_failures >= MAX_FAILURES {
                            let _ = app.emit("sidecar-restarting", "Sidecar unresponsive. Restarting...");
                            let sidecar_clone = sidecar.clone();
                            std::thread::spawn(move || {
                                let rt = tokio::runtime::Runtime::new().unwrap();
                                let _ = rt.block_on(sidecar_clone.stop());
                            }).join().unwrap_or_default();
                            std::thread::sleep(RESTART_COOLDOWN);
                            std::thread::spawn(move || {
                                let rt = tokio::runtime::Runtime::new().unwrap();
                                let _ = rt.block_on(sidecar.start());
                            }).join().unwrap_or_default();
                            consecutive_failures = 0;
                            let _ = app.emit("sidecar-restarted", "Sidecar restarted successfully");
                        }
                    }
                }
            }
        });
    }

    pub fn stop(&self) {
        self.running.store(false, Ordering::SeqCst);
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn watchdog_new_starts_stopped() {
        let w = SidecarWatchdog::new();
        assert!(!w.running.load(Ordering::SeqCst));
    }

    #[test]
    fn watchdog_stop_sets_running_false() {
        let w = SidecarWatchdog::new();
        w.running.store(true, Ordering::SeqCst);
        w.stop();
        assert!(!w.running.load(Ordering::SeqCst));
    }
}



