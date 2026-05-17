jest.mock('vscode', () => ({
    workspace: {
        workspaceFolders: [{ uri: { fsPath: '/workspace' } }],
        findFiles: jest.fn().mockResolvedValue([]),
        openTextDocument: jest.fn(),
        applyEdit: jest.fn().mockResolvedValue(true),
    },
    window: {
        activeTextEditor: {
            document: { uri: { fsPath: '/workspace/src/test.ts' }, getText: () => 'const x = 1;' },
            selection: { isEmpty: true },
        },
        showInformationMessage: jest.fn(),
        createTerminal: jest.fn().mockReturnValue({
            show: jest.fn(),
            sendText: jest.fn(),
            dispose: jest.fn(),
        }),
    },
    Uri: { file: jest.fn().mockImplementation((p: string) => ({ fsPath: p, path: p })) },
    Range: jest.fn(),
    WorkspaceEdit: jest.fn().mockImplementation(() => ({ replace: jest.fn() })),
    languages: {
        getDiagnostics: jest.fn().mockReturnValue([]),
    },
    commands: { executeCommand: jest.fn() },
}), { virtual: true });

import { AgenticLoopManager } from '../codingAgent/AgenticLoopManager';
import { KernelClient } from '../api/client';
import { TerminalManager } from '../codingAgent/TerminalManager';
import { GitManager } from '../codingAgent/GitManager';

jest.mock('../api/client');

describe('AgenticLoopManager', () => {
    let manager: AgenticLoopManager;
    let mockClient: jest.Mocked<KernelClient>;
    let terminalManager: TerminalManager;
    let gitManager: GitManager;
    let mockContext: any;

    beforeEach(() => {
        jest.clearAllMocks();
        mockClient = new KernelClient() as jest.Mocked<KernelClient>;
        mockClient.runAgent = jest.fn().mockResolvedValue({ narration: 'COMPLETE' });

        terminalManager = new TerminalManager();
        gitManager = new GitManager();
        manager = new AgenticLoopManager(mockClient, undefined, terminalManager, gitManager);

        mockContext = {
            activeFile: '/workspace/src/test.ts',
            language: 'typescript',
            content: 'const x = 1;',
            selection: null,
            visibleFiles: ['/workspace/src/test.ts'],
            diagnostics: [],
        };
    });

    it('AgenticLoopManager_SimpleComplete_ShouldFinish', async () => {
        const result = await manager.executeTask('do nothing', mockContext);
        expect(result.completed).toBe(true);
        expect(result.iterations).toBe(1);
    });

    it('AgenticLoopManager_ShouldLimitIterations', async () => {
        mockClient.runAgent
            .mockResolvedValue({ narration: 'RUN: echo hello' });

        const result = await manager.executeTask('loop forever', mockContext);
        expect(result.iterations).toBeLessThanOrEqual(10);
    });

    it('AgenticLoopManager_FormatResult_ShouldIncludeDetails', () => {
        const result = {
            task: 'add tests',
            iterations: 3,
            completed: true,
            steps: [
                { type: 'read' as const, description: 'Reading file', success: true, file: 'test.ts' },
                { type: 'edit' as const, description: 'Editing file', success: true, file: 'test.ts' },
                { type: 'complete' as const, description: 'Done', success: true },
            ],
            changes: [],
            durationMs: 1500,
        };

        const formatted = manager.formatResult(result);
        expect(formatted).toContain('add tests');
        expect(formatted).toContain('3');
        expect(formatted).toContain('Reading file');
        expect(formatted).toContain('Editing file');
    });

    it('AgenticLoopManager_FormatResult_WithError_ShouldIncludeError', () => {
        const result = {
            task: 'broken task',
            iterations: 5,
            completed: false,
            steps: [{ type: 'error' as const, description: 'Something broke', success: false }],
            changes: [],
            error: 'Max iterations reached',
            durationMs: 5000,
        };

        const formatted = manager.formatResult(result);
        expect(formatted).toContain('broken task');
        expect(formatted).toContain('Max iterations reached');
    });
});
