import { useEffect, useState, useMemo, useCallback } from "react";
import { apiGet, apiPut } from "../api/client";

interface PeerScore {
  nodeId: string;
  overallScore: number;
  successRateScore: number;
  latencyScore: number;
  availabilityScore: number;
  tenureScore: number;
  capacityScore: number;
  catalogScore: number;
  totalJobsExecuted: number;
  totalJobsFailed: number;
  uptimePercentage: number;
  firstSeen: string;
  lastSeen: string;
}

interface RankingWeights {
  successRateWeight: number;
  latencyWeight: number;
  availabilityWeight: number;
  tenureWeight: number;
  capacityWeight: number;
  catalogWeight: number;
}

interface RankingStrategy {
  currentStrategyName: string;
  availableStrategies: string[];
}

interface RankingHistoryEntry {
  nodeId: string;
  eventType: string;
  overallScore: number;
  tier: string;
  delta: number;
  reason: string | null;
  timestamp: string;
}

interface TierGroup {
  tier: string;
  count: number;
  avgScore: number;
}

const TIER_COLORS: Record<string, string> = { Preferred: "#22C55E", Trusted: "#38BDF8", Standard: "#FBBF24", Untrusted: "#FB7185" };
const TIERS = ["All", "Untrusted", "Standard", "Trusted", "Preferred"];
const WEIGHT_KEYS: (keyof RankingWeights)[] = ["successRateWeight", "latencyWeight", "availabilityWeight", "tenureWeight", "capacityWeight", "catalogWeight"];
const WEIGHT_LABELS: Record<keyof RankingWeights, string> = { successRateWeight: "Sucesso", latencyWeight: "Latência", availabilityWeight: "Disponibilidade", tenureWeight: "Tempo", capacityWeight: "Capacidade", catalogWeight: "Catálogo" };

function getTier(score: number): string {
  if (score >= 91) return "Preferred";
  if (score >= 71) return "Trusted";
  if (score >= 31) return "Standard";
  return "Untrusted";
}

