import { invoke as tauriInvoke } from "@tauri-apps/api/core";
import { listen as tauriListen } from "@tauri-apps/api/event";

export async function invoke<T>(cmd: string, args?: Record<string, unknown>): Promise<T> {
  return await tauriInvoke<T>(cmd, args);
}

export async function listen<T>(event: string, handler: (payload: T) => void) {
  return await tauriListen<T>(event, (e) => handler(e.payload));
}
