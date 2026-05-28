const DEFAULT_API_BASE_URL = "http://localhost:5235";
const AUTH_STORAGE_KEY = "krnl.desktop.auth";

export type AuthMethod = "jwt" | "apiKey" | "anonymous";

export interface DesktopAuthSettings {
  apiBaseUrl: string;
  authToken: string;
  authMethod: AuthMethod;
}

export interface ApiKeyListItem {
  keyId: string;
  keyPrefix: string;
  name: string;
  scope: number;
  createdAt: string;
  expiresAt: string;
  lastUsedAt?: string | null;
  active: boolean;
  displayPrefix?: string;
}

export interface ApiKeyCreationRequest {
  name: string;
  ttl: string;
  scope: number;
}

export interface ApiKeyCreationResult {
  keyId: string;
  fullKey: string;
  name: string;
  scope: number;
  expiresAt: string;
  warning: string;
}

export interface ApiKeyUsageSummary {
  total: number;
  active: number;
  expired: number;
  revoked: number;
  lastUsed?: string | null;
}

export interface TelemetryConsentState {
  userId?: string;
  consentLevel: string;
  grantedAt?: string | null;
  revokedAt?: string | null;
}

export interface TelemetryActionResult {
  requestId?: string | null;
  status: string;
  message: string;
}

export interface StorageLike {
  getItem(key: string): string | null;
  setItem(key: string, value: string): void;
  removeItem(key: string): void;
}

const memoryStorage: StorageLike = {
  getItem(key: string) {
    return memoryStore.get(key) ?? null;
  },
  setItem(key: string, value: string) {
    memoryStore.set(key, value);
  },
  removeItem(key: string) {
    memoryStore.delete(key);
  },
};

const memoryStore = new Map<string, string>();

function getStorage(storage?: StorageLike): StorageLike {
  if (storage) return storage;
  const globalStorage = globalThis.localStorage as StorageLike | undefined;
  return globalStorage ?? memoryStorage;
}

export function maskSecret(value: string): string {
  if (!value) return "";
  if (value.length <= 12) return value;
  return `${value.slice(0, 8)}••••${value.slice(-4)}`;
}

export function describeAuthMethod(token: string): AuthMethod {
  if (!token) return "anonymous";
  return token.startsWith("krnl_") ? "apiKey" : "jwt";
}

export function loadDesktopAuthSettings(storage?: StorageLike): DesktopAuthSettings {
  const current = getStorage(storage);
  const raw = current.getItem(AUTH_STORAGE_KEY);
  if (!raw) {
    return {
      apiBaseUrl: DEFAULT_API_BASE_URL,
      authToken: "",
      authMethod: "anonymous",
    };
  }

  try {
    const parsed = JSON.parse(raw) as Partial<DesktopAuthSettings>;
    const authToken = parsed.authToken ?? "";
    return {
      apiBaseUrl: parsed.apiBaseUrl || DEFAULT_API_BASE_URL,
      authToken,
      authMethod: parsed.authMethod || describeAuthMethod(authToken),
    };
  } catch {
    return {
      apiBaseUrl: DEFAULT_API_BASE_URL,
      authToken: "",
      authMethod: "anonymous",
    };
  }
}

export function saveDesktopAuthSettings(settings: DesktopAuthSettings, storage?: StorageLike): void {
  const current = getStorage(storage);
  current.setItem(AUTH_STORAGE_KEY, JSON.stringify(settings));
}

export function clearDesktopAuthSettings(storage?: StorageLike): void {
  getStorage(storage).removeItem(AUTH_STORAGE_KEY);
}

export function describeAuthState(settings: DesktopAuthSettings): string {
  if (!settings.authToken) return "Sem sessão autenticada";
  return settings.authMethod === "apiKey"
    ? "Sessão autenticada via API key"
    : "Sessão autenticada via JWT";
}

export function getAuthHeaders(settings?: DesktopAuthSettings): HeadersInit {
  const current = settings ?? loadDesktopAuthSettings();
  const headers: Record<string, string> = {};
  if (current.authToken) headers.Authorization = `Bearer ${current.authToken}`;
  return headers;
}

export function normalizeBaseUrl(value: string): string {
  return value.replace(/\/+$/, "");
}

async function requestJson<T>(path: string, init: RequestInit = {}, storage?: StorageLike): Promise<T> {
  const settings = loadDesktopAuthSettings(storage);
  const response = await fetch(`${normalizeBaseUrl(settings.apiBaseUrl)}${path}`, {
    ...init,
    headers: {
      "Content-Type": "application/json",
      ...getAuthHeaders(settings),
      ...(init.headers ?? {}),
    },
  });

  if (!response.ok) {
    throw new Error(await response.text());
  }

  return (await response.json()) as T;
}

export async function listApiKeys(storage?: StorageLike): Promise<ApiKeyListItem[]> {
  const items = await requestJson<ApiKeyListItem[]>("/account/api-keys", { method: "GET" }, storage);
  return items.map((item) => ({ ...item, displayPrefix: maskSecret(item.keyPrefix) }));
}

export async function createApiKey(request: ApiKeyCreationRequest, storage?: StorageLike): Promise<ApiKeyCreationResult> {
  return requestJson<ApiKeyCreationResult>("/account/api-keys", {
    method: "POST",
    body: JSON.stringify(request),
  }, storage);
}

export async function revokeApiKey(keyId: string, storage?: StorageLike): Promise<void> {
  await requestJson<{ revoked: boolean }>(`/account/api-keys/${keyId}/revoke`, { method: "POST" }, storage);
}

export async function getApiKeyStats(storage?: StorageLike): Promise<ApiKeyUsageSummary> {
  return requestJson<ApiKeyUsageSummary>("/account/api-keys/stats", { method: "GET" }, storage);
}

export async function getTelemetryConsent(storage?: StorageLike): Promise<TelemetryConsentState> {
  return requestJson<TelemetryConsentState>("/api/privacy/telemetry/consent", { method: "GET" }, storage);
}

export async function setTelemetryConsent(consentLevel: string, storage?: StorageLike): Promise<TelemetryConsentState> {
  return requestJson<TelemetryConsentState>("/api/privacy/telemetry/consent", {
    method: "PUT",
    body: JSON.stringify({ consentLevel }),
  }, storage);
}

export async function requestTelemetryExport(storage?: StorageLike): Promise<TelemetryActionResult> {
  return requestJson<TelemetryActionResult>("/api/privacy/telemetry/export", { method: "POST" }, storage);
}

export async function requestTelemetryDeletion(storage?: StorageLike): Promise<TelemetryActionResult> {
  return requestJson<TelemetryActionResult>("/api/privacy/telemetry/delete", { method: "POST" }, storage);
}
