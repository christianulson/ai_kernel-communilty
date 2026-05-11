/**
 * Extension activation tests
 * Mocks vscode API to verify commands and tree view registration
 */
jest.mock('vscode', () => ({
    window: {
        createStatusBarItem: jest.fn(() => ({
            text: '',
            command: '',
            tooltip: '',
            show: jest.fn()
        })),
        createWebviewPanel: jest.fn(() => ({
            webview: { html: '', onDidReceiveMessage: jest.fn(), postMessage: jest.fn() },
            onDidDispose: jest.fn(),
            reveal: jest.fn(),
            dispose: jest.fn()
        })),
        registerTreeDataProvider: jest.fn(),
        showInformationMessage: jest.fn(),
        showErrorMessage: jest.fn()
    },
    commands: {
        registerCommand: jest.fn()
    },
    EventEmitter: jest.fn(() => ({
        event: jest.fn()
    })),
    TreeItem: jest.fn(),
    TreeItemCollapsibleState: { None: 0 },
    Disposable: jest.fn(() => ({ dispose: jest.fn() })),
    StatusBarAlignment: { Right: 1 }
}), { virtual: true });

import * as vscode from 'vscode';

describe('Extension', () => {
    let context: vscode.ExtensionContext;

    beforeEach(() => {
        jest.clearAllMocks();
        context = {
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
            'aikernel.chat', expect.any(Function)
        );
    });

    it('should register dashboard command', async () => {
        const { activate } = require('../extension');
        await activate(context);
        expect(vscode.commands.registerCommand).toHaveBeenCalledWith(
            'aikernel.dashboard', expect.any(Function)
        );
    });

    it('should register policies command', async () => {
        const { activate } = require('../extension');
        await activate(context);
        expect(vscode.commands.registerCommand).toHaveBeenCalledWith(
            'aikernel.policies', expect.any(Function)
        );
    });

    it('should register episodes command', async () => {
        const { activate } = require('../extension');
        await activate(context);
        expect(vscode.commands.registerCommand).toHaveBeenCalledWith(
            'aikernel.episodes', expect.any(Function)
        );
    });

    it('should register memory command', async () => {
        const { activate } = require('../extension');
        await activate(context);
        expect(vscode.commands.registerCommand).toHaveBeenCalledWith(
            'aikernel.memory', expect.any(Function)
        );
    });

    it('should register settings command', async () => {
        const { activate } = require('../extension');
        await activate(context);
        expect(vscode.commands.registerCommand).toHaveBeenCalledWith(
            'aikernel.settings', expect.any(Function)
        );
    });

    it('should register start and stop sidecar commands', async () => {
        const { activate } = require('../extension');
        await activate(context);
        expect(vscode.commands.registerCommand).toHaveBeenCalledWith(
            'aikernel.start', expect.any(Function)
        );
        expect(vscode.commands.registerCommand).toHaveBeenCalledWith(
            'aikernel.stop', expect.any(Function)
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
            'aikernel.nav', expect.any(Object)
        );
    });

    it('should register navigate command', async () => {
        const { activate } = require('../extension');
        await activate(context);
        expect(vscode.commands.registerCommand).toHaveBeenCalledWith(
            'aikernel.navigate', expect.any(Function)
        );
    });

    it('should add all subscriptions to context', async () => {
        const { activate } = require('../extension');
        await activate(context);
        expect(context.subscriptions.length).toBeGreaterThanOrEqual(9);
    });
});
