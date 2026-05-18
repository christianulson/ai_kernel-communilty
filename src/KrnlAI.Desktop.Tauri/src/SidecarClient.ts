const SIDECAR_BASE = "http://127.0.0.1:5001";

export interface HealthResponse {
  status: string;
  version: string;
}

export interface AgentRunRequest {
  prompt: string;
  mode?: string;
}

export interface AgentRunResponse {
  narration: string;
  error?: string;
}

export const SidecarClient = {
  async health(): Promise<HealthResponse> {
    const res = await fetch(`${SIDECAR_BASE}/health`);
    return res.json();
  },

  async runAgent(prompt: string): Promise<AgentRunResponse> {
    const res = await fetch(`${SIDECAR_BASE}/agent/run`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ prompt } satisfies AgentRunRequest),
    });
    return res.json();
  },
};
