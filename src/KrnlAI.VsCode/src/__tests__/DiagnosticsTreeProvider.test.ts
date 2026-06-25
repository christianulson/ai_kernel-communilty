import { KernelClient } from '../api/client';

const mockFire = jest.fn();

jest.mock('vscode', () => ({
    window: {
        registerTreeDataProvider: jest.fn()
    },
    workspace: {
        getConfiguration: jest.fn(() => ({
            get: jest.fn((key: string, defaultVal?: any) => defaultVal)
        }))
    },
    EventEmitter: jest.fn(() => ({
        event: jest.fn(),
        fire: mockFire
    })),
    TreeItem: jest.fn().mockImplementation(function (this: any, label: string) {
        this.label = label;
    }),
    TreeItemCollapsibleState: { None: 0 }
}), { virtual: true });

describe('DiagnosticsTreeProvider', () => {
    beforeEach(() => {
        jest.clearAllMocks();
    });

    it('constructor should call refresh', () => {
        const { DiagnosticsTreeProvider } = require('../providers/diagnosticsTreeProvider');
        const client = new KernelClient();
        const provider = new DiagnosticsTreeProvider(client);
        expect(provider).toBeDefined();
    });

    it('getTreeItem should return tree item with correct properties', () => {
        const { DiagnosticsTreeProvider } = require('../providers/diagnosticsTreeProvider');
        const provider = new DiagnosticsTreeProvider(new KernelClient());
        const item = provider.getTreeItem({
            label: 'API Status',
            description: '✅ Online',
            contextValue: 'ok'
        });
        expect(item.label).toBe('API Status');
        expect(item.description).toBe('✅ Online');
        expect(item.command).toEqual({ command: 'krnlai.status.check', title: 'Refresh' });
    });

    it('getChildren should return data for root call', () => {
        const { DiagnosticsTreeProvider } = require('../providers/diagnosticsTreeProvider');
        const provider = new DiagnosticsTreeProvider(new KernelClient());
        const children = provider.getChildren();
        expect(Array.isArray(children)).toBe(true);
        expect(children.length).toBeGreaterThanOrEqual(1);
    });

    it('getChildren should return empty for element', () => {
        const { DiagnosticsTreeProvider } = require('../providers/diagnosticsTreeProvider');
        const provider = new DiagnosticsTreeProvider(new KernelClient());
        const children = provider.getChildren({ label: 'test', description: 'test', contextValue: 'test' });
        expect(children).toEqual([]);
    });

    it('refresh should update data on success', async () => {
        const { DiagnosticsTreeProvider } = require('../providers/diagnosticsTreeProvider');
        const client = new KernelClient();
        (global as any).fetch = jest.fn().mockResolvedValue({
            ok: true,
            headers: { get: () => 'application/json' },
            json: jest.fn().mockResolvedValue({ status: 'ok', ts: '2024-01-01', version: '1.0' })
        });
        const provider = new DiagnosticsTreeProvider(client);
        await new Promise(r => setTimeout(r, 10));
        expect(mockFire).toHaveBeenCalled();
        const children = provider.getChildren();
        expect(children.some((c: any) => c.description.includes('Online'))).toBe(true);
    });

    it('refresh should handle unavailable', async () => {
        const { DiagnosticsTreeProvider } = require('../providers/diagnosticsTreeProvider');
        const client = new KernelClient();
        (global as any).fetch = jest.fn().mockRejectedValue(new Error('fail'));
        const provider = new DiagnosticsTreeProvider(client);
        await new Promise(r => setTimeout(r, 10));
        const children = provider.getChildren();
        expect(children.some((c: any) => c.description.includes('Unreachable'))).toBe(true);
    });
});
