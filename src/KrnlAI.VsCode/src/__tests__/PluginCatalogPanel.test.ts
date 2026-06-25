import * as vscode from 'vscode';

function mockPanel() {
    return {
        webview: {
            html: '',
            onDidReceiveMessage: jest.fn(),
            postMessage: jest.fn()
        },
        onDidDispose: jest.fn(),
        reveal: jest.fn(),
        dispose: jest.fn()
    };
}

jest.mock('vscode', () => ({
    window: {
        createWebviewPanel: jest.fn(),
        showInformationMessage: jest.fn(),
        showErrorMessage: jest.fn()
    },
    workspace: {
        getConfiguration: jest.fn(() => ({
            get: jest.fn((_key: string, defaultVal?: any) => defaultVal)
        }))
    },
    ViewColumn: { Beside: 2, One: 1 },
    Disposable: jest.fn(),
    EventEmitter: jest.fn(() => ({ event: jest.fn(), fire: jest.fn() }))
}), { virtual: true });

describe('PluginCatalogPanel', () => {
    beforeEach(() => {
        jest.clearAllMocks();
    });

    it('createOrShow should create a new panel when none exists', () => {
        const panel = mockPanel();
        (vscode.window.createWebviewPanel as jest.Mock).mockReturnValue(panel);
        const { PluginCatalogPanel } = require('../panels/pluginCatalogPanel');
        PluginCatalogPanel.createOrShow();
        expect(vscode.window.createWebviewPanel).toHaveBeenCalledWith(
            'krnlai.pluginCatalog', 'Krnl-AI - Plugin Catalog',
            expect.any(Number), expect.objectContaining({ enableScripts: true })
        );
    });

    it('createOrShow should reuse existing panel', () => {
        const panel = mockPanel();
        (vscode.window.createWebviewPanel as jest.Mock).mockReturnValue(panel);
        const mod1 = require('../panels/pluginCatalogPanel');
        // Force reset currentPanel for clean test
        mod1.PluginCatalogPanel.currentPanel = undefined;
        const { PluginCatalogPanel } = require('../panels/pluginCatalogPanel');
        PluginCatalogPanel.createOrShow();
        PluginCatalogPanel.createOrShow();
        expect(panel.reveal).toHaveBeenCalledTimes(1);
        expect(vscode.window.createWebviewPanel).toHaveBeenCalledTimes(1);
    });

    it('createOrShow should set HTML content', () => {
        const panel = mockPanel();
        (vscode.window.createWebviewPanel as jest.Mock).mockReturnValue(panel);
        const mod2 = require('../panels/pluginCatalogPanel');
        mod2.PluginCatalogPanel.currentPanel = undefined;
        const { PluginCatalogPanel } = require('../panels/pluginCatalogPanel');
        PluginCatalogPanel.createOrShow();
        expect(panel.webview.html).toContain('Plugin Catalog');
    });

    it('should handle load message', async () => {
        const panel = mockPanel();
        (vscode.window.createWebviewPanel as jest.Mock).mockReturnValue(panel);
        const mod3 = require('../panels/pluginCatalogPanel');
        mod3.PluginCatalogPanel.currentPanel = undefined;
        const { PluginCatalogPanel } = require('../panels/pluginCatalogPanel');
        PluginCatalogPanel.createOrShow();
        const onMessage = panel.webview.onDidReceiveMessage.mock.calls[0][0];
        await onMessage({ type: 'load' });
        expect(panel.webview.postMessage).toHaveBeenCalledWith(
            expect.objectContaining({ type: 'plugins' })
        );
    });

    it('should handle install message', async () => {
        const panel = mockPanel();
        (vscode.window.createWebviewPanel as jest.Mock).mockReturnValue(panel);
        const mod4 = require('../panels/pluginCatalogPanel');
        mod4.PluginCatalogPanel.currentPanel = undefined;
        const { PluginCatalogPanel } = require('../panels/pluginCatalogPanel');
        PluginCatalogPanel.createOrShow();
        const onMessage = panel.webview.onDidReceiveMessage.mock.calls[0][0];
        await onMessage({ type: 'install', pluginId: 'test-plugin' });
        const postCalls = panel.webview.postMessage.mock.calls;
        const installResult = postCalls.find((c: any) => c[0].type === 'installResult');
        expect(installResult).toBeDefined();
    });
});
