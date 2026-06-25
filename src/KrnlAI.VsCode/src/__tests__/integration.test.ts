import * as vscode from 'vscode';

let mockConfigValues: Record<string, any> = {};
let mockShowInformationMessage = jest.fn();
let mockShowErrorMessage = jest.fn();
let mockShowWarningMessage = jest.fn();

jest.mock('vscode', () => ({
    window: {
        createStatusBarItem: jest.fn(() => ({
            text: '',
            command: '',
            tooltip: '',
            show: jest.fn(),
            dispose: jest.fn()
        })),
        createWebviewPanel: jest.fn(() => ({
            webview: { html: '', onDidReceiveMessage: jest.fn(), postMessage: jest.fn() },
            onDidDispose: jest.fn(),
            reveal: jest.fn(),
            dispose: jest.fn()
        })),
        registerTreeDataProvider: jest.fn(),
        showInformationMessage: (...args: any[]) => mockShowInformationMessage(...args),
        showErrorMessage: (...args: any[]) => mockShowErrorMessage(...args),
        showWarningMessage: (...args: any[]) => mockShowWarningMessage(...args),
        showInputBox: jest.fn(),
        showTextDocument: jest.fn(),
        visibleTextEditors: [],
        activeTextEditor: undefined,
        createTerminal: jest.fn(() => ({ show: jest.fn(), sendText: jest.fn(), dispose: jest.fn() })),
        withProgress: jest.fn()
    },
    workspace: {
        getConfiguration: jest.fn(() => ({
            get: jest.fn((key: string, defaultVal?: any) => {
                return key in mockConfigValues ? mockConfigValues[key] : defaultVal;
            }),
            has: jest.fn(() => true),
            inspect: jest.fn(),
            update: jest.fn()
        })),
        onDidChangeConfiguration: jest.fn(() => ({ dispose: jest.fn() })),
        openTextDocument: jest.fn()
    },
    languages: {
        getDiagnostics: jest.fn(() => []),
        registerCodeLensProvider: jest.fn(() => ({ dispose: jest.fn() })),
        registerCodeActionsProvider: jest.fn(() => ({ dispose: jest.fn() })),
        registerInlineCompletionItemProvider: jest.fn(() => ({ dispose: jest.fn() })),
        registerHoverProvider: jest.fn(() => ({ dispose: jest.fn() }))
    },
    commands: {
        registerCommand: jest.fn(() => ({ dispose: jest.fn() })),
        executeCommand: jest.fn()
    },
    EventEmitter: jest.fn(() => ({
        event: jest.fn(),
        fire: jest.fn()
    })),
    TreeItem: jest.fn().mockImplementation(function (this: any, label: string) {
        this.label = label;
    }),
    TreeItemCollapsibleState: { None: 0 },
    Disposable: jest.fn((dispose?: () => void) => ({ dispose: jest.fn(() => dispose?.()) })),
    StatusBarAlignment: { Right: 1 },
    CodeLens: jest.fn(),
    CompletionItem: jest.fn(),
    CompletionItemKind: { Snippet: 27 },
    RelativePattern: jest.fn(),
    Range: jest.fn(),
    Position: jest.fn(),
    DiagnosticSeverity: { Error: 0, Warning: 1, Information: 2, Hint: 3 },
    ViewColumn: { Beside: 2, One: 1 },
    ProgressLocation: { Notification: 1 }
}), { virtual: true });

function createMockContext(): vscode.ExtensionContext {
    return {
        subscriptions: [],
        extensionPath: '',
        extensionUri: null as any,
        extensionMode: 1,
        globalState: null as any,
        workspaceState: null as any,
        secrets: null as any,
        globalStorageUri: null as any,
        logUri: null as any,
        storageUri: null as any,
        asAbsolutePath: jest.fn(),
        extension: null as any,
        environmentVariableCollection: null as any,
        globalStoragePath: '',
        logPath: '',
        storagePath: ''
    } as any;
}

function disposeContext(context: vscode.ExtensionContext | undefined): void {
    for (const subscription of context?.subscriptions ?? []) {
        subscription.dispose();
    }
}

