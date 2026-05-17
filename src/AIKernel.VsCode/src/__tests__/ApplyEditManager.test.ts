import { ApplyEditManager, FileChange } from '../codingAgent/ApplyEditManager';

// Mock vscode
jest.mock('vscode', () => ({
    Uri: {
        file: jest.fn().mockImplementation((path: string) => ({ fsPath: path, path })),
    },
    Range: jest.fn().mockImplementation((startLine: any, startChar: any, endLine: any, endChar: any) => ({
        start: { line: startLine, character: startChar },
        end: { line: endLine, character: endChar },
    })),
    WorkspaceEdit: jest.fn().mockImplementation(() => ({
        replace: jest.fn(),
    })),
    window: {
        showInformationMessage: jest.fn(),
        showErrorMessage: jest.fn(),
        showTextDocument: jest.fn(),
    },
    commands: {
        executeCommand: jest.fn(),
    },
    workspace: {
        openTextDocument: jest.fn().mockImplementation((uriOrContent: any) => {
            if (typeof uriOrContent === 'object' && uriOrContent.fsPath) {
                return Promise.resolve({
                    uri: uriOrContent,
                    getText: () => 'original content',
                    positionAt: (offset: number) => ({ line: 0, character: offset }),
                });
            }
            return Promise.resolve({
                uri: { fsPath: '/tmp/diff', path: '/tmp/diff' },
                getText: () => uriOrContent?.content || '',
                positionAt: (offset: number) => ({ line: 0, character: offset }),
            });
        }),
        applyEdit: jest.fn().mockResolvedValue(true),
    },
}), { virtual: true });

describe('ApplyEditManager', () => {
    let manager: ApplyEditManager;
    let mockApprovalGate: { requestApproval: jest.Mock };
    let sampleChange: FileChange;

    beforeEach(() => {
        jest.clearAllMocks();
        manager = new ApplyEditManager();
        mockApprovalGate = {
            requestApproval: jest.fn().mockResolvedValue('allowed'),
        };
        sampleChange = {
            filePath: '/workspace/src/app.ts',
            originalContent: 'const x = 1;',
            newContent: 'const x = 2;',
            label: 'Correção',
        };
    });

    it('ApplyEditManager_ApplyChange_ShouldApplyEdit', async () => {
        const result = await manager.applyChange(sampleChange, mockApprovalGate);
        expect(result).toBe(true);
        const { workspace } = require('vscode');
        expect(workspace.applyEdit).toHaveBeenCalledTimes(1);
    });

    it('ApplyEditManager_ApplyChange_Rejected_ShouldNotApply', async () => {
        mockApprovalGate.requestApproval.mockResolvedValue('rejected');
        const result = await manager.applyChange(sampleChange, mockApprovalGate);
        expect(result).toBe(false);
        const { workspace } = require('vscode');
        expect(workspace.applyEdit).not.toHaveBeenCalled();
    });

    it('ApplyEditManager_ApplyChange_NoApprovalGate_ShouldApplyDirectly', async () => {
        const result = await manager.applyChange(sampleChange);
        expect(result).toBe(true);
        const { workspace } = require('vscode');
        expect(workspace.applyEdit).toHaveBeenCalledTimes(1);
    });

    it('ApplyEditManager_ApplyMultiFile_ShouldApplyAll', async () => {
        const changes: FileChange[] = [
            sampleChange,
            { ...sampleChange, filePath: '/workspace/src/bar.ts', newContent: 'const y = 3;', label: 'Refactor' },
        ];
        const result = await manager.applyMultiFile(changes, 'Refatoração geral', mockApprovalGate);
        expect(result).toBe(true);
        const { workspace } = require('vscode');
        expect(workspace.applyEdit).toHaveBeenCalledTimes(2);
    });

    it('ApplyEditManager_ApplyMultiFile_Empty_ShouldReturnTrue', async () => {
        const result = await manager.applyMultiFile([], 'vazio');
        expect(result).toBe(true);
    });

    it('ApplyEditManager_ApplyMultiFile_Rejected_ShouldNotApply', async () => {
        mockApprovalGate.requestApproval.mockResolvedValue('rejected');
        const result = await manager.applyMultiFile([sampleChange], 'Refactor', mockApprovalGate);
        expect(result).toBe(false);
        const { workspace } = require('vscode');
        expect(workspace.applyEdit).not.toHaveBeenCalled();
    });

    it('ApplyEditManager_ShowDiff_ShouldOpenDiffView', async () => {
        await manager.showDiff(
            '/workspace/src/app.ts',
            'const x = 1;',
            'const x = 2;',
            'Correção'
        );
        const { commands } = require('vscode');
        expect(commands.executeCommand).toHaveBeenCalledWith(
            'vscode.diff',
            expect.any(Object),
            expect.any(Object),
            'Correção: app.ts'
        );
    });

    it('ApplyEditManager_ApplyWithDiff_ShouldShowDiffThenApply', async () => {
        const result = await manager.applyWithDiff(sampleChange, mockApprovalGate);
        expect(result).toBe(true);
        const { commands } = require('vscode');
        expect(commands.executeCommand).toHaveBeenCalledWith(
            'vscode.diff',
            expect.any(Object),
            expect.any(Object),
            expect.stringContaining('Correção')
        );
        const { workspace } = require('vscode');
        expect(workspace.applyEdit).toHaveBeenCalledTimes(1);
    });

    it('ApplyEditManager_ApplyWithDiff_Rejected_ShouldNotApply', async () => {
        mockApprovalGate.requestApproval.mockResolvedValue('rejected');
        const result = await manager.applyWithDiff(sampleChange, mockApprovalGate);
        expect(result).toBe(false);
        const { workspace } = require('vscode');
        expect(workspace.applyEdit).not.toHaveBeenCalled();
    });
});
