import { useEffect, useState } from "react";
import { Routes, Route } from "react-router-dom";
import ChatPage from "./pages/ChatPage";
import DashboardPage from "./pages/DashboardPage";
import SettingsPage from "./pages/SettingsPage";
import SidecarStatus from "./components/SidecarStatus";
import { invoke } from "./TauriBridge";

export default function App() {
  const [sidecarRunning, setSidecarRunning] = useState(false);
  const [appVersion, setAppVersion] = useState("");

  useEffect(() => {
    invoke("get_app_version").then(setAppVersion);
    checkSidecar();
  }, []);

  async function checkSidecar() {
    try {
      const status = await invoke<{ running: boolean }>("get_sidecar_status");
      setSidecarRunning(status.running);
    } catch {
      setSidecarRunning(false);
    }
  }

  return (
    <div style={{ display: "flex", flexDirection: "column", height: "100vh" }}>
      <header
        style={{
          display: "flex",
          justifyContent: "space-between",
          alignItems: "center",
          padding: "8px 16px",
          background: "#1a1a2e",
          color: "#fff",
        }}
      >
        <h1 style={{ fontSize: 16, margin: 0 }}>Krnl-AI Desktop</h1>
        <div style={{ display: "flex", gap: 16, alignItems: "center" }}>
          <SidecarStatus running={sidecarRunning} />
          <nav style={{ display: "flex", gap: 8 }}>
            <a href="/" style={{ color: "#fff" }}>
              Chat
            </a>
            <a href="/dashboard" style={{ color: "#fff" }}>
              Dashboard
            </a>
            <a href="/settings" style={{ color: "#fff" }}>
              Settings
            </a>
          </nav>
          <span style={{ fontSize: 12, opacity: 0.6 }}>v{appVersion}</span>
        </div>
      </header>

      <main style={{ flex: 1, overflow: "auto" }}>
        <Routes>
          <Route path="/" element={<ChatPage />} />
          <Route path="/dashboard" element={<DashboardPage />} />
          <Route path="/settings" element={<SettingsPage />} />
        </Routes>
      </main>
    </div>
  );
}