describe('Extension Integration', () => {
    function getHandler(commandName: string): (...args: any[]) => any {
        const calls = (vscode.commands.registerCommand as jest.Mock).mock.calls;
        const match = calls.find((c: any) => c[0] === commandName);
        if (!match) throw new Error(`Command ${commandName} not registered`);
        return match[1];
    }

    function setup() {
        jest.clearAllMocks();
        mockConfigValues = {};
        mockShowInformationMessage = jest.fn();
        mockShowErrorMessage = jest.fn();
        mockShowWarningMessage = jest.fn();

        (global as any).fetch = jest.fn().mockResolvedValue({
            ok: true,
            headers: { get: () => 'application/json' },
            json: jest.fn().mockResolvedValue({ status: 'ok', version: '1.0' })
        });
    }

    describe('krnlai.status.check', () => {
        it('should show success when healthy', async () => {
            setup();
            const context = createMockContext();
            try {
                const ext = require('../extension');
                await ext.activate(context);
                const handler = getHandler('krnlai.status.check');
                await handler();
                expect(mockShowInformationMessage).toHaveBeenCalledWith(
                    expect.stringContaining('ok')
                );
            } finally {
                disposeContext(context);
            }
        });

        it('should show error when unhealthy', async () => {
            setup();
            (global as any).fetch = jest.fn().mockRejectedValue(new Error('fail'));
            const context = createMockContext();
            try {
                const ext = require('../extension');
                await ext.activate(context);
                const handler = getHandler('krnlai.status.check');
                await handler();
                expect(mockShowErrorMessage).toHaveBeenCalledWith(
                    expect.stringContaining('Unavailable')
                );
            } finally {
                disposeContext(context);
            }
        });
    });

    describe('krnlai.plugins.install', () => {
        it('should show input box and install plugin', async () => {
            setup();
            mockShowInformationMessage.mockResolvedValue(undefined);
            (vscode.window as any).showInputBox = jest.fn().mockResolvedValue('test-plugin');
            const context = createMockContext();
            try {
                const ext = require('../extension');
                await ext.activate(context);
                const handler = getHandler('krnlai.plugins.install');
                await handler();
                expect(mockShowInformationMessage).toHaveBeenCalledWith(
                    expect.stringContaining('test-plugin')
                );
            } finally {
                disposeContext(context);
            }
        });

        it('should handle pluginId argument', async () => {
            setup();
            mockShowInformationMessage.mockResolvedValue(undefined);
            const context = createMockContext();
            try {
                const ext = require('../extension');
                await ext.activate(context);
                const handler = getHandler('krnlai.plugins.install');
                await handler('arg-plugin');
                expect(mockShowInformationMessage).toHaveBeenCalledWith(
                    expect.stringContaining('arg-plugin')
                );
            } finally {
                disposeContext(context);
            }
        });

        it('should do nothing when cancelled', async () => {
            setup();
            (vscode.window as any).showInputBox = jest.fn().mockResolvedValue(undefined);
            const context = createMockContext();
            try {
                const ext = require('../extension');
                await ext.activate(context);
                const handler = getHandler('krnlai.plugins.install');
                await handler();
                expect(mockShowInformationMessage).not.toHaveBeenCalledWith(
                    expect.stringContaining('installed')
                );
            } finally {
                disposeContext(context);
            }
        });
    });

    describe('krnlai.plugins.listCatalog', () => {
        it('should open plugin catalog panel', async () => {
            setup();
            const context = createMockContext();
            try {
                const ext = require('../extension');
                await ext.activate(context);
                const handler = getHandler('krnlai.plugins.listCatalog');
                handler();
                expect(vscode.window.createWebviewPanel).toHaveBeenCalledWith(
                    'krnlai.pluginCatalog', expect.any(String),
                    expect.any(Number), expect.any(Object)
                );
            } finally {
                disposeContext(context);
            }
        });
    });

    describe('krnlai.diagnostics.refresh', () => {
        it('should refresh diagnostics tree', async () => {
            setup();
            const context = createMockContext();
            try {
                const ext = require('../extension');
                await ext.activate(context);
                const handler = getHandler('krnlai.diagnostics.refresh');
                handler();
                expect(vscode.window.registerTreeDataProvider).toHaveBeenCalledWith(
                    'krnlai.diagnostics', expect.any(Object)
                );
            } finally {
                disposeContext(context);
            }
        });
    });

    describe('coding commands', () => {
        it('should register suggestFix and warn when no active editor', async () => {
            setup();
            mockConfigValues = { 'codingAgent.enabled': true };
            const context = createMockContext();
            jest.isolateModules(() => {
                const mod = require('../extension');
                mod.activate(context);
            });
            const handler = getHandler('krnlai.coding.suggestFix');
            expect(handler).toBeDefined();
            await handler();
            expect(mockShowWarningMessage).toHaveBeenCalledWith('Open a file first');
            disposeContext(context);
        });
    });
});
