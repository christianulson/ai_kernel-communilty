const SIDECAR_BASE = "http://127.0.0.1:5001";

export type RuntimeMode = "embedded" | "localApi" | "remoteApi";

export interface RuntimeConfig {
  mode?: RuntimeMode;
  endpoint?: string;
  sidecarPort?: number;
}

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

let runtimeConfig: Required<RuntimeConfig> = {
  mode: "embedded",
  endpoint: "http://127.0.0.1:5001",
  sidecarPort: 5001,
};

function getBaseUrl(): string {
  if (runtimeConfig.mode === "embedded") {
    return `http://127.0.0.1:${runtimeConfig.sidecarPort}`;
  }

  return runtimeConfig.endpoint.replace(/\/+$/, "");
}

export const SidecarClient = {
  configureRuntime(config: RuntimeConfig): void {
    runtimeConfig = {
      ...runtimeConfig,
      ...config,
      sidecarPort: config.sidecarPort ?? runtimeConfig.sidecarPort,
      endpoint: config.endpoint ?? runtimeConfig.endpoint,
      mode: config.mode ?? runtimeConfig.mode,
    };
  },

  async health(): Promise<HealthResponse> {
    const res = await fetch(`${getBaseUrl()}/health`);
    return res.json();
  },

  async runAgent(prompt: string): Promise<AgentRunResponse> {
    const res = await fetch(`${getBaseUrl()}/agent/run`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ prompt, mode: runtimeConfig.mode } satisfies AgentRunRequest),
    });
    return res.json();
  },
};
