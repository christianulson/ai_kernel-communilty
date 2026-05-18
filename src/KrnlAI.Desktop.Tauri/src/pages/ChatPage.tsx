import { useState } from "react";
import { SidecarClient } from "../SidecarClient";

export default function ChatPage() {
  const [prompt, setPrompt] = useState("");
  const [response, setResponse] = useState("");
  const [loading, setLoading] = useState(false);

  async function handleSubmit() {
    if (!prompt.trim()) return;
    setLoading(true);
    try {
      const result = await SidecarClient.runAgent(prompt);
      setResponse(result.narration || result.error || "No response");
    } catch (e) {
      setResponse(`Error: ${e}`);
    } finally {
      setLoading(false);
    }
  }

  return (
    <div style={{ padding: 16, maxWidth: 800, margin: "0 auto" }}>
      <h2>Chat</h2>
      <textarea
        value={prompt}
        onChange={(e) => setPrompt(e.target.value)}
        placeholder="Type your message..."
        rows={4}
        style={{ width: "100%", marginBottom: 8, padding: 8 }}
      />
      <button onClick={handleSubmit} disabled={loading}>
        {loading ? "Sending..." : "Send"}
      </button>
      {response && (
        <div
          style={{
            marginTop: 16,
            padding: 12,
            background: "#f0f0f0",
            borderRadius: 8,
            whiteSpace: "pre-wrap",
          }}
        >
          {response}
        </div>
      )}
    </div>
  );
}
