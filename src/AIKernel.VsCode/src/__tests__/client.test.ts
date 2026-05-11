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
        mockGetConfiguration({ endpoint: 'http://test:5000' });
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

        it('should URL-encode query', async () => {
            const fetchMock = jest.fn().mockResolvedValue({ ok: true, headers: { get: () => 'application/json' }, json: jest.fn().mockResolvedValue({ hits: [], totalCount: 0 }) });
            (global as any).fetch = fetchMock;
            await new KernelClient().searchMemory('test query with spaces');
            expect(fetchMock.mock.calls[0][0]).toContain('q=test%20query%20with%20spaces');
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
        it('should reject non-loopback endpoint and fall back to default', () => {
            mockGetConfiguration({ endpoint: 'http://evil.com:8080' });
            const client = new KernelClient() as any;
            expect((client as any).getBaseUrl()).toBe('http://localhost:5000');
        });

        it('should use sidecar port when standalone', () => {
            mockGetConfiguration({ standalone: true, sidecarPort: 9000 });
            const client = new KernelClient();
            expect((client as any).getBaseUrl()).toBe('http://localhost:9000');
        });

        it('should default to 5001 when standalone without port', () => {
            mockGetConfiguration({ standalone: true });
            const client = new KernelClient();
            expect((client as any).getBaseUrl()).toBe('http://localhost:5001');
        });
    });
});
