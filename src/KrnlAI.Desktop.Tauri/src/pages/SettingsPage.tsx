import { useEffect, useState } from "react";
import { invoke } from "../TauriBridge";
import { isPermissionGranted, requestPermission, sendNotification } from "@tauri-apps/plugin-notification";

export default function SettingsPage() {
  const [systemInfo, setSystemInfo] = useState<{
    os: string;
    arch: string;
    cpu_cores: number;
  } | null>(null);
  const [notifGranted, setNotifGranted] = useState(false);

  useEffect(() => {
    invoke<{ os: string; arch: string; cpu_cores: number }>("get_system_info").then(setSystemInfo);
    checkNotificationPermission();

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

  return (
    <div style={{ padding: 16, maxWidth: 800, margin: "0 auto" }}>
      <h2>Settings</h2>

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
