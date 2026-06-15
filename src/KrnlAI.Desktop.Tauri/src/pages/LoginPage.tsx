import { useState } from "react";
import { useNavigate } from "react-router-dom";
import { useAuth } from "../App";
import { apiPost } from "../api/client";

export default function LoginPage() {
  const { setToken } = useAuth();
  const navigate = useNavigate();
  const [url, setUrl] = useState(() => localStorage.getItem("krnl_api_base_url") ?? "http://localhost:5235");
  const [username, setUsername] = useState("");
  const [password, setPassword] = useState("");
  const [apiKey, setApiKey] = useState("");
  const [mode, setMode] = useState<"jwt" | "apikey">("jwt");
  const [error, setError] = useState("");
  const [loading, setLoading] = useState(false);

  const handleLogin = async () => {
    setError("");
    setLoading(true);

    localStorage.setItem("krnl_api_base_url", url);

    if (mode === "apikey") {
      if (!apiKey.trim()) { setError("Informe a API key"); setLoading(false); return; }
      setToken(apiKey.trim());
      const verify = await fetch(`${url}/account/api-keys`, { headers: { Authorization: `Bearer ${apiKey.trim()}` } });
      if (!verify.ok) { setError("API key inválida"); setToken(""); setLoading(false); return; }
      navigate("/");
      setLoading(false);
      return;
    }

    if (!username.trim() || !password.trim()) {
      setError("Informe usuário e senha");
      setLoading(false);
      return;
    }

    const res = await apiPost<{ token: string; refreshToken?: string }>("/auth/login", { username, password });
    if (res.ok && res.data?.token) {
      setToken(res.data.token);
      if (res.data.refreshToken) localStorage.setItem("krnl_refresh_token", res.data.refreshToken);
      navigate("/");
    } else {
      setError(res.error ?? "Falha na autenticação");
    }
    setLoading(false);
  };

  return (
    <div style={{ maxWidth: 400, margin: "0 auto", paddingTop: "15vh" }}>
      <div className="card" style={{ padding: 32 }}>
        <h1 style={{ fontSize: 24, fontWeight: 700, marginBottom: 4, textAlign: "center" }}>⚡ Krnl-AI</h1>
        <p style={{ textAlign: "center", color: "#8AA0BC", marginBottom: 24 }}>Desktop Client</p>

        <div style={{ marginBottom: 16 }}>
          <label style={{ fontSize: 12, color: "#8AA0BC", marginBottom: 4, display: "block" }}>Servidor</label>
          <input value={url} onChange={(e) => setUrl(e.target.value)} placeholder="http://localhost:5235" />
        </div>

        <div style={{ display: "flex", gap: 8, marginBottom: 16 }}>
          <button onClick={() => setMode("jwt")} style={{
            flex: 1, padding: "8px 0", fontSize: 13, borderRadius: 8,
            background: mode === "jwt" ? "#38BDF8" : "#1E293B",
            color: mode === "jwt" ? "#03111D" : "#8AA0BC",
            border: "none", cursor: "pointer",
          }}>Usuário/Senha</button>
          <button onClick={() => setMode("apikey")} style={{
            flex: 1, padding: "8px 0", fontSize: 13, borderRadius: 8,
            background: mode === "apikey" ? "#38BDF8" : "#1E293B",
            color: mode === "apikey" ? "#03111D" : "#8AA0BC",
            border: "none", cursor: "pointer",
          }}>API Key</button>
        </div>

        {mode === "jwt" ? (
          <>
            <div style={{ marginBottom: 12 }}>
              <label style={{ fontSize: 12, color: "#8AA0BC", marginBottom: 4, display: "block" }}>Usuário</label>
              <input value={username} onChange={(e) => setUsername(e.target.value)} placeholder="admin" />
            </div>
            <div style={{ marginBottom: 16 }}>
              <label style={{ fontSize: 12, color: "#8AA0BC", marginBottom: 4, display: "block" }}>Senha</label>
              <input type="password" value={password} onChange={(e) => setPassword(e.target.value)} placeholder="••••••••" />
            </div>
          </>
        ) : (
          <div style={{ marginBottom: 16 }}>
            <label style={{ fontSize: 12, color: "#8AA0BC", marginBottom: 4, display: "block" }}>API Key</label>
            <input value={apiKey} onChange={(e) => setApiKey(e.target.value)} placeholder="krnl_..." />
          </div>
        )}

        {error && <p style={{ color: "#FB7185", fontSize: 13, marginBottom: 12 }}>{error}</p>}

        <button onClick={handleLogin} disabled={loading} style={{
          width: "100%", padding: 12, fontSize: 15, fontWeight: 600,
          background: "#38BDF8", color: "#03111D", borderRadius: 10, border: "none",
          opacity: loading ? 0.6 : 1,
        }}>
          {loading ? "Conectando..." : "Entrar"}
        </button>
      </div>
    </div>
  );
}
