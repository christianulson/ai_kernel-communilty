jest.mock('vscode', () => {
    const pushSubs: any[] = [];
    return {
        window: {
            createWebviewPanel: jest.fn(() => ({
                webview: {
                    html: '',
                    onDidReceiveMessage: jest.fn(),
                    postMessage: jest.fn()
                },
                onDidDispose: jest.fn(cb => pushSubs.push(cb)),
                reveal: jest.fn(),
                dispose: jest.fn()
            })),
            activeTextEditor: undefined,
            visibleTextEditors: [],
            onDidChangeActiveTextEditor: jest.fn(() => ({ dispose: jest.fn() })),
            showInformationMessage: jest.fn()
        },
        workspace: {
            getConfiguration: jest.fn(() => ({ get: jest.fn() })),
            onDidChangeConfiguration: jest.fn(() => ({ dispose: jest.fn() })),
            openTextDocument: jest.fn()
        },
        languages: {
            getDiagnostics: jest.fn(() => []),
            onDidChangeDiagnostics: jest.fn(() => ({ dispose: jest.fn() })),
            registerCodeLensProvider: jest.fn(() => ({ dispose: jest.fn() }))
        },
        commands: { registerCommand: jest.fn() },
        EventEmitter: jest.fn(() => ({ event: jest.fn() })),
        TreeItem: jest.fn(),
        TreeItemCollapsibleState: { None: 0 },
        Disposable: jest.fn(() => ({ dispose: jest.fn() })),
        StatusBarAlignment: { Right: 1 },
        ViewColumn: { Beside: 2 }
    };
}, { virtual: true });

import { ChatViewProvider } from '../chat/ChatViewProvider';

describe('ChatViewProvider', () => {
    let mockClient: any;

    beforeEach(() => {
        jest.clearAllMocks();
        mockClient = {
            runAgent: jest.fn().mockResolvedValue({ narration: 'response' })
        };
        ChatViewProvider.currentPanel = undefined;
    });

    describe('createOrShow', () => {
        it('ShouldCreateNewPanel_WhenNoCurrentPanel', () => {
            ChatViewProvider.createOrShow(mockClient);
            expect(ChatViewProvider.currentPanel).toBeDefined();
        });

        it('ShouldReuseExistingPanel_WhenAlreadyOpen', () => {
            ChatViewProvider.createOrShow(mockClient);
            const panel1 = ChatViewProvider.currentPanel;
            ChatViewProvider.createOrShow(mockClient);
            expect(ChatViewProvider.currentPanel).toBe(panel1);
        });
    });

    describe('dispose', () => {
        it('ShouldClearCurrentPanel_WhenDisposed', () => {
            ChatViewProvider.createOrShow(mockClient);
            ChatViewProvider.currentPanel!.dispose();
            expect(ChatViewProvider.currentPanel).toBeUndefined();
        });

        it('ShouldDisposeWithoutThrowing', () => {
            ChatViewProvider.createOrShow(mockClient);
            const panel = ChatViewProvider.currentPanel!;
            expect(() => panel.dispose()).not.toThrow();
        });
    });

    describe('handleMessage', () => {
        it('ShouldHandleGetContext_AndPushContext', async () => {
            ChatViewProvider.createOrShow(mockClient);
            const panel = ChatViewProvider.currentPanel!;
            const pushContextSpy = jest.spyOn(panel as any, '_pushContext');

            const msgHandler = (panel as any)._handleMessage.bind(panel);
            await msgHandler({ type: 'getContext' });

            expect(pushContextSpy).toHaveBeenCalled();
        });

        it('ShouldHandleSendMessage_AndRespond', async () => {
            ChatViewProvider.createOrShow(mockClient);
            const panel = ChatViewProvider.currentPanel!;
            const mockWebview = (panel as any)._panel.webview;

            const msgHandler = (panel as any)._handleMessage.bind(panel);
            await msgHandler({ type: 'send', text: 'hello' });

            expect(mockClient.runAgent).toHaveBeenCalledWith('hello');
            expect(mockWebview.postMessage).toHaveBeenCalledWith(
                expect.objectContaining({ type: 'done' })
            );
        });

        it('ShouldHandleApprovalResponse', async () => {
            ChatViewProvider.createOrShow(mockClient);
            const panel = ChatViewProvider.currentPanel!;
            const approvalManager = (panel as any)._approvalManager;
            const respondSpy = jest.spyOn(approvalManager, 'respond');

            const msgHandler = (panel as any)._handleMessage.bind(panel);
            await msgHandler({ type: 'approvalResponse', id: 'test-1', decision: 'allowed' });

            expect(respondSpy).toHaveBeenCalledWith('test-1', 'allowed');
        });

        it('ShouldHandleSetApprovalMode', async () => {
            ChatViewProvider.createOrShow(mockClient);
            const panel = ChatViewProvider.currentPanel!;
            const approvalManager = (panel as any)._approvalManager;
            const setModeSpy = jest.spyOn(approvalManager, 'setMode');

            const msgHandler = (panel as any)._handleMessage.bind(panel);
            await msgHandler({ type: 'setApprovalMode', mode: 'safeAgent' });

            expect(setModeSpy).toHaveBeenCalledWith('safeAgent');
        });

        it('ShouldHandleSlashCommandsRequest', async () => {
            ChatViewProvider.createOrShow(mockClient);
            const panel = ChatViewProvider.currentPanel!;
            const mockWebview = (panel as any)._panel.webview;

            const msgHandler = (panel as any)._handleMessage.bind(panel);
            await msgHandler({ type: 'getSlashCommands' });

            expect(mockWebview.postMessage).toHaveBeenCalledWith(
                expect.objectContaining({ type: 'slashCommands' })
            );
        });
    });
});
