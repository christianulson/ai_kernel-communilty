import { useState, useEffect, createContext, useContext, useCallback } from "react";
import { Routes, Route, NavLink, Navigate } from "react-router-dom";
import ChatPage from "./pages/ChatPage";
import DashboardPage from "./pages/DashboardPage";
import ApiKeysPage from "./pages/ApiKeysPage";
import PeerRankingPage from "./pages/PeerRankingPage";
import KanbanPage from "./pages/KanbanPage";
import PrivacyPage from "./pages/PrivacyPage";
import SettingsPage from "./pages/SettingsPage";
import LoginPage from "./pages/LoginPage";
import { listenNavigate } from "./TauriBridge";
import "./App.css";

interface AuthContextValue {
  token: string;
  setToken: (t: string) => void;
  clearToken: () => void;
}

const AuthCtx = createContext<AuthContextValue>({ token: "", setToken: () => {}, clearToken: () => {} });
export const useAuth = () => useContext(AuthCtx);

const NAV_ITEMS = [
  ["💬 Chat", "/"],
  ["📊 Dashboard", "/dashboard"],
  ["🔑 API Keys", "/api-keys"],
  ["📈 Peer Ranking", "/peer-ranking"],
  ["📋 Kanban", "/kanban"],
  ["🛡️ Privacy", "/privacy"],
  ["⚙️ Settings", "/settings"],
] as const;

function App() {
  const [token, setTokenState] = useState(() => localStorage.getItem("krnl_token") ?? "");
  const setToken = useCallback((t: string) => { setTokenState(t); localStorage.setItem("krnl_token", t); }, []);
  const clearToken = useCallback(() => { setTokenState(""); localStorage.removeItem("krnl_token"); }, []);

  useEffect(() => {
    let unlisten: (() => void) | undefined;
    listenNavigate((path) => {
      window.history.pushState(null, "", path);
      window.dispatchEvent(new PopStateEvent("popstate"));
    }).then((u) => { unlisten = u; });
    return () => { unlisten?.(); };
  }, []);

  return (
    <AuthCtx.Provider value={{ token, setToken, clearToken }}>
      <div style={{ display: "flex", height: "100vh" }}>
        {token && (
          <nav style={{
            width: 220, background: "#0E1727", padding: 16,
            display: "flex", flexDirection: "column", gap: 4,
          }}>
            <div style={{ display: "flex", alignItems: "center", justifyContent: "space-between", marginBottom: 16 }}>
              <h2 style={{ fontSize: 18, fontWeight: 700, color: "#E5EEFC", margin: 0 }}>⚡ Krnl-AI</h2>
            </div>
            {NAV_ITEMS.map(([label, path]) => (
              <NavLink key={path} to={path} style={({ isActive }) => ({
                padding: "10px 14px", borderRadius: 10, textDecoration: "none", fontSize: 14,
                color: isActive ? "#E5EEFC" : "#8AA0BC",
                background: isActive ? "#1E293B" : "transparent",
                transition: "background 0.15s",
              })}>{label}</NavLink>
            ))}
            <div style={{ flex: 1 }} />
            <button onClick={clearToken} style={{
              background: "transparent", color: "#FB7185", fontSize: 13, textAlign: "left", padding: 8,
            }}>Sair</button>
          </nav>
        )}
        <main style={{
          flex: 1, padding: 24, overflow: "auto",
          background: "#08111F", color: "#E5EEFC",
        }}>
          <Routes>
            <Route path="/login" element={<LoginPage />} />
            <Route path="/" element={token ? <ChatPage /> : <Navigate to="/login" />} />
            <Route path="/dashboard" element={token ? <DashboardPage /> : <Navigate to="/login" />} />
            <Route path="/api-keys" element={token ? <ApiKeysPage /> : <Navigate to="/login" />} />
            <Route path="/peer-ranking" element={token ? <PeerRankingPage /> : <Navigate to="/login" />} />
            <Route path="/kanban" element={token ? <KanbanPage /> : <Navigate to="/login" />} />
            <Route path="/privacy" element={token ? <PrivacyPage /> : <Navigate to="/login" />} />
            <Route path="/settings" element={token ? <SettingsPage /> : <Navigate to="/login" />} />
          </Routes>
        </main>
      </div>
    </AuthCtx.Provider>
  );
}

export default App;
