import { useEffect, useState } from "react";
import { invoke } from "../TauriBridge";
import { isPermissionGranted, requestPermission, sendNotification } from "@tauri-apps/plugin-notification";
import {
  clearDesktopAuthSettings,
  describeAuthMethod,
  describeAuthState,
  loadDesktopAuthSettings,
  maskSecret,
  saveDesktopAuthSettings,
  type DesktopAuthSettings,
} from "../desktopServices";

export default function SettingsPage() {
  const [systemInfo, setSystemInfo] = useState<{
    os: string;
    arch: string;
    cpu_cores: number;
  } | null>(null);
  const [notifGranted, setNotifGranted] = useState(false);
  const [auth, setAuth] = useState<DesktopAuthSettings>(loadDesktopAuthSettings());

  useEffect(() => {
    invoke<{ os: string; arch: string; cpu_cores: number }>("get_system_info").then(setSystemInfo);
    checkNotificationPermission();
    setAuth(loadDesktopAuthSettings());

    console.log("Ctrl+Alt+K available when global-shortcut plugin is added");
  }, []);

  async function checkNotificationPermission() {
    const granted = await isPermissionGranted();
    if (granted) {
      setNotifGranted(true);
    } else {
      const permission = await requestPermission();
      setNotifGranted(permission === "granted");
    }
  }

  async function testNotification() {
    await sendNotification({ title: "Krnl-AI", body: "Hello from desktop!" });
  }

  function saveAuth() {
    const normalized = {
      ...auth,
      authMethod: auth.authToken ? describeAuthMethod(auth.authToken) : auth.authMethod,
    };
    saveDesktopAuthSettings(normalized);
    setAuth(normalized);
  }

  function clearAuth() {
    clearDesktopAuthSettings();
    setAuth(loadDesktopAuthSettings());
  }

  return (
    <div style={{ padding: 16, maxWidth: 900, margin: "0 auto" }}>
      <h2>Settings</h2>

      <section style={cardStyle}>
        <h3>Auth</h3>
        <p>{describeAuthState(auth)}</p>
        <p>Token: {auth.authToken ? maskSecret(auth.authToken) : "none"}</p>
        <div style={gridStyle}>
          <input
            value={auth.apiBaseUrl}
            onChange={(e) => setAuth((current) => ({ ...current, apiBaseUrl: e.target.value }))}
            placeholder="API base URL"
          />
          <input
            value={auth.authToken}
            onChange={(e) => setAuth((current) => ({ ...current, authToken: e.target.value }))}
            placeholder="JWT or API key"
          />
          <select
            value={auth.authMethod}
            onChange={(e) => setAuth((current) => ({ ...current, authMethod: e.target.value as DesktopAuthSettings["authMethod"] }))}
          >
            <option value="anonymous">Anonymous</option>
            <option value="jwt">JWT</option>
            <option value="apiKey">API key</option>
          </select>
        </div>
        <div style={{ display: "flex", gap: 8 }}>
          <button onClick={saveAuth}>Save</button>
          <button onClick={clearAuth}>Clear</button>
        </div>
      </section>

      <section style={{ marginBottom: 24 }}>
        <h3>System Info</h3>
        {systemInfo && (
          <ul>
            <li>OS: {systemInfo.os}</li>
            <li>Arch: {systemInfo.arch}</li>
            <li>CPU Cores: {systemInfo.cpu_cores}</li>
          </ul>
        )}
      </section>

      <section style={{ marginBottom: 24 }}>
        <h3>Notifications</h3>
        <p>Permission: {notifGranted ? "Granted" : "Not granted"}</p>
        <button onClick={testNotification} disabled={!notifGranted}>
          Test Notification
        </button>
      </section>

      <section>
        <h3>Global Hotkey</h3>
        <p>Ctrl+Alt+K (coming soon)</p>
      </section>
    </div>
  );
}

const cardStyle: React.CSSProperties = {
  marginBottom: 24,
  padding: 16,
  border: "1px solid #334",
  borderRadius: 16,
  background: "#111827",
  color: "#fff",
};

const gridStyle: React.CSSProperties = {
  display: "grid",
  gridTemplateColumns: "2fr 2fr 1fr",
  gap: 8,
  marginBottom: 12,
};
