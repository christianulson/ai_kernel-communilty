import { invoke as tauriInvoke } from "@tauri-apps/api/core";
import { listen as tauriListen } from "@tauri-apps/api/event";

export interface HealthStatus {
  status: string;
  version: string;
  sidecarRunning: boolean;
}

export interface SystemInfo {
  os: string;
  arch: string;
  cpuCores: string;
  memoryGb: string;
}

export interface SidecarStatus {
  running: boolean;
  pid: number | null;
  port: number;
}

export async function checkHealth(): Promise<HealthStatus> {
  return tauriInvoke<HealthStatus>("check_health");
}

export async function getSystemInfo(): Promise<SystemInfo> {
  return tauriInvoke<SystemInfo>("get_system_info");
}

export async function startSidecar(): Promise<SidecarStatus> {
  return tauriInvoke<SidecarStatus>("start_sidecar");
}

export async function stopSidecar(): Promise<void> {
  return tauriInvoke<void>("stop_sidecar");
}

export async function getSidecarStatus(): Promise<SidecarStatus> {
  return tauriInvoke<SidecarStatus>("get_sidecar_status");
}

export async function getAppVersion(): Promise<string> {
  return tauriInvoke<string>("get_app_version");
}

export async function listenNavigate(handler: (path: string) => void) {
  return tauriListen<string>("navigate", (e) => handler(e.payload));
}