export default function PeerRankingPage() {
  const [scores, setScores] = useState<PeerScore[]>([]);
  const [tiers, setTiers] = useState<TierGroup[]>([]);
  const [weights, setWeights] = useState<RankingWeights | null>(null);
  const [strategy, setStrategy] = useState<RankingStrategy | null>(null);
  const [history, setHistory] = useState<RankingHistoryEntry[]>([]);
  const [selectedNode, setSelectedNode] = useState<string | null>(null);
  const [textFilter, setTextFilter] = useState("");
  const [tierFilter, setTierFilter] = useState("All");
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  const refresh = useCallback(async () => {
    setLoading(true);
    setError("");
    const [sRes, tRes, wRes, stRes] = await Promise.all([
      apiGet<PeerScore[]>("/p2p/ranking"),
      apiGet<TierGroup[]>("/p2p/ranking/tiers"),
      apiGet<RankingWeights>("/p2p/ranking/weights"),
      apiGet<RankingStrategy>("/p2p/ranking/strategy"),
    ]);
    if (sRes.ok && sRes.data) setScores(sRes.data);
    else setError(sRes.error ?? "Falha ao carregar scores");
    if (tRes.ok && tRes.data) setTiers(tRes.data);
    if (wRes.ok && wRes.data) setWeights(wRes.data);
    if (stRes.ok && stRes.data) setStrategy(stRes.data);
    setLoading(false);
  }, []);

  useEffect(() => { refresh(); }, [refresh]);

  useEffect(() => {
    if (!selectedNode) { setHistory([]); return; }
    apiGet<RankingHistoryEntry[]>(`/p2p/ranking/history/${selectedNode}`).then((r) => {
      if (r.ok && r.data) setHistory(r.data);
    });
  }, [selectedNode]);

  const filteredScores = useMemo(() => {
    let list = scores;
    if (textFilter) { const q = textFilter.toLowerCase(); list = list.filter((s) => s.nodeId.toLowerCase().includes(q)); }
    if (tierFilter !== "All") list = list.filter((s) => getTier(s.overallScore) === tierFilter);
    return [...list].sort((a, b) => b.overallScore - a.overallScore);
  }, [scores, textFilter, tierFilter]);

  const handleSaveWeights = async () => {
    if (!weights) return;
    const res = await apiPut<{ ok: boolean }>("/p2p/ranking/weights", weights);
    if (res.ok) refresh();
    else setError(res.error ?? "Falha ao salvar pesos");
  };

  return (
    <div style={{ maxWidth: 1000, margin: "0 auto" }}>
      <h1 style={{ fontSize: 24, fontWeight: 700, marginBottom: 4 }}>📈 Peer Ranking</h1>
      <p style={{ color: "#8AA0BC", marginBottom: 20 }}>Pontuações de reputação dos nós da rede.</p>

      {error && <p style={{ color: "#FB7185", marginBottom: 12 }}>{error}</p>}

      <div className="card">
        <h3 style={{ marginBottom: 12 }}>Distribuição por Nível</h3>
        <div style={{ display: "flex", gap: 16, flexWrap: "wrap" }}>
          {tiers.map((g) => (
            <div key={g.tier} style={{ textAlign: "center", minWidth: 100 }}>
              <span style={{ fontSize: 24, fontWeight: 700, color: TIER_COLORS[g.tier] ?? "#E5EEFC" }}>{g.count}</span>
              <p style={{ fontSize: 12, color: "#8AA0BC" }}>{g.tier}</p>
              <p style={{ fontSize: 11, color: "#8AA0BC" }}>Média: {g.avgScore.toFixed(1)}</p>
            </div>
          ))}
        </div>
      </div>

      <div className="card">
        <h3 style={{ marginBottom: 12 }}>Pesos do Ranking</h3>
        {weights ? (
          <div style={{ display: "flex", flexDirection: "column", gap: 8 }}>
            {WEIGHT_KEYS.map((key) => (
              <div key={key} style={{ display: "flex", alignItems: "center", gap: 12 }}>
                <label style={{ minWidth: 110, fontSize: 13, color: "#8AA0BC" }}>{WEIGHT_LABELS[key]}</label>
                <input type="range" min={0} max={100} value={Math.round(weights[key] * 100)}
                  onChange={(e) => setWeights({ ...weights, [key]: parseInt(e.target.value) / 100 })}
                  style={{ flex: 1, maxWidth: 200 }} />
                <span style={{ minWidth: 36, textAlign: "right", fontFamily: "monospace", fontSize: 13 }}>{(weights[key] * 100).toFixed(0)}%</span>
              </div>
            ))}
            <button onClick={handleSaveWeights} style={{ alignSelf: "flex-start", marginTop: 8, background: "#38BDF8", color: "#03111D", padding: "8px 20px", borderRadius: 8, border: "none", fontWeight: 600, cursor: "pointer" }}>
              Salvar Pesos
            </button>
          </div>
        ) : <p style={{ color: "#8AA0BC" }}>Carregando...</p>}
      </div>

      {strategy && (
        <div className="card">
          <h3 style={{ marginBottom: 8 }}>Estratégia: {strategy.currentStrategyName}</h3>
          <div style={{ display: "flex", gap: 8, flexWrap: "wrap" }}>
            {strategy.availableStrategies.filter((s) => s !== strategy.currentStrategyName).map((s) => (
              <button key={s} onClick={async () => { const r = await apiPut<{ ok: boolean }>("/p2p/ranking/strategy", { strategyName: s }); if (r.ok) refresh(); }}
                style={{ background: "#1E293B", color: "#E5EEFC", padding: "6px 14px", borderRadius: 8, border: "none", cursor: "pointer", fontSize: 13 }}>
                Mudar para {s}
              </button>
            ))}
          </div>
        </div>
      )}

      <div className="card">
        <div style={{ display: "flex", gap: 12, marginBottom: 12 }}>
          <input value={textFilter} onChange={(e) => setTextFilter(e.target.value)} placeholder="Filtrar por node ID..." style={{ flex: 1 }} />
          <select value={tierFilter} onChange={(e) => setTierFilter(e.target.value)} style={{ background: "#0E1727", border: "1px solid #1E293B", color: "#E5EEFC", borderRadius: 8, padding: "8px 12px" }}>
            {TIERS.map((t) => <option key={t} value={t}>{t}</option>)}
          </select>
          <span style={{ color: "#8AA0BC", fontSize: 13, display: "flex", alignItems: "center" }}>{filteredScores.length} peers</span>
        </div>

        {loading ? <p style={{ color: "#8AA0BC" }}>Carregando...</p> : filteredScores.length === 0 ? <p style={{ color: "#8AA0BC" }}>Nenhum peer encontrado.</p> : (
          <div style={{ overflowX: "auto", maxHeight: 400, overflowY: "auto" }}>
            <table style={{ width: "100%", fontSize: 12, borderCollapse: "collapse" }}>
              <thead>
                <tr style={{ borderBottom: "1px solid #1E293B", position: "sticky", top: 0, background: "#0E1727" }}>
                  <th style={{ textAlign: "left", padding: "8px 4px", color: "#8AA0BC" }}>Node ID</th>
                  <th style={{ textAlign: "left", padding: "8px 4px", color: "#8AA0BC" }}>Nível</th>
                  <th style={{ textAlign: "left", padding: "8px 4px", color: "#8AA0BC" }}>Score</th>
                  <th style={{ textAlign: "left", padding: "8px 4px", color: "#8AA0BC" }}>Sucesso</th>
                  <th style={{ textAlign: "left", padding: "8px 4px", color: "#8AA0BC" }}>Latência</th>
                  <th style={{ textAlign: "left", padding: "8px 4px", color: "#8AA0BC" }}>Jobs</th>
                  <th style={{ textAlign: "left", padding: "8px 4px", color: "#8AA0BC" }}>Uptime</th>
                </tr>
              </thead>
              <tbody>
                {filteredScores.map((s) => {
                  const tier = getTier(s.overallScore);
                  return (
                    <tr key={s.nodeId} onClick={() => setSelectedNode(s.nodeId === selectedNode ? null : s.nodeId)}
                      style={{ borderBottom: "1px solid #1E293B", cursor: "pointer", background: selectedNode === s.nodeId ? "#1E293B" : undefined }}>
                      <td style={{ padding: "8px 4px", fontFamily: "monospace", fontSize: 11 }}>{s.nodeId}</td>
                      <td style={{ padding: "8px 4px", color: TIER_COLORS[tier], fontWeight: 600 }}>{tier}</td>
                      <td style={{ padding: "8px 4px", fontWeight: 700 }}>{s.overallScore.toFixed(1)}</td>
                      <td style={{ padding: "8px 4px" }}>{(s.successRateScore * 100).toFixed(0)}%</td>
                      <td style={{ padding: "8px 4px" }}>{(s.latencyScore * 100).toFixed(0)}%</td>
                      <td style={{ padding: "8px 4px" }}>{s.totalJobsExecuted}/{s.totalJobsFailed}</td>
                      <td style={{ padding: "8px 4px" }}>{s.uptimePercentage.toFixed(1)}%</td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>
        )}
      </div>

      {selectedNode && history.length > 0 && (
        <div className="card">
          <h3 style={{ marginBottom: 8 }}>Histórico — {selectedNode}</h3>
          <div style={{ overflowX: "auto", maxHeight: 200, overflowY: "auto" }}>
            <table style={{ width: "100%", fontSize: 12, borderCollapse: "collapse" }}>
              <thead>
                <tr style={{ borderBottom: "1px solid #1E293B", position: "sticky", top: 0, background: "#0E1727" }}>
                  <th style={{ textAlign: "left", padding: "6px 4px", color: "#8AA0BC" }}>Evento</th>
                  <th style={{ textAlign: "left", padding: "6px 4px", color: "#8AA0BC" }}>Score</th>
                  <th style={{ textAlign: "left", padding: "6px 4px", color: "#8AA0BC" }}>Δ</th>
                  <th style={{ textAlign: "left", padding: "6px 4px", color: "#8AA0BC" }}>Motivo</th>
                  <th style={{ textAlign: "left", padding: "6px 4px", color: "#8AA0BC" }}>Data</th>
                </tr>
              </thead>
              <tbody>
                {history.map((h, i) => (
                  <tr key={i} style={{ borderBottom: "1px solid #1E293B" }}>
                    <td style={{ padding: "6px 4px" }}>{h.eventType}</td>
                    <td style={{ padding: "6px 4px" }}>{h.overallScore.toFixed(1)}</td>
                    <td style={{ padding: "6px 4px", color: h.delta >= 0 ? "#22C55E" : "#FB7185" }}>{h.delta >= 0 ? "+" : ""}{h.delta.toFixed(1)}</td>
                    <td style={{ padding: "6px 4px", color: "#8AA0BC" }}>{h.reason ?? "—"}</td>
                    <td style={{ padding: "6px 4px", fontSize: 11 }}>{new Date(h.timestamp).toLocaleString()}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}
    </div>
  );
}
