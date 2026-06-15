const AUTH_STORAGE_KEY = "krnl.desktop.auth";

export type AuthMethod = "jwt" | "apiKey" | "anonymous";

export interface DesktopAuthSettings {
  apiBaseUrl: string;
  authToken: string;
  authMethod: AuthMethod;
}

export interface StorageLike {
  getItem(key: string): string | null;
  setItem(key: string, value: string): void;
  removeItem(key: string): void;
}

function getStorage(storage?: StorageLike): StorageLike {
  if (storage) return storage;
  const g = globalThis.localStorage as StorageLike | undefined;
  return g ?? { getItem: () => null, setItem: () => {}, removeItem: () => {} };
}

export function maskSecret(value: string): string {
  if (!value || value.length <= 12) return value;
  return `${value.slice(0, 8)}••••${value.slice(-4)}`;
}

export function describeAuthMethod(token: string): AuthMethod {
  if (!token) return "anonymous";
  return token.startsWith("krnl_") ? "apiKey" : "jwt";
}

export function loadDesktopAuthSettings(storage?: StorageLike): DesktopAuthSettings {
  const raw = getStorage(storage).getItem(AUTH_STORAGE_KEY);
  if (!raw) return { apiBaseUrl: "http://localhost:5235", authToken: "", authMethod: "anonymous" };
  try {
    const p = JSON.parse(raw) as Partial<DesktopAuthSettings>;
    return {
      apiBaseUrl: p.apiBaseUrl || "http://localhost:5235",
      authToken: p.authToken ?? "",
      authMethod: p.authMethod || describeAuthMethod(p.authToken ?? ""),
    };
  } catch {
    return { apiBaseUrl: "http://localhost:5235", authToken: "", authMethod: "anonymous" };
  }
}

export function saveDesktopAuthSettings(settings: DesktopAuthSettings, storage?: StorageLike): void {
  getStorage(storage).setItem(AUTH_STORAGE_KEY, JSON.stringify(settings));
}

export function clearDesktopAuthSettings(storage?: StorageLike): void {
  getStorage(storage).removeItem(AUTH_STORAGE_KEY);
}

export function describeAuthState(settings: DesktopAuthSettings): string {
  if (!settings.authToken) return "Sem sessão autenticada";
  return settings.authMethod === "apiKey" ? "Sessão via API key" : "Sessão via JWT";
}

export function normalizeBaseUrl(value: string): string {
  return value.replace(/\/+$/, "");
}
