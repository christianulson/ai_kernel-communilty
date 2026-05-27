import { KernelClient } from '../api/client';

// Mock vscode
jest.mock('vscode', () => ({
    workspace: {
        getConfiguration: jest.fn()
    }
}), { virtual: true });

const mockGetConfiguration = (overrides: Record<string, any> = {}) => {
    const vscode = require('vscode');
    vscode.workspace.getConfiguration.mockReturnValue({
        get: (key: string, defaultVal?: any) => overrides[key] ?? defaultVal
    });
};

const mockFetch = (response: any, ok = true) => {
    (global as any).fetch = jest.fn().mockResolvedValue({
        ok,
        headers: { get: () => 'application/json' },
        json: jest.fn().mockResolvedValue(response)
    });
};

const mockFetchError = () => {
    (global as any).fetch = jest.fn().mockRejectedValue(new Error('Connection refused'));
};

describe('KernelClient', () => {
    beforeEach(() => {
        jest.clearAllMocks();
        mockGetConfiguration({ endpoint: 'http://localhost:5000' });
    });

    // ── Health ──
    describe('health()', () => {
        it('should return health data on success', async () => {
            mockFetch({ status: 'ok', ts: '2024-01-01', version: '1.0' });
            const client = new KernelClient();
            const result = await client.health();
            expect(result).toEqual({ status: 'ok', ts: '2024-01-01', version: '1.0' });
        });

        it('should return null on HTTP error', async () => {
            mockFetch(null, false);
            const result = await new KernelClient().health();
            expect(result).toBeNull();
        });

        it('should return null on network error', async () => {
            mockFetchError();
            const result = await new KernelClient().health();
            expect(result).toBeNull();
        });
    });

    // ── runAgent ──
    describe('runAgent()', () => {
        it('should return narration on success', async () => {
            mockFetch({ narration: 'Hello!', activeStages: ['gateway'] });
            const result = await new KernelClient().runAgent('Oi');
            expect(result.narration).toBe('Hello!');
            expect(result.error).toBeUndefined();
        });

        it('should return error on network failure', async () => {
            mockFetchError();
            const result = await new KernelClient().runAgent('Oi');
            expect(result.error).toContain('Erro de conexão');
        });

        it('should send prompt in request body', async () => {
            const fetchMock = jest.fn().mockResolvedValue({
                ok: true, headers: { get: () => 'application/json' },
                json: jest.fn().mockResolvedValue({})
            });
            (global as any).fetch = fetchMock;

            await new KernelClient().runAgent('Teste', 'standalone');
            const body = JSON.parse(fetchMock.mock.calls[0][1].body);
            expect(body.prompt).toBe('Teste');
            expect(body.mode).toBe('standalone');
        });
    });

    // ── getScorecard ──
    describe('getScorecard()', () => {
        it('should return scorecard data', async () => {
            const data = { reliability: 0.95, efficiency: 0.88, safety: 0.99, antiLoop: 0.92, governance: 0.85, overall: 0.92 };
            mockFetch(data);
            const result = await new KernelClient().getScorecard();
            expect(result).toEqual(data);
        });

        it('should return null on error', async () => {
            mockFetchError();
            expect(await new KernelClient().getScorecard()).toBeNull();
        });
    });

    // ── getPolicies ──
    describe('getPolicies()', () => {
        it('should return policies list', async () => {
            const policies = [{ id: 'p1', name: 'Pol 1', domain: 'http', version: '1.0', createdAt: '2024-01-01', isActive: true }];
            mockFetch({ policies });
            const result = await new KernelClient().getPolicies();
            expect(result).toHaveLength(1);
            expect(result![0].name).toBe('Pol 1');
        });

        it('should pass domain filter', async () => {
            const fetchMock = jest.fn().mockResolvedValue({ ok: true, headers: { get: () => 'application/json' }, json: jest.fn().mockResolvedValue({ policies: [] }) });
            (global as any).fetch = fetchMock;
            await new KernelClient().getPolicies('security');
            expect(fetchMock.mock.calls[0][0]).toContain('domain=security');
        });

        it('should return null on error', async () => {
            mockFetchError();
            expect(await new KernelClient().getPolicies()).toBeNull();
        });
    });

    // ── getEpisodes ──
    describe('getEpisodes()', () => {
        it('should return episodes list', async () => {
            const episodes = [{ id: 'e1', goalId: 'g1', status: 'completed', createdAt: '2024-01-01' }];
            mockFetch({ episodes });
            const result = await new KernelClient().getEpisodes();
            expect(result).toHaveLength(1);
        });

        it('should return null on error', async () => {
            mockFetchError();
            expect(await new KernelClient().getEpisodes()).toBeNull();
        });
    });

    // ── getEpisode ──
    describe('getEpisode()', () => {
        it('should return episode detail', async () => {
            const detail = { id: 'e1', goalId: 'g1', status: 'completed', createdAt: '2024-01-01', steps: [{ label: 'Step 1', detail: 'OK', ok: true }] };
            mockFetch(detail);
            const result = await new KernelClient().getEpisode('e1');
            expect(result?.goalId).toBe('g1');
            expect(result?.steps).toHaveLength(1);
        });

        it('should return null on error', async () => {
            mockFetchError();
            expect(await new KernelClient().getEpisode('e1')).toBeNull();
        });
    });

    // ── searchMemory ──
    describe('searchMemory()', () => {
        it('should return search hits', async () => {
            const hits = [{ id: 'h1', content: 'test content', source: 'web', score: 0.9 }];
            mockFetch({ hits, totalCount: 1 });
            const result = await new KernelClient().searchMemory('test');
            expect(result?.hits).toHaveLength(1);
            expect(result?.totalCount).toBe(1);
        });

        it('should POST memory search using the shared runtime contract', async () => {
            const fetchMock = jest.fn().mockResolvedValue({ ok: true, headers: { get: () => 'application/json' }, json: jest.fn().mockResolvedValue({ hits: [], totalCount: 0 }) });
            (global as any).fetch = fetchMock;
            await new KernelClient().searchMemory('test query with spaces');
            expect(fetchMock.mock.calls[0][0]).toContain('/memory/search');
            expect(fetchMock.mock.calls[0][1]).toMatchObject({ method: 'POST' });
            const body = JSON.parse(fetchMock.mock.calls[0][1].body);
            expect(body.query).toBe('test query with spaces');
            expect(body.limit).toBe(20);
        });

        it('should return null on error', async () => {
            mockFetchError();
            expect(await new KernelClient().searchMemory('test')).toBeNull();
        });
    });

    // ── getMemoryMetrics ──
    describe('getMemoryMetrics()', () => {
        it('should return metrics data', async () => {
            mockFetch({ totalChunks: 100, totalDocuments: 20, totalSizeBytes: 1024 });
            const result = await new KernelClient().getMemoryMetrics();
            expect(result?.totalChunks).toBe(100);
        });

        it('should return null on error', async () => {
            mockFetchError();
            expect(await new KernelClient().getMemoryMetrics()).toBeNull();
        });
    });

    // ── getEmotionalState ──
    describe('getEmotionalState()', () => {
        it('should return emotional state on success', async () => {
            mockFetch({ valence: 0.5, arousal: 0.3, motivation: 0.7, updatedAt: '2026-01-01T00:00:00Z' });
            const result = await new KernelClient().getEmotionalState('test-user');
            expect(result?.valence).toBe(0.5);
            expect(result?.arousal).toBe(0.3);
            expect(result?.motivation).toBe(0.7);
        });

        it('should return null on error', async () => {
            mockFetchError();
            expect(await new KernelClient().getEmotionalState('test-user')).toBeNull();
        });
    });

    // ── getStatusMessage ──
    describe('getStatusMessage()', () => {
        it('should return connected message when healthy', async () => {
            mockFetch({ status: 'ok', ts: '2024-01-01', version: '1.0' });
            const msg = await new KernelClient().getStatusMessage();
            expect(msg).toContain('pass-filled');
        });

        it('should return unavailable when health fails', async () => {
            mockFetchError();
            const msg = await new KernelClient().getStatusMessage();
            expect(msg).toContain('Indisponível');
        });
    });

    // ── getBaseUrl ──
    describe('base URL', () => {
        it('should reject non-loopback endpoint in localApi mode and fall back to default', () => {
            mockGetConfiguration({ mode: 'localApi', endpoint: 'http://evil.com:8080' });
            const client = new KernelClient() as any;
            expect((client as any).getBaseUrl()).toBe('http://localhost:5000');
        });

        it('should use sidecar port in embedded mode', () => {
            mockGetConfiguration({ mode: 'embedded', sidecarPort: 9000 });
            const client = new KernelClient();
            expect((client as any).getBaseUrl()).toBe('http://localhost:9000');
        });

        it('should default to 5001 when legacy standalone is enabled without port', () => {
            mockGetConfiguration({ standalone: true });
            const client = new KernelClient();
            expect((client as any).getBaseUrl()).toBe('http://localhost:5001');
        });

        it('should allow non-loopback endpoint in remoteApi mode', () => {
            mockGetConfiguration({ mode: 'remoteApi', endpoint: 'https://api.krnlai.dev' });
            const client = new KernelClient();
            expect((client as any).getBaseUrl()).toBe('https://api.krnlai.dev');
        });
    });

    describe('coding endpoints', () => {
        it('codingExplain should POST to /api/coding/explain', async () => {
            mockGetConfiguration();
            mockFetch({ narration: 'explanation' });
            const client = new KernelClient();
            const result = await client.codingExplain('const x = 1;', 'typescript');
            expect(result.narration).toBe('explanation');
            expect(global.fetch).toHaveBeenCalledWith(
                expect.stringContaining('/api/coding/explain'),
                expect.objectContaining({
                    method: 'POST',
                    body: expect.stringContaining('const x = 1;')
                })
            );
        });

        it('codingFix should POST with diagnostics', async () => {
            mockGetConfiguration();
            mockFetch({ narration: 'fix' });
            const client = new KernelClient();
            const result = await client.codingFix('const x = 1;', ['error: unused var'], 'typescript');
            expect(result.narration).toBe('fix');
        });

        it('codingTest should POST to /api/coding/test', async () => {
            mockGetConfiguration();
            mockFetch({ narration: 'test' });
            const client = new KernelClient();
            const result = await client.codingTest('function foo() {}', 'typescript');
            expect(result.narration).toBe('test');
        });

        it('codingReview should POST to /api/coding/review', async () => {
            mockGetConfiguration();
            mockFetch({ narration: 'review' });
            const client = new KernelClient();
            const result = await client.codingReview('/test.ts', 'content', 'typescript');
            expect(result.narration).toBe('review');
        });

        it('codingExplain should handle HTTP error', async () => {
            mockGetConfiguration();
            mockFetchError();
            const client = new KernelClient();
            const result = await client.codingExplain('code', 'ts');
            expect(result.error).toContain('Erro de conexão');
        });

        it('getPendingApprovals should fetch pending list', async () => {
            mockGetConfiguration();
            mockFetch({ approvals: [{ id: '1', action: 'test', details: [], createdAt: '' }] });
            const client = new KernelClient();
            const result = await client.getPendingApprovals();
            expect(result).toHaveLength(1);
            expect(result[0].id).toBe('1');
        });

        it('respondApproval should POST decision', async () => {
            mockGetConfiguration();
            (global as any).fetch = jest.fn().mockResolvedValue({ ok: true });
            const client = new KernelClient();
            const result = await client.respondApproval('1', 'allowed');
            expect(result).toBe(true);
        });

        it('respondApproval should return false on error', async () => {
            mockGetConfiguration();
            (global as any).fetch = jest.fn().mockRejectedValue(new Error('fail'));
            const client = new KernelClient();
            const result = await client.respondApproval('1', 'rejected');
            expect(result).toBe(false);
        });
    });
});
