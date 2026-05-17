jest.mock('vscode', () => ({
    CompletionItem: jest.fn(),
    CompletionItemKind: { Snippet: 27 },
    window: {
        createTerminal: jest.fn().mockReturnValue({
            show: jest.fn(),
            sendText: jest.fn(),
            dispose: jest.fn(),
        }),
    },
    workspace: {
        workspaceFolders: [{ uri: { fsPath: '/workspace' } }],
    },
}), { virtual: true });

import { SlashCommandManager } from '../codingAgent/SlashCommandManager';
import { KernelClient } from '../api/client';
import { EditorContext } from '../codingAgent/EditorContextProvider';
import { TerminalManager } from '../codingAgent/TerminalManager';
import { GitManager } from '../codingAgent/GitManager';

jest.mock('../api/client');

describe('SlashCommandManager', () => {
    let manager: SlashCommandManager;
    let mockClient: jest.Mocked<KernelClient>;
    let mockContext: EditorContext;
    let terminalManager: TerminalManager;
    let gitManager: GitManager;

    beforeEach(() => {
        jest.clearAllMocks();
        mockClient = new KernelClient() as jest.Mocked<KernelClient>;
        mockClient.runAgent = jest.fn().mockResolvedValue({ narration: 'Resposta do agente' });

        terminalManager = new TerminalManager();
        gitManager = new GitManager();
        manager = new SlashCommandManager(mockClient, terminalManager, gitManager);

        mockContext = {
            activeFile: '/test/file.ts',
            language: 'typescript',
            content: 'function test() { return 1; }',
            selection: 'return 1;',
            visibleFiles: ['/test/file.ts'],
            diagnostics: [],
        };
    });

    describe('initial state', () => {
        it('ShouldHaveDefaultCommandsRegistered', () => {
            const commands = manager.getAll();
            expect(commands.length).toBeGreaterThanOrEqual(15);
            const ids = commands.map(c => c.id);
            expect(ids).toContain('/explain');
            expect(ids).toContain('/fix');
            expect(ids).toContain('/test');
            expect(ids).toContain('/refactor');
            expect(ids).toContain('/review');
            expect(ids).toContain('/doc');
            expect(ids).toContain('/run');
            expect(ids).toContain('/build');
            expect(ids).toContain('/test-cmd');
            expect(ids).toContain('/commit');
            expect(ids).toContain('/diff');
            expect(ids).toContain('/branch');
            expect(ids).toContain('/status');
            expect(ids).toContain('/log');
            expect(ids).toContain('/review-pr');
        });
    });

    describe('get', () => {
        it('ShouldReturnCommand_WhenCommandExists', () => {
            const cmd = manager.get('/explain');
            expect(cmd).toBeDefined();
            expect(cmd!.id).toBe('/explain');
        });

        it('ShouldReturnUndefined_WhenCommandDoesNotExist', () => {
            const cmd = manager.get('/unknown');
            expect(cmd).toBeUndefined();
        });
    });

    describe('register', () => {
        it('ShouldAddNewCommand', () => {
            const handler = jest.fn();
            manager.register({ id: '/custom', description: 'Custom command', handler });
            expect(manager.get('/custom')).toBeDefined();
        });
    });

    describe('parse', () => {
        it('ShouldParseSlashCommand_WhenInputStartsWithSlash', () => {
            const result = manager.parse('/explain this code');
            expect(result.command).toBe('/explain');
            expect(result.args).toBe('this code');
            expect(result.rest).toBe('');
        });

        it('ShouldReturnRestAsInput_WhenNoSlashCommand', () => {
            const result = manager.parse('hello world');
            expect(result.command).toBeUndefined();
            expect(result.args).toBe('');
            expect(result.rest).toBe('hello world');
        });

        it('ShouldReturnUndefinedCommand_WhenSlashCommandIsUnknown', () => {
            const result = manager.parse('/unknown arg');
            expect(result.command).toBeUndefined();
            expect(result.rest).toBe('/unknown arg');
        });
    });

    describe('execute', () => {
        it('ShouldExecuteExplainHandler', async () => {
            await manager.execute('/explain', mockContext);
            expect(mockClient.runAgent).toHaveBeenCalledWith(
                expect.stringContaining('Explique este código')
            );
        });

        it('ShouldExecuteFixHandler_WithDiagnostics', async () => {
            mockContext.diagnostics = [{ message: 'erro', severity: 'error', source: 'ts' }];
            await manager.execute('/fix', mockContext);
            expect(mockClient.runAgent).toHaveBeenCalledWith(
                expect.stringContaining('Corrija este código')
            );
        });

        it('ShouldExecuteTestHandler', async () => {
            await manager.execute('/test', mockContext);
            expect(mockClient.runAgent).toHaveBeenCalledWith(
                expect.stringContaining('testes unitários')
            );
        });

        it('ShouldExecuteRefactorHandler', async () => {
            await manager.execute('/refactor', mockContext);
            expect(mockClient.runAgent).toHaveBeenCalledWith(
                expect.stringContaining('Refatore este código')
            );
        });

        it('ShouldExecuteReviewHandler', async () => {
            await manager.execute('/review', mockContext);
            expect(mockClient.runAgent).toHaveBeenCalledWith(
                expect.stringContaining('revisão de código')
            );
        });

        it('ShouldExecuteDocHandler', async () => {
            await manager.execute('/doc', mockContext);
            expect(mockClient.runAgent).toHaveBeenCalledWith(
                expect.stringContaining('documentação')
            );
        });

        it('ShouldThrow_WhenCommandNotFound', async () => {
            await expect(manager.execute('/unknown', mockContext)).rejects.toThrow('Comando não encontrado');
        });
    });

    describe('getCompletionItems', () => {
        it('ShouldReturnCompletionItemsForAllCommands', () => {
            const items = manager.getCompletionItems();
            expect(items.length).toBe(manager.getAll().length);
            items.forEach(item => {
                expect(item.insertText).toMatch(/^\/[\w-]+ /);
                expect(item.detail).toBeTruthy();
            });
        });
    });
});
