import { useEffect, useState } from "react";
import { checkHealth, getSystemInfo, getSidecarStatus, type HealthStatus, type SystemInfo, type SidecarStatus } from "../TauriBridge";

export default function DashboardPage() {
  const [health, setHealth] = useState<HealthStatus | null>(null);
  const [sysInfo, setSysInfo] = useState<SystemInfo | null>(null);
  const [sidecar, setSidecar] = useState<SidecarStatus | null>(null);
  const [error, setError] = useState("");
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const errors: string[] = [];
    Promise.all([
      checkHealth().then(setHealth).catch((e) => errors.push("health: " + String(e))),
      getSystemInfo().then(setSysInfo).catch(() => errors.push("system: unavailable")),
      getSidecarStatus().then(setSidecar).catch(() => errors.push("sidecar: unavailable")),
    ]).finally(() => { if (errors.length) setError(errors.join("; ")); setLoading(false); });
  }, []);

  if (loading) return <p style={{ color: "#8AA0BC" }}>Carregando...</p>;

  return (
    <div style={{ maxWidth: 800, margin: "0 auto" }}>
      <h1 style={{ fontSize: 24, fontWeight: 700, marginBottom: 4 }}>📊 Dashboard</h1>
      <p style={{ color: "#8AA0BC", marginBottom: 20 }}>Status do sistema Krnl-AI via Tauri IPC.</p>

      {error && <p style={{ color: "#FB7185", marginBottom: 16 }}>{error}</p>}

      <div className="card">
        <h3>Servidor</h3>
        <p style={{ fontSize: 28, fontWeight: 700, color: health?.status === "ok" ? "#22C55E" : "#FB7185", marginTop: 8 }}>
          {health?.status === "ok" ? "🟢 Online" : "🔴 Offline"}
        </p>
        {health && <p style={{ color: "#8AA0BC", fontSize: 13, marginTop: 4 }}>v{health.version}</p>}
      </div>

      <div className="card">
        <h3>Sidecar</h3>
        <p style={{ fontSize: 18, fontWeight: 600, marginTop: 8, color: sidecar?.running ? "#22C55E" : "#FB7185" }}>
          {sidecar?.running ? `🟢 Rodando (porta ${sidecar.port})` : "🔴 Parado"}
        </p>
        {sidecar?.pid && <p style={{ color: "#8AA0BC", fontSize: 13 }}>PID: {sidecar.pid}</p>}
      </div>

      {sysInfo && (
        <div className="card">
          <h3>Sistema</h3>
          <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: 12, marginTop: 8, fontSize: 14 }}>
            <div><span style={{ color: "#8AA0BC" }}>OS:</span> {sysInfo.os}</div>
            <div><span style={{ color: "#8AA0BC" }}>Arquitetura:</span> {sysInfo.arch}</div>
            <div><span style={{ color: "#8AA0BC" }}>CPUs:</span> {sysInfo.cpuCores}</div>
            <div><span style={{ color: "#8AA0BC" }}>RAM:</span> {sysInfo.memoryGb}</div>
          </div>
        </div>
      )}
    </div>
  );
}
