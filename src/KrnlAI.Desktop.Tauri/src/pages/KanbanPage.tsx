import { useEffect, useState, useCallback } from "react";
import { apiGet } from "../api/client";

interface KanbanCard {
  id: string;
  description: string;
  status: string;
  progress: number;
  priority: number;
  domain: string | null;
  createdAt: string;
  deadline: string | null;
  subGoals: KanbanCard[] | null;
}

interface KanbanColumn {
  column: string;
  label: string;
  cards: KanbanCard[];
  totalCount: number;
}

interface KanbanResponse {
  columns: KanbanColumn[];
  metadata: { totalGoals: number; totalColumns: number };
}

function priorityBadge(p: number): string {
  const level = Math.min(Math.floor(p), 4);
  const labels = ["P0", "P1", "P2", "P3", "P4"];
  return labels[level] ?? `P${level}`;
}

function priorityColor(p: number): string {
  if (p <= 0) return "#FB7185";
  if (p <= 1) return "#FBBF24";
  return "#8AA0BC";
}

export default function KanbanPage() {
  const [board, setBoard] = useState<KanbanResponse | null>(null);
  const [search, setSearch] = useState("");
  const [daysBack, setDaysBack] = useState("10");
  const [domain, setDomain] = useState("");
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  const fetchBoard = useCallback(async () => {
    setLoading(true);
    setError("");
    const params = new URLSearchParams();
    if (search) params.set("search", search);
    if (daysBack) params.set("daysBack", daysBack);
    if (domain) params.set("domain", domain);
    const qs = params.toString();
    const res = await apiGet<KanbanResponse>(`/api/goals/kanban${qs ? "?" + qs : ""}`);
    if (res.ok && res.data) setBoard(res.data);
    else setError(res.error ?? "Falha ao carregar kanban");
    setLoading(false);
  }, [search, daysBack, domain]);

  useEffect(() => { fetchBoard(); }, [fetchBoard]);

  const columns = board?.columns ?? [];

  return (
    <div style={{ maxWidth: 1200, margin: "0 auto" }}>
      <h1 style={{ fontSize: 24, fontWeight: 700, marginBottom: 4 }}>📋 Kanban</h1>
      <p style={{ color: "#8AA0BC", marginBottom: 20 }}>Gerenciamento visual de objetivos.</p>

      {error && <p style={{ color: "#FB7185", marginBottom: 12 }}>{error}</p>}

      <div className="card">
        <div style={{ display: "flex", gap: 12, flexWrap: "wrap", alignItems: "center" }}>
          <label style={{ fontSize: 13, color: "#8AA0BC", display: "flex", alignItems: "center", gap: 4 }}>
            Dias:
            <input type="number" value={daysBack} onChange={(e) => setDaysBack(e.target.value)} style={{ width: 60, background: "#0E1727", border: "1px solid #1E293B", color: "#E5EEFC", borderRadius: 6, padding: "6px 8px", fontSize: 13 }} />
          </label>
          <input value={search} onChange={(e) => setSearch(e.target.value)} placeholder="Buscar cards..." style={{ flex: 1, minWidth: 150, background: "#0E1727", border: "1px solid #1E293B", color: "#E5EEFC", borderRadius: 8, padding: "8px 12px", fontSize: 13 }} />
          <input value={domain} onChange={(e) => setDomain(e.target.value)} placeholder="Domínio..." style={{ maxWidth: 130, background: "#0E1727", border: "1px solid #1E293B", color: "#E5EEFC", borderRadius: 8, padding: "8px 12px", fontSize: 13 }} />
          <button onClick={fetchBoard} style={{ background: "#38BDF8", color: "#03111D", padding: "8px 16px", borderRadius: 8, border: "none", fontWeight: 600, cursor: "pointer", fontSize: 13 }}>
            Buscar
          </button>
        </div>
      </div>

      {loading ? <p style={{ color: "#8AA0BC" }}>Carregando...</p> : columns.length === 0 ? (
        <p style={{ color: "#8AA0BC", marginTop: 20 }}>Nenhum objetivo encontrado.</p>
      ) : (
        <div style={{ display: "flex", gap: 16, overflowX: "auto", marginTop: 16, minHeight: 400 }}>
          {columns.map((col) => (
            <div key={col.column} style={{ minWidth: 280, maxWidth: 320, flex: 1, display: "flex", flexDirection: "column", gap: 8 }}>
              <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center", padding: "8px 12px", borderRadius: 8, background: "#0E1727", border: "1px solid #1E293B" }}>
                <strong style={{ fontSize: 14 }}>{col.label}</strong>
                <span style={{ background: "#1E293B", color: "#8AA0BC", padding: "2px 8px", borderRadius: 10, fontSize: 12 }}>{col.totalCount}</span>
              </div>
              {col.cards.map((card) => (
                <div key={card.id} style={{ padding: 12, borderRadius: 8, background: "#0E1727", border: "1px solid #1E293B", display: "flex", flexDirection: "column", gap: 6 }}>
                  <div style={{ display: "flex", justifyContent: "space-between", alignItems: "flex-start", gap: 8 }}>
                    <span style={{ fontWeight: 500, fontSize: 13, flex: 1 }}>{card.description}</span>
                    <span style={{ fontSize: 11, fontWeight: 700, padding: "2px 6px", borderRadius: 4, color: priorityColor(card.priority), border: `1px solid ${priorityColor(card.priority)}`, whiteSpace: "nowrap" }}>
                      {priorityBadge(card.priority)}
                    </span>
                  </div>
                  {card.domain && <span style={{ fontSize: 12, color: "#8AA0BC" }}>{card.domain}</span>}
                  <div style={{ display: "flex", alignItems: "center", gap: 8 }}>
                    <div style={{ flex: 1, height: 4, background: "#1E293B", borderRadius: 2, overflow: "hidden" }}>
                      <div style={{ width: `${Math.round(card.progress * 100)}%`, height: "100%", background: card.progress >= 1 ? "#22C55E" : "#38BDF8", borderRadius: 2 }} />
                    </div>
                    <span style={{ fontSize: 11, fontFamily: "monospace" }}>{Math.round(card.progress * 100)}%</span>
                  </div>
                  {card.deadline && <span style={{ fontSize: 12, color: "#8AA0BC" }}>Prazo: {new Date(card.deadline).toLocaleDateString()}</span>}
                  {card.subGoals && card.subGoals.length > 0 && <span style={{ fontSize: 12, color: "#8AA0BC" }}>{card.subGoals.length} sub-objetivos</span>}
                </div>
              ))}
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
