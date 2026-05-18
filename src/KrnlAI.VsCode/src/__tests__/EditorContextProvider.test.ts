jest.mock('vscode', () => {
    const mockDocument = {
        getText: jest.fn(() => 'const x = 1;'),
        uri: { fsPath: '/test/file.ts' },
        fileName: '/test/file.ts',
        languageId: 'typescript',
        lineCount: 1
    };

    return {
        window: {
            activeTextEditor: {
                document: mockDocument,
                selection: { isEmpty: false, active: { line: 0, character: 0 } },
                selections: [{ isEmpty: false }]
            },
            visibleTextEditors: [
                { document: { ...mockDocument, uri: { fsPath: '/test/file.ts' } } },
                { document: { ...mockDocument, uri: { fsPath: '/test/other.ts' }, fileName: '/test/other.ts', languageId: 'typescript' } }
            ],
            showInformationMessage: jest.fn()
        },
        workspace: {
            getConfiguration: jest.fn(() => ({ get: jest.fn() })),
            findFiles: jest.fn((pattern: string) => {
                if (pattern.includes('..') || pattern.includes('malicious') ||
                    /[<>|:;*?{}[\]!()&@#$%^=+~`'"]/.test(pattern.replace('**/', ''))) return [];
                return [{ fsPath: '/workspace/found.ts' }];
            }),
            openTextDocument: jest.fn(() => ({ getText: jest.fn(() => 'found content') }))
        },
        languages: {
            getDiagnostics: jest.fn(() => [
                [
                    { fsPath: '/test/file.ts' },
                    [
                        { message: 'Test error', severity: 0, source: 'ts' },
                        { message: 'Test warning', severity: 1, source: 'ts' },
                        { message: 'Test info', severity: 2, source: 'eslint' }
                    ]
                ]
            ]),
            registerCodeLensProvider: jest.fn(() => ({ dispose: jest.fn() }))
        },
        EventEmitter: jest.fn(() => ({ event: jest.fn() })),
        Disposable: jest.fn(() => ({ dispose: jest.fn() })),
        RelativePattern: jest.fn(),
        DiagnosticSeverity: { Error: 0, Warning: 1, Information: 2, Hint: 3 }
    };
}, { virtual: true });

import { EditorContextProvider } from '../codingAgent/EditorContextProvider';

describe('EditorContextProvider', () => {
    let provider: EditorContextProvider;

    beforeEach(() => {
        provider = new EditorContextProvider();
    });

    describe('getActiveEditorContent', () => {
        it('ShouldReturnEditorContent_WhenEditorIsActive', () => {
            const content = provider.getActiveEditorContent();
            expect(content).toBe('const x = 1;');
        });

        it('ShouldReturnUndefined_WhenNoActiveEditor', () => {
            jest.resetModules();
            jest.doMock('vscode', () => ({
                window: { activeTextEditor: undefined, visibleTextEditors: [] },
                workspace: { getConfiguration: jest.fn(() => ({ get: jest.fn() })) },
                languages: { getDiagnostics: jest.fn(() => []) }
            }), { virtual: true });
            const { EditorContextProvider: ECP } = require('../codingAgent/EditorContextProvider');
            const p = new ECP();
            expect(p.getActiveEditorContent()).toBeUndefined();
        });
    });

    describe('getSelection', () => {
        it('ShouldReturnSelection_WhenTextIsSelected', () => {
            const selection = provider.getSelection();
            expect(selection).toBe('const x = 1;');
        });

        it('ShouldReturnNull_WhenSelectionIsEmpty', () => {
            jest.resetModules();
            jest.doMock('vscode', () => ({
                window: {
                    activeTextEditor: {
                        document: { getText: jest.fn() },
                        selection: { isEmpty: true }
                    },
                    visibleTextEditors: []
                },
                workspace: { getConfiguration: jest.fn(() => ({ get: jest.fn() })) },
                languages: { getDiagnostics: jest.fn(() => []) }
            }), { virtual: true });
            const { EditorContextProvider: ECP } = require('../codingAgent/EditorContextProvider');
            const p = new ECP();
            expect(p.getSelection()).toBeNull();
        });
    });

    describe('getVisibleFiles', () => {
        it('ShouldReturnListOfVisibleFilePaths', () => {
            const files = provider.getVisibleFiles();
            expect(files).toEqual(['/test/file.ts', '/test/other.ts']);
        });
    });

    describe('getWorkspaceDiagnostics', () => {
        it('ShouldReturnFormattedDiagnostics', () => {
            const diags = provider.getWorkspaceDiagnostics();
            expect(diags).toHaveLength(3);
            expect(diags[0]).toEqual({ message: 'Test error', severity: 'error', source: 'ts' });
            expect(diags[1]).toEqual({ message: 'Test warning', severity: 'warning', source: 'ts' });
            expect(diags[2]).toEqual({ message: 'Test info', severity: 'info', source: 'eslint' });
        });
    });

    describe('getCurrentFilePath', () => {
        it('ShouldReturnActiveEditorFilePath', () => {
            expect(provider.getCurrentFilePath()).toBe('/test/file.ts');
        });
    });

    describe('getCurrentLanguage', () => {
        it('ShouldReturnActiveEditorLanguage', () => {
            expect(provider.getCurrentLanguage()).toBe('typescript');
        });
    });

    describe('getFullContext', () => {
        it('ShouldReturnCompleteEditorContext', async () => {
            const ctx = await provider.getFullContext();
            expect(ctx).toHaveProperty('activeFile', '/test/file.ts');
            expect(ctx).toHaveProperty('language', 'typescript');
            expect(ctx).toHaveProperty('content', 'const x = 1;');
            expect(ctx).toHaveProperty('selection', 'const x = 1;');
            expect(ctx).toHaveProperty('visibleFiles');
            expect(ctx.visibleFiles).toHaveLength(2);
            expect(ctx).toHaveProperty('diagnostics');
            expect(ctx.diagnostics).toHaveLength(3);
        });

        it('ShouldLimitContentToDefaultMaxLength', async () => {
            jest.resetModules();
            jest.doMock('vscode', () => ({
                window: {
                    activeTextEditor: {
                        document: {
                            getText: jest.fn(() => 'x'.repeat(100000)),
                            uri: { fsPath: '/test/file.ts' }
                        },
                        selection: { isEmpty: true }
                    },
                    visibleTextEditors: []
                },
                workspace: { getConfiguration: jest.fn(() => ({ get: jest.fn() })), findFiles: jest.fn(), openTextDocument: jest.fn() },
                languages: { getDiagnostics: jest.fn(() => []) }
            }), { virtual: true });
            const { EditorContextProvider: ECP } = require('../codingAgent/EditorContextProvider');
            const p = new ECP();
            const ctx = await p.getFullContext();
            expect(ctx.content!.length).toBe(50000);
        });
    });

    describe('resolveFileReference - security', () => {
        it('ShouldRejectPathTraversal', async () => {
            const result = await provider.resolveFileReference('@file:../../etc/passwd');
            expect(result).toBeUndefined();
        });

        it('ShouldRejectAbsolutePath', async () => {
            const result = await provider.resolveFileReference('@file:/etc/passwd');
            expect(result).toBeUndefined();
        });

        it('ShouldRejectGlobPatterns', async () => {
            const result = await provider.resolveFileReference('@file:**/*.env');
            expect(result).toBeUndefined();
        });

        it('ShouldRejectSpecialChars', async () => {
            const result = await provider.resolveFileReference('@file:file;rm -rf /');
            expect(result).toBeUndefined();
        });

        it('ShouldRejectNonFilePrefix', async () => {
            const result = await provider.resolveFileReference('not-a-file-ref');
            expect(result).toBeUndefined();
        });
    });
});
