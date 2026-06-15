import { useEffect, useState } from "react";
import { getAppVersion, startSidecar, stopSidecar, getSidecarStatus, type SidecarStatus } from "../TauriBridge";

export default function SettingsPage() {
  const [endpoint, setEndpoint] = useState(() => localStorage.getItem("krnl_api_base_url") ?? "http://localhost:5235");
  const [version, setVersion] = useState("");
  const [sidecar, setSidecar] = useState<SidecarStatus | null>(null);
  const [starting, setStarting] = useState(false);
  const [sidecarError, setSidecarError] = useState("");

  useEffect(() => {
    getAppVersion().then(setVersion).catch(() => setVersion("—"));
    getSidecarStatus().then(setSidecar).catch(() => setSidecarError("sidecar indisponível"));
  }, []);

  const handleSaveEndpoint = () => {
    localStorage.setItem("krnl_api_base_url", endpoint);
  };

  const handleStartSidecar = async () => {
    setStarting(true);
    setSidecarError("");
    try {
      const s = await startSidecar();
      setSidecar(s);
    } catch (e) {
      setSidecarError(`Erro ao iniciar: ${e}`);
    }
    setStarting(false);
  };

  const handleStopSidecar = async () => {
    setSidecarError("");
    try {
      await stopSidecar();
      setSidecar({ running: false, pid: null, port: 5001 });
    } catch (e) {
      setSidecarError(`Erro ao parar: ${e}`);
    }
  };

  return (
    <div style={{ maxWidth: 800, margin: "0 auto" }}>
      <h1 style={{ fontSize: 24, fontWeight: 700, marginBottom: 4 }}>⚙️ Configurações</h1>
      <p style={{ color: "#8AA0BC", marginBottom: 20 }}>Configure sua conexão e o sidecar local.</p>

      <div className="card">
        <h3 style={{ marginBottom: 12 }}>Endpoint da API</h3>
        <div style={{ display: "flex", gap: 8 }}>
          <input value={endpoint} onChange={(e) => setEndpoint(e.target.value)} placeholder="http://localhost:5235" />
          <button onClick={handleSaveEndpoint} style={{ background: "#38BDF8", color: "#03111D", padding: "10px 20px", borderRadius: 8, border: "none", fontWeight: 600, cursor: "pointer", whiteSpace: "nowrap" }}>
            Salvar
          </button>
        </div>
      </div>

      {sidecarError && <p style={{ color: "#FB7185", marginBottom: 8, fontSize: 13 }}>{sidecarError}</p>}

      <div className="card">
        <h3 style={{ marginBottom: 12 }}>Sidecar</h3>
        <p style={{ fontSize: 14, marginBottom: 12 }}>
          Status: <strong style={{ color: sidecar?.running ? "#22C55E" : "#FB7185" }}>
            {sidecar?.running ? `Rodando (porta ${sidecar.port})` : "Parado"}
          </strong>
        </p>
        {sidecar?.pid && <p style={{ color: "#8AA0BC", fontSize: 13, marginBottom: 8 }}>PID: {sidecar.pid}</p>}
        <div style={{ display: "flex", gap: 8 }}>
          <button onClick={handleStartSidecar} disabled={starting || sidecar?.running} style={{
            background: sidecar?.running ? "#1E293B" : "#22C55E", color: sidecar?.running ? "#8AA0BC" : "#03111D",
            padding: "8px 20px", borderRadius: 8, border: "none", fontWeight: 600, cursor: sidecar?.running ? "not-allowed" : "pointer",
          }}>{starting ? "Iniciando..." : "Iniciar"}</button>
          <button onClick={handleStopSidecar} disabled={!sidecar?.running} style={{
            background: sidecar?.running ? "#FB7185" : "#1E293B", color: sidecar?.running ? "#03111D" : "#8AA0BC",
            padding: "8px 20px", borderRadius: 8, border: "none", fontWeight: 600, cursor: sidecar?.running ? "pointer" : "not-allowed",
          }}>Parar</button>
        </div>
      </div>

      <div className="card">
        <h3 style={{ marginBottom: 12 }}>Sobre</h3>
        <p style={{ color: "#8AA0BC" }}>
          Krnl-AI Desktop v{version} (Tauri)<br />
          Cliente desktop nativo e cross-platform para o agente cognitivo Krnl-AI.
        </p>
      </div>
    </div>
  );
}
