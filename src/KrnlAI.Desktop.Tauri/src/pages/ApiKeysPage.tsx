import { useEffect, useState, useCallback } from "react";
import { apiGet, apiPost } from "../api/client";

interface ApiKeySummary {
  keyId: string;
  keyPrefix: string;
  name: string;
  scope: string;
  createdAt: string;
  expiresAt: string;
  lastUsedAt: string | null;
  active: boolean;
}

interface CreatedApiKey {
  keyId: string;
  fullKey: string;
  name: string;
  scope: string;
  expiresAt: string;
  warning: string;
}

interface ApiKeyStats {
  total: number;
  active: number;
  expired: number;
  revoked: number;
}

const ALL_SCOPES = ["ReadOnly", "ReadWrite", "Full"];

export default function ApiKeysPage() {
  const [keys, setKeys] = useState<ApiKeySummary[]>([]);
  const [stats, setStats] = useState<ApiKeyStats | null>(null);
  const [createdKey, setCreatedKey] = useState<CreatedApiKey | null>(null);
  const [name, setName] = useState("");
  const [scope, setScope] = useState("ReadWrite");
  const [ttlDays, setTtlDays] = useState("90");
  const [loading, setLoading] = useState(true);
  const [creating, setCreating] = useState(false);
  const [error, setError] = useState("");

  const refresh = useCallback(async () => {
    setLoading(true);
    setError("");
    const [keysRes, statsRes] = await Promise.all([apiGet<ApiKeySummary[]>("/account/api-keys"), apiGet<ApiKeyStats>("/account/api-keys/stats")]);
    if (keysRes.ok && keysRes.data) setKeys(keysRes.data);
    else setError(keysRes.error ?? "Falha ao carregar chaves");
    if (statsRes.ok && statsRes.data) setStats(statsRes.data);
    setLoading(false);
  }, []);

  useEffect(() => { refresh(); }, [refresh]);

  const handleCreate = async () => {
    if (!name.trim()) return;
    setCreating(true);
    setError("");
    const ttl = `${parseInt(ttlDays) || 90}.00:00:00`;
    const res = await apiPost<CreatedApiKey>("/account/api-keys", { name: name.trim(), scope, ttl });
    if (res.ok && res.data) {
      setCreatedKey(res.data);
      setName("");
      setScope("ReadWrite");
      setTtlDays("90");
      refresh();
    } else {
      setError(res.error ?? "Falha ao criar chave");
    }
    setCreating(false);
  };

  const handleRevoke = async (keyId: string, keyName: string) => {
    if (!window.confirm(`Revogar "${keyName}"?`)) return;
    const res = await apiPost<{ ok: boolean }>(`/account/api-keys/${keyId}/revoke`, {});
    if (res.ok) refresh();
    else setError(res.error ?? "Falha ao revogar");
  };

  const [copyMsg, setCopyMsg] = useState("");

  const handleCopyKey = async (fullKey: string) => {
    try {
      await navigator.clipboard.writeText(fullKey);
      setCopyMsg("Copiado!");
      setTimeout(() => setCopyMsg(""), 2000);
    } catch { setCopyMsg("Falha ao copiar"); setTimeout(() => setCopyMsg(""), 2000); }
  };

  const sortedKeys = [...keys].sort((a, b) => {
    if (a.active !== b.active) return a.active ? -1 : 1;
    return new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime();
  });

  return (
    <div style={{ maxWidth: 900, margin: "0 auto" }}>
      <h1 style={{ fontSize: 24, fontWeight: 700, marginBottom: 4 }}>🔑 API Keys</h1>
      <p style={{ color: "#8AA0BC", marginBottom: 20 }}>Gerencie suas chaves de API para acesso programático.</p>

      {error && <p style={{ color: "#FB7185", marginBottom: 12, padding: 8, background: "#1A1A3F", borderRadius: 8 }}>{error}</p>}

      <div className="card">
        <h3 style={{ marginBottom: 12 }}>Visão Geral</h3>
        <div style={{ display: "flex", gap: 16, flexWrap: "wrap" }}>
          {stats ? (
            <>
              <div style={{ textAlign: "center", minWidth: 80 }}><span style={{ fontSize: 24, fontWeight: 700 }}>{stats.total}</span><p style={{ fontSize: 12, color: "#8AA0BC" }}>Total</p></div>
              <div style={{ textAlign: "center", minWidth: 80 }}><span style={{ fontSize: 24, fontWeight: 700, color: "#22C55E" }}>{stats.active}</span><p style={{ fontSize: 12, color: "#8AA0BC" }}>Ativas</p></div>
              <div style={{ textAlign: "center", minWidth: 80 }}><span style={{ fontSize: 24, fontWeight: 700, color: "#FBBF24" }}>{stats.expired}</span><p style={{ fontSize: 12, color: "#8AA0BC" }}>Expiradas</p></div>
              <div style={{ textAlign: "center", minWidth: 80 }}><span style={{ fontSize: 24, fontWeight: 700, color: "#FB7185" }}>{stats.revoked}</span><p style={{ fontSize: 12, color: "#8AA0BC" }}>Revogadas</p></div>
            </>
          ) : <p style={{ color: "#8AA0BC" }}>Carregando...</p>}
        </div>
      </div>

      <div className="card">
        <h3 style={{ marginBottom: 12 }}>Criar Nova Chave</h3>
        <input value={name} onChange={(e) => setName(e.target.value)} placeholder="Nome da chave" style={{ marginBottom: 12 }} />
        <div style={{ display: "flex", gap: 12, marginBottom: 12 }}>
          <select value={scope} onChange={(e) => setScope(e.target.value)} style={{ flex: 1, background: "#0E1727", border: "1px solid #1E293B", color: "#E5EEFC", borderRadius: 8, padding: "10px 14px" }}>
            {ALL_SCOPES.map((s) => <option key={s} value={s}>{s}</option>)}
          </select>
          <input type="number" value={ttlDays} onChange={(e) => setTtlDays(e.target.value)} min={1} max={730} style={{ width: 100 }} />
          <span style={{ display: "flex", alignItems: "center", color: "#8AA0BC", fontSize: 13 }}>dias</span>
        </div>
        <button onClick={handleCreate} disabled={creating || !name.trim()} style={{
          background: "#38BDF8", color: "#03111D", padding: "10px 20px", borderRadius: 10, border: "none", fontWeight: 600, cursor: "pointer", opacity: creating ? 0.6 : 1,
        }}>{creating ? "Criando..." : "Criar Chave"}</button>
      </div>

      {createdKey && (
        <div className="card" style={{ borderLeft: "3px solid #FBBF24" }}>
          <h3 style={{ marginBottom: 8 }}>Chave Criada — Copie agora!</h3>
          <p style={{ color: "#FBBF24", fontSize: 13, marginBottom: 8 }}>{createdKey.warning}</p>
          <div style={{ display: "flex", gap: 8 }}>
            <code style={{ flex: 1, padding: 10, background: "#0E1727", borderRadius: 8, fontSize: 13, wordBreak: "break-all" }}>{createdKey.fullKey}</code>
            <button onClick={() => handleCopyKey(createdKey.fullKey)} style={{ background: "#1E293B", color: "#E5EEFC", padding: "8px 16px", borderRadius: 8, border: "none", cursor: "pointer" }}>{copyMsg || "Copiar"}</button>
          </div>
          <p style={{ color: "#8AA0BC", fontSize: 12, marginTop: 8 }}>Expira em: {new Date(createdKey.expiresAt).toLocaleDateString()}</p>
          <button onClick={() => setCreatedKey(null)} style={{ background: "transparent", color: "#8AA0BC", fontSize: 12, marginTop: 4, cursor: "pointer", border: "none" }}>Descartar</button>
        </div>
      )}

      <div className="card">
        <h3 style={{ marginBottom: 12 }}>Suas Chaves</h3>
        {loading ? <p style={{ color: "#8AA0BC" }}>Carregando...</p> : sortedKeys.length === 0 ? <p style={{ color: "#8AA0BC" }}>Nenhuma chave ainda.</p> : (
          <div style={{ overflowX: "auto" }}>
            <table style={{ width: "100%", fontSize: 13, borderCollapse: "collapse" }}>
              <thead>
                <tr style={{ borderBottom: "1px solid #1E293B" }}>
                  <th style={{ textAlign: "left", padding: "8px 4px", color: "#8AA0BC" }}>Nome</th>
                  <th style={{ textAlign: "left", padding: "8px 4px", color: "#8AA0BC" }}>Prefixo</th>
                  <th style={{ textAlign: "left", padding: "8px 4px", color: "#8AA0BC" }}>Escopo</th>
                  <th style={{ textAlign: "left", padding: "8px 4px", color: "#8AA0BC" }}>Status</th>
                  <th style={{ textAlign: "left", padding: "8px 4px", color: "#8AA0BC" }}>Criação</th>
                  <th style={{ textAlign: "left", padding: "8px 4px", color: "#8AA0BC" }}>Expira</th>
                  <th style={{ textAlign: "left", padding: "8px 4px", color: "#8AA0BC" }}></th>
                </tr>
              </thead>
              <tbody>
                {sortedKeys.map((k) => {
                  const expired = new Date(k.expiresAt) < new Date();
                  return (
                    <tr key={k.keyId} style={{ borderBottom: "1px solid #1E293B" }}>
                      <td style={{ padding: "8px 4px", fontWeight: 600 }}>{k.name}</td>
                      <td style={{ padding: "8px 4px", fontFamily: "monospace", fontSize: 12 }}>{k.keyPrefix}...</td>
                      <td style={{ padding: "8px 4px" }}>{k.scope}</td>
                      <td style={{ padding: "8px 4px", color: k.active && !expired ? "#22C55E" : expired ? "#FB7185" : "#8AA0BC" }}>
                        {k.active && !expired ? "Ativa" : expired ? "Expirada" : "Revogada"}
                      </td>
                      <td style={{ padding: "8px 4px", fontSize: 12 }}>{new Date(k.createdAt).toLocaleDateString()}</td>
                      <td style={{ padding: "8px 4px", fontSize: 12, color: expired ? "#FB7185" : undefined }}>{new Date(k.expiresAt).toLocaleDateString()}</td>
                      <td style={{ padding: "8px 4px" }}>
                        {k.active && !expired && (
                          <button onClick={() => handleRevoke(k.keyId, k.name)} style={{ background: "transparent", color: "#FB7185", border: "1px solid #FB7185", borderRadius: 6, padding: "4px 10px", fontSize: 12, cursor: "pointer" }}>
                            Revogar
                          </button>
                        )}
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>
        )}
      </div>
    </div>
  );
}
