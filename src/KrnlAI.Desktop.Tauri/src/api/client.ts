const STORAGE_KEY = "krnl.desktop.auth";

interface AuthSettings {
  apiBaseUrl: string;
  authToken: string;
}

function loadAuth(): AuthSettings {
  try {
    const raw = localStorage.getItem(STORAGE_KEY);
    if (raw) return JSON.parse(raw);
  } catch { /* ignore */ }
  return { apiBaseUrl: "http://localhost:5235", authToken: "" };
}

export async function apiGet<T>(path: string): Promise<{ ok: boolean; data?: T; error?: string }> {
  try {
    const auth = loadAuth();
    const res = await fetch(`${auth.apiBaseUrl}${path}`, {
      headers: auth.authToken ? { Authorization: `Bearer ${auth.authToken}` } : {},
    });
    if (!res.ok) {
      const text = await res.text();
      return { ok: false, error: `HTTP ${res.status}: ${text}` };
    }
    return { ok: true, data: await res.json() as T };
  } catch (e) {
    return { ok: false, error: String(e) };
  }
}

export async function apiPost<T>(path: string, body: unknown): Promise<{ ok: boolean; data?: T; error?: string }> {
  try {
    const auth = loadAuth();
    const res = await fetch(`${auth.apiBaseUrl}${path}`, {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
        ...(auth.authToken ? { Authorization: `Bearer ${auth.authToken}` } : {}),
      },
      body: JSON.stringify(body),
    });
    if (!res.ok) {
      const text = await res.text();
      return { ok: false, error: `HTTP ${res.status}: ${text}` };
    }
    return { ok: true, data: await res.json() as T };
  } catch (e) {
    return { ok: false, error: String(e) };
  }
}

export async function apiPut<T>(path: string, body: unknown): Promise<{ ok: boolean; data?: T; error?: string }> {
  try {
    const auth = loadAuth();
    const res = await fetch(`${auth.apiBaseUrl}${path}`, {
      method: "PUT",
      headers: {
        "Content-Type": "application/json",
        ...(auth.authToken ? { Authorization: `Bearer ${auth.authToken}` } : {}),
      },
      body: JSON.stringify(body),
    });
    if (!res.ok) {
      const text = await res.text();
      return { ok: false, error: `HTTP ${res.status}: ${text}` };
    }
    return { ok: true, data: await res.json() as T };
  } catch (e) {
    return { ok: false, error: String(e) };
  }
}
