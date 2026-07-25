jest.mock('vscode', () => ({
    window: {
        createWebviewPanel: jest.fn(),
        showInformationMessage: jest.fn(),
        showErrorMessage: jest.fn()
    },
    workspace: {
        getConfiguration: jest.fn()
    },
    ViewColumn: { One: 1, Beside: 2 },
    Disposable: class { dispose() { } },
    EventEmitter: class { event = jest.fn(); fire = jest.fn(); }
}), { virtual: true });

function mockPanel() {
    return {
        webview: { html: '', onDidReceiveMessage: jest.fn(), postMessage: jest.fn() },
        onDidDispose: jest.fn(),
        reveal: jest.fn(),
        dispose: jest.fn()
    };
}

const mockClient = {
    getQARuns: jest.fn(),
    runQATest: jest.fn(),
    getQARun: jest.fn(),
    getQARunEvidence: jest.fn()
};

jest.mock('../api/client', () => ({
    KernelClient: jest.fn(() => mockClient)
}));

describe('QAPanel', () => {
    let QAPanel: any;
    const vscode = require('vscode');

    beforeEach(() => {
        jest.clearAllMocks();
        jest.useFakeTimers();
        const mod = require('../panels/qaPanel');
        QAPanel = mod.QAPanel;
        QAPanel.currentPanel = undefined;
    });

    afterEach(() => {
        jest.useRealTimers();
    });

    describe('createOrShow', () => {
        it('should create a new panel when none exists', () => {
            const panel = mockPanel();
            vscode.window.createWebviewPanel.mockReturnValue(panel);

            QAPanel.createOrShow();

            expect(vscode.window.createWebviewPanel).toHaveBeenCalledWith(
                'krnlai.qa', 'Krnl-AI - QA Tests',
                vscode.ViewColumn.Beside,
                { enableScripts: true, retainContextWhenHidden: true }
            );
            expect(panel.webview.html).toContain('QA Tests');
        });

        it('should reuse existing panel', () => {
            const panel = mockPanel();
            vscode.window.createWebviewPanel.mockReturnValue(panel);

            QAPanel.createOrShow();
            QAPanel.createOrShow();

            expect(vscode.window.createWebviewPanel).toHaveBeenCalledTimes(1);
        });
    });

    describe('message handling', () => {
        it('should load runs on load message', async () => {
            const panel = mockPanel();
            vscode.window.createWebviewPanel.mockReturnValue(panel);
            QAPanel.createOrShow();

            const runs = [{ id: 'QA-001', targetType: 'Smoke', status: 'Passed', steps: [], results: 'OK', evidence: [] }];
            mockClient.getQARuns.mockResolvedValue(runs);

            const handler = panel.webview.onDidReceiveMessage.mock.calls[0][0];
            await handler({ type: 'load' });

            expect(panel.webview.postMessage).toHaveBeenCalledWith({ type: 'runs', runs });
        });

        it('should run test on runTest message', async () => {
            const panel = mockPanel();
            vscode.window.createWebviewPanel.mockReturnValue(panel);
            QAPanel.createOrShow();

            const run = { id: 'QA-002', targetType: 'Api', status: 'Running', steps: [], results: '', evidence: [] };
            mockClient.runQATest.mockResolvedValue(run);

            const handler = panel.webview.onDidReceiveMessage.mock.calls[0][0];
            await handler({ type: 'runTest', targetType: 'Api', endpoint: 'http://localhost', timeoutSeconds: 30 });

            expect(mockClient.runQATest).toHaveBeenCalledWith({
                targetType: 'Api', endpoint: 'http://localhost', timeoutSeconds: 30, testNames: undefined
            });
        });

        it('should fetch evidence on getEvidence message', async () => {
            const panel = mockPanel();
            vscode.window.createWebviewPanel.mockReturnValue(panel);
            QAPanel.createOrShow();

            mockClient.getQARunEvidence.mockResolvedValue(['output.log', 'screenshot.png']);

            const handler = panel.webview.onDidReceiveMessage.mock.calls[0][0];
            await handler({ type: 'getEvidence', id: 'QA-001' });

            expect(panel.webview.postMessage).toHaveBeenCalledWith({
                type: 'evidence', id: 'QA-001', evidence: ['output.log', 'screenshot.png']
            });
        });
    });
});

export {};
