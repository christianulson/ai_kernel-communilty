import { useEffect, useState } from "react";
import {
  createApiKey,
  getApiKeyStats,
  listApiKeys,
  loadDesktopAuthSettings,
  revokeApiKey,
  type ApiKeyCreationResult,
  type ApiKeyListItem,
  type ApiKeyUsageSummary,
} from "../desktopServices";

export default function ApiKeysPage() {
  const [keys, setKeys] = useState<ApiKeyListItem[]>([]);
  const [stats, setStats] = useState<ApiKeyUsageSummary | null>(null);
  const [name, setName] = useState("");
  const [ttlDays, setTtlDays] = useState("30");
  const [scope, setScope] = useState("1");
  const [created, setCreated] = useState<ApiKeyCreationResult | null>(null);
  const [status, setStatus] = useState("Pronto");
  const [error, setError] = useState("");

  useEffect(() => {
    refresh();
  }, []);

  async function refresh() {
    try {
      setError("");
      const [items, summary] = await Promise.all([listApiKeys(), getApiKeyStats()]);
      setKeys(items);
      setStats(summary);
      setStatus(items.length === 0 ? "Nenhuma chave cadastrada" : `${summary.active} ativa(s)`);
    } catch (e) {
      setError(String(e));
    }
  }

  async function handleCreate() {
    if (!name.trim()) return;
    try {
      setError("");
      const result = await createApiKey({
        name: name.trim(),
        ttl: `${Number(ttlDays)}.00:00:00`,
        scope: Number(scope),
      });
      setCreated(result);
      setStatus("Chave criada. Copie o valor agora.");
      setName("");
      await refresh();
    } catch (e) {
      setError(String(e));
    }
  }

  async function handleRevoke(keyId: string) {
    if (!window.confirm("Revogar esta API key? O acesso será encerrado imediatamente.")) return;
    try {
      await revokeApiKey(keyId);
      setStatus("Chave revogada.");
      await refresh();
    } catch (e) {
      setError(String(e));
    }
  }

  async function copyCreatedKey() {
    if (!created) return;
    await navigator.clipboard.writeText(created.fullKey);
    setStatus("Chave copiada.");
  }

  const auth = loadDesktopAuthSettings();

  return (
    <div style={{ padding: 20, maxWidth: 980, margin: "0 auto" }}>
      <h2>API Keys</h2>
      <p>Gerencie chaves do usuário autenticado em {auth.apiBaseUrl}.</p>
      <p>{status}</p>
      {error && <p style={{ color: "#e66" }}>{error}</p>}

      <section style={cardStyle}>
        <h3>Nova chave</h3>
        <div style={gridStyle}>
          <input value={name} onChange={(e) => setName(e.target.value)} placeholder="Nome" />
          <input value={ttlDays} onChange={(e) => setTtlDays(e.target.value)} placeholder="TTL em dias" />
          <select value={scope} onChange={(e) => setScope(e.target.value)}>
            <option value="0">ReadOnly</option>
            <option value="1">ReadWrite</option>
            <option value="2">Full</option>
          </select>
        </div>
        <button onClick={handleCreate}>Criar chave</button>
      </section>

      {created && (
        <section style={cardStyle}>
          <h3>Chave criada</h3>
          <p>{created.warning}</p>
          <pre style={preStyle}>{created.fullKey}</pre>
          <button onClick={copyCreatedKey}>Copiar chave</button>
        </section>
      )}

      <section style={cardStyle}>
        <h3>Resumo</h3>
        {stats ? (
          <ul>
            <li>Total: {stats.total}</li>
            <li>Ativas: {stats.active}</li>
            <li>Expiradas: {stats.expired}</li>
            <li>Revogadas: {stats.revoked}</li>
          </ul>
        ) : (
          <p>Carregando...</p>
        )}
      </section>

      <section style={cardStyle}>
        <h3>Chaves existentes</h3>
        {keys.length === 0 ? (
          <p>Nenhuma chave cadastrada.</p>
        ) : (
          <div style={{ display: "grid", gap: 12 }}>
            {keys.map((key) => (
              <div key={key.keyId} style={itemStyle}>
                <div>
                  <strong>{key.name}</strong>
                  <div>{key.displayPrefix ?? key.keyPrefix}</div>
                  <div>{key.scope}</div>
                  <div>Expira: {new Date(key.expiresAt).toLocaleString()}</div>
                  <div>Último uso: {key.lastUsedAt ? new Date(key.lastUsedAt).toLocaleString() : "nunca"}</div>
                </div>
                <button onClick={() => handleRevoke(key.keyId)}>Revogar</button>
              </div>
            ))}
          </div>
        )}
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

const itemStyle: React.CSSProperties = {
  display: "flex",
  justifyContent: "space-between",
  gap: 16,
  padding: 14,
  border: "1px solid #334",
  borderRadius: 12,
  background: "#0b1220",
};

const gridStyle: React.CSSProperties = {
  display: "grid",
  gridTemplateColumns: "2fr 1fr 1fr",
  gap: 8,
  marginBottom: 12,
};

const preStyle: React.CSSProperties = {
  whiteSpace: "pre-wrap",
  wordBreak: "break-all",
};
