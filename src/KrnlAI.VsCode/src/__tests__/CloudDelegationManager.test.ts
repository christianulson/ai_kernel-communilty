let mockConfig: any = {};

function resetMockConfig() {
    mockConfig = {
        enabled: true,
        endpoint: 'https://cloud.krnlai.dev',
        apiKey: 'test-key',
        maxExecutionTime: 300,
    };
}

jest.mock('vscode', () => ({
    window: {
        withProgress: jest.fn().mockImplementation(async (_opts: any, task: any) => {
            const progress = { report: jest.fn() };
            const token = { isCancellationRequested: false, onCancellationRequested: jest.fn() };
            return task(progress, token);
        }),
        showWarningMessage: jest.fn(),
    },
    workspace: {
        getConfiguration: jest.fn().mockImplementation(() => ({
            get: jest.fn().mockImplementation((key: string, defaultVal?: any) => {
                return (mockConfig as any)[key] ?? defaultVal;
            }),
        })),
    },
    ProgressLocation: { Notification: 1, Window: 2 },
}), { virtual: true });

import { CloudDelegationManager, DelegationStatus } from '../codingAgent/CloudDelegationManager';

const mockFetch = jest.fn();
global.fetch = mockFetch as any;

describe('CloudDelegationManager', () => {
    let manager: CloudDelegationManager;
    let mockContext: any;

    beforeEach(() => {
        jest.clearAllMocks();
        resetMockConfig();
        mockFetch.mockReset();
        manager = new CloudDelegationManager();
        mockContext = {
            activeFile: '/workspace/src/test.ts',
            language: 'typescript',
            content: 'const x = 1;',
            selection: null,
            visibleFiles: [],
            diagnostics: [],
        };
    });

    it('CloudDelegationManager_Disabled_ShouldReturnFailed', async () => {
        mockConfig.enabled = false;
        const result = await manager.delegate('test task', mockContext);
        expect(result.status).toBe(DelegationStatus.Failed);
        expect(result.error).toContain('not enabled');
    });

    it('CloudDelegationManager_Enabled_ShouldStartDelegation', async () => {
        mockFetch
            .mockResolvedValueOnce({
                ok: true,
                json: async () => ({ id: 'delegate_123' }),
            })
            .mockResolvedValue({
                ok: true,
                json: async () => ({ status: 'completed', result: 'Task done', narration: 'Task done' }),
            });

        const result = await manager.delegate('test task', mockContext);
        expect(result.status).toBe(DelegationStatus.Completed);
        expect(result.result).toBeTruthy();
    });

    it('CloudDelegationManager_ApiError_ShouldReturnFailed', async () => {
        mockFetch.mockResolvedValueOnce({ ok: false, status: 500, text: async () => 'Server error' });

        const result = await manager.delegate('test', mockContext);
        expect(result.status).toBe(DelegationStatus.Failed);
        expect(result.error).toContain('500');
    });

    it('CloudDelegationManager_NetworkError_ShouldReturnFailed', async () => {
        mockFetch.mockRejectedValueOnce(new Error('Network error'));

        const result = await manager.delegate('test', mockContext);
        expect(result.status).toBe(DelegationStatus.Failed);
        expect(result.error).toContain('Network error');
    });

    it('CloudDelegationManager_ListDelegations_ShouldReturnSorted', async () => {
        mockFetch
            .mockResolvedValueOnce({
                ok: true,
                json: async () => ({ id: 'd1' }),
            })
            .mockResolvedValue({
                ok: true,
                json: async () => ({ status: 'completed', result: 'OK' }),
            });

        await manager.delegate('task 1', mockContext);
        const list = manager.listDelegations();
        expect(list.length).toBe(1);
    });

    it('CloudDelegationManager_GetDelegation_ShouldReturnById', () => {
        const result = manager.getDelegation('nonexistent');
        expect(result).toBeUndefined();
    });

    it('CloudDelegationManager_ClearCompleted_ShouldRemoveDone', async () => {
        mockFetch
            .mockResolvedValueOnce({
                ok: true,
                json: async () => ({ id: 'd1' }),
            })
            .mockResolvedValue({
                ok: true,
                json: async () => ({ status: 'completed', result: 'OK' }),
            });

        await manager.delegate('task', mockContext);
        expect(manager.listDelegations().length).toBe(1);
        manager.clearCompleted();
        expect(manager.listDelegations().length).toBe(0);
    });
});
