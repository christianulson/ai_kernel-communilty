jest.mock(
    'vscode',
    () => ({
        window: {
            createWebviewPanel: jest.fn(),
            showInformationMessage: jest.fn(),
            showErrorMessage: jest.fn(),
        },
        workspace: {
            getConfiguration: jest.fn(),
        },
        ViewColumn: { One: 1, Beside: 2 },
        Disposable: class {
            dispose() {}
        },
        EventEmitter: class {
            event = jest.fn();
            fire = jest.fn();
        },
    }),
    { virtual: true },
);

function mockPanel() {
    return {
        webview: { html: '', onDidReceiveMessage: jest.fn(), postMessage: jest.fn() },
        onDidDispose: jest.fn(),
        reveal: jest.fn(),
        dispose: jest.fn(),
    };
}

const mockClient = {
    getBacklogItems: jest.fn(),
    createBacklogItem: jest.fn(),
    updateBacklogStatus: jest.fn(),
    deleteBacklogItem: jest.fn(),
};

jest.mock('../api/client', () => ({
    KernelClient: jest.fn(() => mockClient),
}));

describe('BacklogPanel', () => {
    let BacklogPanel: any;
    const vscode = require('vscode');

    beforeEach(() => {
        jest.clearAllMocks();
        const mod = require('../panels/backlogPanel');
        BacklogPanel = mod.BacklogPanel;
        BacklogPanel.currentPanel = undefined;
    });

    describe('createOrShow', () => {
        it('should create a new panel when none exists', () => {
            const panel = mockPanel();
            vscode.window.createWebviewPanel.mockReturnValue(panel);

            BacklogPanel.createOrShow();

            expect(vscode.window.createWebviewPanel).toHaveBeenCalledWith(
                'krnlai.backlog',
                'Krnl-AI - Backlog',
                vscode.ViewColumn.Beside,
                { enableScripts: true, retainContextWhenHidden: true },
            );
            expect(panel.webview.html).toContain('Backlog');
            expect(panel.webview.onDidReceiveMessage).toHaveBeenCalled();
        });

        it('should reuse existing panel', () => {
            const panel = mockPanel();
            vscode.window.createWebviewPanel.mockReturnValue(panel);

            BacklogPanel.createOrShow();
            BacklogPanel.createOrShow();

            expect(vscode.window.createWebviewPanel).toHaveBeenCalledTimes(1);
            expect(panel.reveal).toHaveBeenCalledTimes(1);
        });
    });

    describe('message handling', () => {
        it('should load items on load message', async () => {
            const panel = mockPanel();
            vscode.window.createWebviewPanel.mockReturnValue(panel);
            BacklogPanel.createOrShow();

            const items = [{ id: 'B-001', title: 'Test', status: 'Pending' }];
            mockClient.getBacklogItems.mockResolvedValue(items);

            const handler = panel.webview.onDidReceiveMessage.mock.calls[0][0];
            await handler({ type: 'load' });

            expect(mockClient.getBacklogItems).toHaveBeenCalled();
            expect(panel.webview.postMessage).toHaveBeenCalledWith({ type: 'items', items });
        });

        it('should create item on create message', async () => {
            const panel = mockPanel();
            vscode.window.createWebviewPanel.mockReturnValue(panel);
            BacklogPanel.createOrShow();

            const created = { id: 'B-002', title: 'New', status: 'Pending' };
            mockClient.createBacklogItem.mockResolvedValue(created);
            mockClient.getBacklogItems.mockResolvedValue([created]);

            const handler = panel.webview.onDidReceiveMessage.mock.calls[0][0];
            await handler({
                type: 'create',
                title: 'New',
                description: 'Desc',
                priority: 'High',
                dependencies: [],
                tags: [],
            });

            expect(mockClient.createBacklogItem).toHaveBeenCalledWith({
                title: 'New',
                description: 'Desc',
                priority: 'High',
                dependencies: [],
                tags: [],
            });
            expect(panel.webview.postMessage).toHaveBeenCalledWith({
                type: 'createResult',
                item: created,
                error: undefined,
            });
        });

        it('should update status on updateStatus message', async () => {
            const panel = mockPanel();
            vscode.window.createWebviewPanel.mockReturnValue(panel);
            BacklogPanel.createOrShow();

            const updated = { id: 'B-001', title: 'Test', status: 'InProgress' };
            mockClient.updateBacklogStatus.mockResolvedValue(updated);
            mockClient.getBacklogItems.mockResolvedValue([updated]);

            const handler = panel.webview.onDidReceiveMessage.mock.calls[0][0];
            await handler({ type: 'updateStatus', id: 'B-001', status: 'InProgress' });

            expect(mockClient.updateBacklogStatus).toHaveBeenCalledWith('B-001', 'InProgress');
        });

        it('should delete item on delete message', async () => {
            const panel = mockPanel();
            vscode.window.createWebviewPanel.mockReturnValue(panel);
            BacklogPanel.createOrShow();

            mockClient.deleteBacklogItem.mockResolvedValue(true);
            mockClient.getBacklogItems.mockResolvedValue([]);

            const handler = panel.webview.onDidReceiveMessage.mock.calls[0][0];
            await handler({ type: 'delete', id: 'B-001' });

            expect(mockClient.deleteBacklogItem).toHaveBeenCalledWith('B-001');
            expect(panel.webview.postMessage).toHaveBeenCalledWith({ type: 'deleteResult', id: 'B-001', ok: true });
        });
    });
});

export {};
