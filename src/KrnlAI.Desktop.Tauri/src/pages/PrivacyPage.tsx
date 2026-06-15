import { useEffect, useState } from "react";
import { apiGet, apiPut, apiPost } from "../api/client";

interface TelemetryState { consentLevel: string; grantedAt: string | null; revokedAt: string | null; }
interface TelemetryResult { status: string; message: string; }

export default function PrivacyPage() {
  const [consent, setConsent] = useState<TelemetryState | null>(null);
  const [status, setStatus] = useState("");
  const [error, setError] = useState("");

  useEffect(() => { refresh(); }, []);

  async function refresh() {
    setError("");
    const res = await apiGet<TelemetryState>("/api/privacy/telemetry/consent");
    if (res.ok && res.data) setConsent(res.data);
    else setError(res.error ?? "Falha ao carregar consentimento");
  }

  async function updateConsent(level: string) {
    const res = await apiPut<TelemetryState>("/api/privacy/telemetry/consent", { consentLevel: level });
    if (res.ok && res.data) { setConsent(res.data); setStatus(`Consentimento: ${level}`); }
    else setError(res.error ?? "Falha ao atualizar");
  }

  async function exportData() {
    const res = await apiPost<TelemetryResult>("/api/privacy/telemetry/export", {});
    if (res.ok && res.data) setStatus(res.data.message);
    else setError(res.error ?? "Falha ao exportar");
  }

  async function deleteData() {
    if (!window.confirm("Solicitar exclusão dos dados?")) return;
    const res = await apiPost<TelemetryResult>("/api/privacy/telemetry/delete", {});
    if (res.ok && res.data) setStatus(res.data.message);
    else setError(res.error ?? "Falha ao solicitar exclusão");
  }

  return (
    <div style={{ maxWidth: 800, margin: "0 auto" }}>
      <h1 style={{ fontSize: 24, fontWeight: 700, marginBottom: 4 }}>🛡️ Privacidade</h1>
      <p style={{ color: "#8AA0BC", marginBottom: 20 }}>Controle de consentimento de telemetria e fluxos LGPD.</p>

      {status && <p style={{ color: "#22C55E", fontSize: 13, marginBottom: 8 }}>{status}</p>}
      {error && <p style={{ color: "#FB7185", fontSize: 13, marginBottom: 8 }}>{error}</p>}

      <div className="card">
        <h3 style={{ marginBottom: 12 }}>Consentimento Atual</h3>
        {consent ? (
          <>
            <p><strong>Nível:</strong> {consent.consentLevel}</p>
            <p style={{ color: "#8AA0BC", fontSize: 13 }}>Concedido: {consent.grantedAt ? new Date(consent.grantedAt).toLocaleString() : "nunca"}</p>
            <p style={{ color: "#8AA0BC", fontSize: 13 }}>Revogado: {consent.revokedAt ? new Date(consent.revokedAt).toLocaleString() : "nunca"}</p>
          </>
        ) : <p style={{ color: "#8AA0BC" }}>Carregando...</p>}
      </div>

      <div className="card">
        <h3 style={{ marginBottom: 12 }}>Alterar Consentimento</h3>
        <div style={{ display: "flex", gap: 8, flexWrap: "wrap" }}>
          {["None", "Anonymous", "Full"].map((level) => (
            <button key={level} onClick={() => updateConsent(level)} style={{
              background: consent?.consentLevel === level ? "#38BDF8" : "#1E293B",
              color: consent?.consentLevel === level ? "#03111D" : "#E5EEFC",
              padding: "8px 16px", borderRadius: 8, border: "none", cursor: "pointer", fontSize: 13,
            }}>{level}</button>
          ))}
        </div>
      </div>

      <div className="card">
        <h3 style={{ marginBottom: 12 }}>LGPD</h3>
        <div style={{ display: "flex", gap: 8, flexWrap: "wrap" }}>
          <button onClick={exportData} style={{ background: "#1E293B", color: "#E5EEFC", padding: "8px 16px", borderRadius: 8, border: "none", cursor: "pointer" }}>Exportar dados</button>
          <button onClick={deleteData} style={{ background: "#3F1A1A", color: "#FB7185", padding: "8px 16px", borderRadius: 8, border: "none", cursor: "pointer" }}>Solicitar exclusão</button>
          <button onClick={refresh} style={{ background: "#1E293B", color: "#E5EEFC", padding: "8px 16px", borderRadius: 8, border: "none", cursor: "pointer" }}>Atualizar</button>
        </div>
      </div>
    </div>
  );
}
