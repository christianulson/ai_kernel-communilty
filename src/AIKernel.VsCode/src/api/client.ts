import * as vscode from 'vscode';

export interface AgentRunResponse { narration?: string; error?: string; transportSteps?: { label: string; detail: string; ok: boolean }[]; activeStages?: string[]; }
export interface HealthResponse { status: string; ts: string; version: string; }
export interface ScorecardData { reliability: number; efficiency: number; safety: number; antiLoop: number; governance: number; overall: number; }
export interface PolicyInfo { id: string; name: string; domain: string; version: string; createdAt: string; isActive: boolean; }
export interface EpisodeInfo { id: string; goalId: string; status: string; createdAt: string; durationMs?: number; }
export interface EpisodeDetail { id: string; goalId: string; status: string; createdAt: string; durationMs?: number; steps?: { label: string; detail: string; ok: boolean }[]; }
export interface MemoryHit { id: string; content: string; source: string; score: number; }
export interface MemoryMetrics { totalChunks?: number; totalDocuments?: number; totalSizeBytes?: number; }

export class KernelClient {
    private getBaseUrl(): string {
        const config = vscode.workspace.getConfiguration('aikernel');
        if (config.get<boolean>('standalone', false)) {
            return `http://localhost:${config.get<number>('sidecarPort', 5001)}`;
        }
        return config.get<string>('endpoint', 'http://localhost:5000');
    }

    private async fetchJson<T>(path: string, options?: RequestInit): Promise<T | null> {
        try {
            const res = await fetch(`${this.getBaseUrl()}${path}`, {
                headers: { 'Content-Type': 'application/json' },
                ...options
            });
            if (!res.ok) return null;
            return await res.json();
        } catch { return null; }
    }

    async health(): Promise<HealthResponse | null> { return this.fetchJson('/health'); }

    async runAgent(prompt: string, mode = 'gateway'): Promise<AgentRunResponse> {
        try {
            const res = await fetch(`${this.getBaseUrl()}/agent/run`, {
                method: 'POST', headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ prompt, mode })
            });
            if (!res.headers.get('content-type')?.includes('application/json')) {
                return { narration: undefined, error: 'Resposta inválida do servidor' };
            }
            return await res.json();
        } catch (ex: any) { return { narration: undefined, error: `Erro de conexão: ${ex.message}` }; }
    }

    async getScorecard(): Promise<ScorecardData | null> { return this.fetchJson('/agent/metrics/scorecard'); }
    async getPolicies(domain?: string): Promise<PolicyInfo[] | null> {
        const params = domain ? `?domain=${domain}` : '';
        const r = await this.fetchJson<{ policies: PolicyInfo[] }>(`/policy/list${params}`);
        return r?.policies || null;
    }
    async getEpisodes(): Promise<EpisodeInfo[] | null> {
        const r = await this.fetchJson<{ episodes: EpisodeInfo[] }>('/episodes/search?pageSize=50');
        return r?.episodes || null;
    }
    async getEpisode(id: string): Promise<EpisodeDetail | null> { return this.fetchJson(`/episodes/${id}`); }
    async searchMemory(query: string): Promise<{ hits: MemoryHit[]; totalCount: number } | null> {
        return this.fetchJson(`/memory/search?q=${encodeURIComponent(query)}&topK=20`);
    }
    async getMemoryMetrics(): Promise<MemoryMetrics | null> { return this.fetchJson('/memory/metrics'); }

    async getStatusMessage(): Promise<string> {
        const health = await this.health();
        if (!health) return '$(circle-slash) Indisponível';
        return `$(pass-filled) ${health.version || 'Conectado'}`;
    }
}
