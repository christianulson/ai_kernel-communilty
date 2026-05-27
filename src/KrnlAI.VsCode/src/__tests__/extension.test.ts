/**
 * Extension activation tests
 * Mocks vscode API to verify commands and tree view registration
 */
let mockConfigValues: Record<string, any> = {};

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
        showInformationMessage: jest.fn(),
        showErrorMessage: jest.fn(),
        showTextDocument: jest.fn(),
        visibleTextEditors: [],
        activeTextEditor: undefined
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
        registerCodeActionsProvider: jest.fn(() => ({ dispose: jest.fn() }))
    },
    // Note: mockConfigValues is used by coding tests below
    commands: {
        registerCommand: jest.fn(() => ({ dispose: jest.fn() }))
    },
    EventEmitter: jest.fn(() => ({
        event: jest.fn()
    })),
    TreeItem: jest.fn(),
    TreeItemCollapsibleState: { None: 0 },
    Disposable: jest.fn((dispose?: () => void) => ({ dispose: jest.fn(() => dispose?.()) })),
    StatusBarAlignment: { Right: 1 },
    CodeLens: jest.fn(),
    CodeLensProvider: jest.fn(),
    CompletionItem: jest.fn(),
    CompletionItemKind: { Snippet: 27 },
    RelativePattern: jest.fn(),
    Range: jest.fn(),
    Position: jest.fn(),
    DiagnosticSeverity: { Error: 0, Warning: 1, Information: 2, Hint: 3 }
}), { virtual: true });

import * as vscode from 'vscode';

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

describe('Extension', () => {
    let context: vscode.ExtensionContext;

    beforeEach(() => {
        jest.clearAllMocks();
        context = createMockContext();
    });

    afterEach(() => {
        disposeContext(context);
        require('../extension').deactivate();
    });

    it('should register all 8 commands on activation', async () => {
        const { activate } = require('../extension');
        await activate(context);
        expect(vscode.commands.registerCommand).toHaveBeenCalledTimes(9); // 8 command + 1 navigate
    });

    it('should register chat command', async () => {
        const { activate } = require('../extension');
        await activate(context);
        expect(vscode.commands.registerCommand).toHaveBeenCalledWith(
            'krnlai.chat', expect.any(Function)
        );
    });

    it('should register dashboard command', async () => {
        const { activate } = require('../extension');
        await activate(context);
        expect(vscode.commands.registerCommand).toHaveBeenCalledWith(
            'krnlai.dashboard', expect.any(Function)
        );
    });

    it('should register policies command', async () => {
        const { activate } = require('../extension');
        await activate(context);
        expect(vscode.commands.registerCommand).toHaveBeenCalledWith(
            'krnlai.policies', expect.any(Function)
        );
    });

    it('should register episodes command', async () => {
        const { activate } = require('../extension');
        await activate(context);
        expect(vscode.commands.registerCommand).toHaveBeenCalledWith(
            'krnlai.episodes', expect.any(Function)
        );
    });

    it('should register memory command', async () => {
        const { activate } = require('../extension');
        await activate(context);
        expect(vscode.commands.registerCommand).toHaveBeenCalledWith(
            'krnlai.memory', expect.any(Function)
        );
    });

    it('should register settings command', async () => {
        const { activate } = require('../extension');
        await activate(context);
        expect(vscode.commands.registerCommand).toHaveBeenCalledWith(
            'krnlai.settings', expect.any(Function)
        );
    });

    it('should register start and stop sidecar commands', async () => {
        const { activate } = require('../extension');
        await activate(context);
        expect(vscode.commands.registerCommand).toHaveBeenCalledWith(
            'krnlai.start', expect.any(Function)
        );
        expect(vscode.commands.registerCommand).toHaveBeenCalledWith(
            'krnlai.stop', expect.any(Function)
        );
    });

    it('should create status bar item on activation', async () => {
        const { activate } = require('../extension');
        await activate(context);
        expect(vscode.window.createStatusBarItem).toHaveBeenCalled();
    });

    it('should register tree data provider', async () => {
        const { activate } = require('../extension');
        await activate(context);
        expect(vscode.window.registerTreeDataProvider).toHaveBeenCalledWith(
            'krnlai.nav', expect.any(Object)
        );
    });

    it('should register navigate command', async () => {
        const { activate } = require('../extension');
        await activate(context);
        expect(vscode.commands.registerCommand).toHaveBeenCalledWith(
            'krnlai.navigate', expect.any(Function)
        );
    });

    it('should add all subscriptions to context', async () => {
        const { activate } = require('../extension');
        await activate(context);
        expect(context.subscriptions.length).toBeGreaterThanOrEqual(9);
    });
});

describe('Extension with Coding Agent Enabled', () => {
    let context: vscode.ExtensionContext;

    beforeEach(() => {
        mockConfigValues = { 'codingAgent.enabled': true };
        context = createMockContext();
    });

    afterEach(() => {
        disposeContext(context);
        require('../extension').deactivate();
        mockConfigValues = {};
    });

    it('ShouldRegisterCodingAgentCommands_WhenEnabled', async () => {
        const { activate } = require('../extension');
        await activate(context);
        const calls = (vscode.commands.registerCommand as jest.Mock).mock.calls;
        const ids = calls.map(c => c[0]);
        expect(ids).toContain('krnlai.coding.chat');
        expect(ids).toContain('krnlai.coding.explain');
        expect(ids).toContain('krnlai.coding.fix');
        expect(ids).toContain('krnlai.coding.test');
        expect(ids).toContain('krnlai.coding.refactor');
        expect(ids).toContain('krnlai.coding.review');
    });

    it('ShouldRegisterCodeLensProvider_WhenEnabled', async () => {
        const { activate } = require('../extension');
        await activate(context);
        expect(vscode.languages.registerCodeLensProvider).toHaveBeenCalledWith(
            { scheme: 'file' },
            expect.any(Object)
        );
    });

    it('ShouldNotDuplicateCodingCommands_WhenActivatedTwice', async () => {
        const mod = require('../extension');
        await mod.activate(context);
        const calls1 = (vscode.commands.registerCommand as jest.Mock).mock.calls;
        const codingIds1 = calls1.filter(c => c[0].startsWith('krnlai.coding')).map(c => c[0]);

        const ctx2 = createMockContext();
        try {
            await mod.activate(ctx2);
            const calls2 = (vscode.commands.registerCommand as jest.Mock).mock.calls;
            const codingIds2 = calls2.filter(c => c[0].startsWith('krnlai.coding')).map(c => c[0]);

            expect(codingIds2.length).toBe(codingIds1.length);
        } finally {
            disposeContext(ctx2);
        }
    });
});
