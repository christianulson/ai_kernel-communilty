import * as vscode from 'vscode';
import { StreamingHandler } from '../chat/StreamingHandler';

export interface AgentRunResponse { narration?: string; error?: string; transportSteps?: { label: string; detail: string; ok: boolean }[]; activeStages?: string[]; }
export interface HealthResponse { status: string; ts: string; version: string; }
export interface ScorecardData { reliability: number; efficiency: number; safety: number; antiLoop: number; governance: number; overall: number; }
export interface PolicyInfo { id: string; name: string; domain: string; version: string; createdAt: string; isActive: boolean; }
export interface EpisodeInfo { id: string; goalId: string; status: string; createdAt: string; durationMs?: number; }
export interface EpisodeDetail { id: string; goalId: string; status: string; createdAt: string; durationMs?: number; steps?: { label: string; detail: string; ok: boolean }[]; }
export interface MemoryHit { id: string; content: string; source: string; score: number; }
export interface MemoryMetrics { totalChunks?: number; totalDocuments?: number; totalSizeBytes?: number; }
export interface EmotionalState { valence: number; arousal: number; motivation: number; updatedAt: string; }
export interface PendingApprovalDTO { id: string; action: string; details: string[]; createdAt: string; }

export class KernelClient {
    private getBaseUrl(): string {
        const config = vscode.workspace.getConfiguration('aikernel');
        if (config.get<boolean>('standalone', false)) {
            return `http://localhost:${config.get<number>('sidecarPort', 5001)}`;
        }
        const endpoint = config.get<string>('endpoint', 'http://localhost:5000');
        try {
            const url = new URL(endpoint);
            if (url.hostname !== 'localhost' && url.hostname !== '127.0.0.1' && url.hostname !== '::1') {
                console.warn(`[AI Kernel] Endpoint rejeitado (não é loopback): ${endpoint}. Usando default.`);
                return 'http://localhost:5000';
            }
        } catch {
            console.warn(`[AI Kernel] Endpoint inválido: ${endpoint}. Usando default.`);
            return 'http://localhost:5000';
        }
        return endpoint;
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
        const params = domain ? `?domain=${encodeURIComponent(domain)}` : '';
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

    async getEmotionalState(userId = 'dev-user'): Promise<EmotionalState | null> {
        return this.fetchJson(`/profile/emotional?userId=${encodeURIComponent(userId)}`);
    }

    private _describeMood(v: number, a: number): string {
        if (v > 0.3) return a < 0.4 ? '😌 Tranquilo' : '⚡ Animado';
        if (v < -0.3) return a < 0.4 ? '😮‍💨 Cansado' : '😰 Tenso';
        return a >= 0.4 ? '🧐 Atento' : '😐 Neutro';
    }

    async getStatusMessage(): Promise<string> {
        const health = await this.health();
        if (!health) return '$(circle-slash) Indisponível';
        const emotional = await this.getEmotionalState();
        const mood = emotional ? this._describeMood(emotional.valence, emotional.arousal) : '';
        return mood ? `$(pass-filled) ${health.version || 'Conectado'} · ${mood}` : `$(pass-filled) ${health.version || 'Conectado'}`;
    }

    private async _codingRequest(endpoint: string, body: any): Promise<AgentRunResponse> {
        try {
            const res = await fetch(`${this.getBaseUrl()}${endpoint}`, {
                method: 'POST', headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(body)
            });
            if (!res.headers.get('content-type')?.includes('application/json')) {
                return { narration: undefined, error: 'Resposta inválida do servidor' };
            }
            return await res.json();
        } catch (ex: any) { return { narration: undefined, error: `Erro de conexão: ${ex.message}` }; }
    }

    async codingExplain(code: string, language?: string): Promise<AgentRunResponse> {
        return this._codingRequest('/api/coding/explain', { code, language });
    }

    async codingFix(code: string, diagnostics: string[], language?: string): Promise<AgentRunResponse> {
        return this._codingRequest('/api/coding/fix', { code, diagnostics, language });
    }

    async codingTest(code: string, language?: string): Promise<AgentRunResponse> {
        return this._codingRequest('/api/coding/test', { code, language });
    }

    async codingReview(filePath: string, content: string, language?: string): Promise<AgentRunResponse> {
        return this._codingRequest('/api/coding/review', { filePath, content, language });
    }

    async codingStreamExplain(
        code: string,
        language: string | undefined,
        onChunk: (chunk: string) => void,
        onComplete: (full: string) => void,
        onError: (err: Error) => void
    ): Promise<void> {
        const handler = new StreamingHandler();
        await handler.streamFromUrl(
            `${this.getBaseUrl()}/api/coding/explain`,
            { code, language },
            onChunk, onComplete, onError
        );
    }

    async streamRunAgent(
        prompt: string,
        mode: string,
        onChunk: (chunk: string) => void,
        onComplete: (full: string) => void,
        onError: (err: Error) => void
    ): Promise<void> {
        const handler = new StreamingHandler();
        await handler.streamFromUrl(
            `${this.getBaseUrl()}/agent/run`,
            { prompt, mode },
            onChunk, onComplete, onError
        );
    }

    async getPendingApprovals(): Promise<PendingApprovalDTO[]> {
        const r = await this.fetchJson<{ approvals: PendingApprovalDTO[] }>('/api/coding/approvals/pending');
        return r?.approvals || [];
    }

    async respondApproval(id: string, decision: 'allowed' | 'rejected'): Promise<boolean> {
        try {
            const res = await fetch(`${this.getBaseUrl()}/api/coding/approvals/${id}/respond`, {
                method: 'POST', headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ decision })
            });
            return res.ok;
        } catch { return false; }
    }
}
