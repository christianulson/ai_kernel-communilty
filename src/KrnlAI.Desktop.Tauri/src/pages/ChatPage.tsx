import { useState, useRef, useEffect } from "react";
import { apiPost } from "../api/client";

interface Message {
  role: "user" | "assistant" | "error";
  content: string;
}

export default function ChatPage() {
  const [input, setInput] = useState("");
  const [messages, setMessages] = useState<Message[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");
  const bottomRef = useRef<HTMLDivElement>(null);

  useEffect(() => { bottomRef.current?.scrollIntoView(); }, [messages]);

  const sendMessage = async () => {
    if (!input.trim()) return;
    const userMsg: Message = { role: "user", content: input };
    setMessages((prev) => [...prev, userMsg]);
    setInput("");
    setLoading(true);
    setError("");

    const res = await apiPost<{ narration: string }>("/agent/run", { prompt: input, mode: "kernel" });
    if (res.ok && res.data) {
      setMessages((prev) => [...prev, { role: "assistant", content: res.data.narration }]);
    } else {
      setMessages((prev) => [...prev, { role: "error", content: res.error ?? "Sem resposta do servidor." }]);
      setError(res.error ?? "");
    }
    setLoading(false);
  };

  return (
    <div style={{ maxWidth: 800, margin: "0 auto", display: "flex", flexDirection: "column", height: "calc(100vh - 48px)" }}>
      <h1 style={{ fontSize: 24, fontWeight: 700, marginBottom: 4 }}>💬 Chat</h1>
      <p style={{ color: "#8AA0BC", marginBottom: 16 }}>Converse com o agente Krnl-AI.</p>

      {error && <p style={{ color: "#FB7185", fontSize: 13, marginBottom: 8 }}>{error}</p>}

      <div className="card" style={{ flex: 1, overflow: "auto", marginBottom: 16, display: "flex", flexDirection: "column", gap: 12 }}>
        {messages.length === 0 && <p style={{ color: "#8AA0BC", textAlign: "center", marginTop: 40 }}>Nenhuma mensagem ainda. Envie algo!</p>}
        {messages.map((msg, i) => (
          <div key={i} style={{
            padding: 12, borderRadius: 10, maxWidth: "85%",
            alignSelf: msg.role === "user" ? "flex-end" : "flex-start",
            background: msg.role === "user" ? "#1E3A5F" : msg.role === "error" ? "#3F1A1A" : "#1A2E3F",
          }}>
            <strong style={{ fontSize: 12, color: msg.role === "user" ? "#38BDF8" : "#8AA0BC" }}>
              {msg.role === "user" ? "Você" : msg.role === "error" ? "Erro" : "Krnl-AI"}
            </strong>
            <p style={{ marginTop: 4, lineHeight: 1.5, whiteSpace: "pre-wrap" }}>{msg.content}</p>
          </div>
        ))}
        {loading && (
          <div style={{ alignSelf: "flex-start", padding: 12, borderRadius: 10, background: "#1A2E3F" }}>
            <p style={{ color: "#8AA0BC" }}>Processando...</p>
          </div>
        )}
        <div ref={bottomRef} />
      </div>

      <div style={{ display: "flex", gap: 8 }}>
        <textarea value={input} onChange={(e) => setInput(e.target.value)}
          onKeyDown={(e) => e.key === "Enter" && !e.shiftKey && (e.preventDefault(), sendMessage())}
          placeholder="Digite sua mensagem..." rows={2}
          style={{ flex: 1, resize: "none", background: "#0E1727", border: "1px solid #1E293B", color: "#E5EEFC", borderRadius: 8, padding: "10px 14px", fontSize: 14 }}
        />
        <button onClick={sendMessage} disabled={loading}
          style={{ background: "#38BDF8", color: "#03111D", padding: "10px 24px", alignSelf: "flex-end", borderRadius: 10, border: "none", fontWeight: 600, cursor: "pointer", opacity: loading ? 0.6 : 1 }}>
          {loading ? "..." : "Enviar"}
        </button>
      </div>
    </div>
  );
}
