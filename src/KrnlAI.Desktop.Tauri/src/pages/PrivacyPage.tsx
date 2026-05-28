import { useEffect, useState } from "react";
import {
  getTelemetryConsent,
  requestTelemetryDeletion,
  requestTelemetryExport,
  setTelemetryConsent,
  loadDesktopAuthSettings,
  type TelemetryConsentState,
} from "../desktopServices";

export default function PrivacyPage() {
  const [consent, setConsent] = useState<TelemetryConsentState | null>(null);
  const [status, setStatus] = useState("Pronto");
  const [error, setError] = useState("");

  useEffect(() => {
    refresh();
  }, []);

  async function refresh() {
    try {
      setError("");
      setConsent(await getTelemetryConsent());
    } catch (e) {
      setError(String(e));
    }
  }

  async function updateConsent(level: string) {
    try {
      const next = await setTelemetryConsent(level);
      setConsent(next);
      setStatus(`Consentimento atualizado para ${level}`);
    } catch (e) {
      setError(String(e));
    }
  }

  async function exportData() {
    try {
      const result = await requestTelemetryExport();
      setStatus(result.message);
    } catch (e) {
      setError(String(e));
    }
  }

  async function deleteData() {
    if (!window.confirm("Solicitar exclusão dos dados?")) return;
    try {
      const result = await requestTelemetryDeletion();
      setStatus(result.message);
    } catch (e) {
      setError(String(e));
    }
  }

  const auth = loadDesktopAuthSettings();

  return (
    <div style={{ padding: 20, maxWidth: 840, margin: "0 auto" }}>
      <h2>Privacidade</h2>
      <p>Controle o consentimento de telemetria e os fluxos LGPD do usuário autenticado em {auth.apiBaseUrl}.</p>
      <p>{status}</p>
      {error && <p style={{ color: "#e66" }}>{error}</p>}

      <section style={cardStyle}>
        <h3>Consentimento atual</h3>
        {consent ? (
          <>
            <p><strong>Nível:</strong> {consent.consentLevel}</p>
            <p><strong>Concedido:</strong> {consent.grantedAt ? new Date(consent.grantedAt).toLocaleString() : "nunca"}</p>
            <p><strong>Revogado:</strong> {consent.revokedAt ? new Date(consent.revokedAt).toLocaleString() : "nunca"}</p>
          </>
        ) : (
          <p>Carregando...</p>
        )}
      </section>

      <section style={cardStyle}>
        <h3>Alterar consentimento</h3>
        <div style={{ display: "flex", gap: 8, flexWrap: "wrap" }}>
          <button onClick={() => updateConsent("None")}>Não coletar</button>
          <button onClick={() => updateConsent("Anonymous")}>Coleta anônima</button>
          <button onClick={() => updateConsent("Full")}>Coleta completa</button>
        </div>
      </section>

      <section style={cardStyle}>
        <h3>LGPD</h3>
        <div style={{ display: "flex", gap: 8, flexWrap: "wrap" }}>
          <button onClick={exportData}>Exportar dados</button>
          <button onClick={deleteData}>Solicitar exclusão</button>
          <button onClick={refresh}>Atualizar</button>
        </div>
      </section>
    </div>
  );
}

const cardStyle: React.CSSProperties = {
  border: "1px solid #334",
  borderRadius: 16,
  padding: 16,
  marginBottom: 16,
  background: "#111827",
  color: "#fff",
};
