jest.mock(
    'vscode',
    () => ({
        window: {
            createWebviewPanel: jest.fn(),
            showInformationMessage: jest.fn(),
            showErrorMessage: jest.fn(),
            activeTextEditor: undefined,
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
};

jest.mock('../api/client', () => ({
    KernelClient: jest.fn(() => mockClient),
}));

describe('KanbanPanel', () => {
    let KanbanPanel: any;
    const vscode = require('vscode');

    beforeEach(() => {
        jest.clearAllMocks();
        const mod = require('../panels/KanbanPanel');
        KanbanPanel = mod.KanbanPanel;
        KanbanPanel.currentPanel = undefined;
    });

    describe('createOrShow', () => {
        it('should create a new panel when none exists', () => {
            const panel = mockPanel();
            vscode.window.createWebviewPanel.mockReturnValue(panel);

            KanbanPanel.createOrShow();

            expect(vscode.window.createWebviewPanel).toHaveBeenCalledWith(
                'krnlai.kanban',
                'Kanban',
                vscode.ViewColumn.One,
                { enableScripts: true, retainContextWhenHidden: true },
            );
            expect(panel.webview.html).toContain('Kanban Board');
        });

        it('should reuse existing panel', () => {
            const panel = mockPanel();
            vscode.window.createWebviewPanel.mockReturnValue(panel);

            KanbanPanel.createOrShow();
            KanbanPanel.createOrShow();

            expect(vscode.window.createWebviewPanel).toHaveBeenCalledTimes(1);
        });
    });

    describe('message handling', () => {
        it('should load items on load message', async () => {
            const panel = mockPanel();
            vscode.window.createWebviewPanel.mockReturnValue(panel);
            KanbanPanel.createOrShow();

            const items = [{ id: 'B-001', title: 'Card', status: 'Pending' }];
            mockClient.getBacklogItems.mockResolvedValue(items);

            const handler = panel.webview.onDidReceiveMessage.mock.calls[0][0];
            await handler({ type: 'load' });

            expect(panel.webview.postMessage).toHaveBeenCalledWith({ type: 'items', items });
        });

        it('should move card on move message', async () => {
            const panel = mockPanel();
            vscode.window.createWebviewPanel.mockReturnValue(panel);
            KanbanPanel.createOrShow();

            const updated = { id: 'B-001', title: 'Card', status: 'InProgress' };
            mockClient.updateBacklogStatus.mockResolvedValue(updated);
            mockClient.getBacklogItems.mockResolvedValue([updated]);

            const handler = panel.webview.onDidReceiveMessage.mock.calls[0][0];
            await handler({ type: 'move', id: 'B-001', status: 'InProgress' });

            expect(mockClient.updateBacklogStatus).toHaveBeenCalledWith('B-001', 'InProgress');
        });

        it('should create card on create message', async () => {
            const panel = mockPanel();
            vscode.window.createWebviewPanel.mockReturnValue(panel);
            KanbanPanel.createOrShow();

            const created = { id: 'B-002', title: 'New Card', status: 'Pending' };
            mockClient.createBacklogItem.mockResolvedValue(created);
            mockClient.getBacklogItems.mockResolvedValue([created]);

            const handler = panel.webview.onDidReceiveMessage.mock.calls[0][0];
            await handler({ type: 'create', title: 'New Card', description: 'Desc', priority: 'High' });

            expect(mockClient.createBacklogItem).toHaveBeenCalledWith({
                title: 'New Card',
                description: 'Desc',
                priority: 'High',
            });
        });
    });
});

export {};
