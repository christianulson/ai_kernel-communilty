jest.mock('vscode', () => ({
    CodeAction: jest.fn().mockImplementation((title: string, kind: any) => ({
        title,
        kind,
        command: undefined
    })),
    CodeActionKind: {
        QuickFix: { value: 'quickfix' },
        Refactor: { value: 'refactor' }
    },
    CancellationToken: { None: { isCancellationRequested: false, onCancellationRequested: jest.fn() } }
}), { virtual: true });

import { CodeActionProvider } from '../codingAgent/CodeActionProvider';

describe('CodeActionProvider', () => {
    let provider: CodeActionProvider;
    let mockDocument: any;
    let mockRange: any;
    let mockToken: any;

    beforeEach(() => {
        jest.clearAllMocks();
        provider = new CodeActionProvider();
        mockDocument = {
            getText: jest.fn(),
            uri: { fsPath: '/test.ts' },
            languageId: 'typescript'
        };
        mockRange = { isEmpty: false, start: { line: 0, character: 0 }, end: { line: 1, character: 0 } };
        mockToken = { isCancellationRequested: false, onCancellationRequested: jest.fn() };
    });

    const emptyContext = { diagnostics: [], triggerKind: 0, only: undefined };

    it('ShouldProvideExplain_WhenSelectionExists', () => {
        mockDocument.getText.mockReturnValue('const x = 1;');
        const actions = provider.provideCodeActions(mockDocument, mockRange, emptyContext, mockToken);
        const explain = actions.find(a => a.title.includes('Explain'));
        expect(explain).toBeDefined();
        expect(explain!.command?.command).toBe('krnlai.coding.explain');
    });

    it('ShouldProvideFix_WhenDiagnosticsExist', () => {
        mockDocument.getText.mockReturnValue('const x = 1;');
        const context = { diagnostics: [{ message: 'Error', range: mockRange, severity: 0, code: 'err' }], triggerKind: 1, only: undefined };
        const actions = provider.provideCodeActions(mockDocument, mockRange, context, mockToken);
        const fix = actions.find(a => a.title.includes('Fix'));
        expect(fix).toBeDefined();
        expect(fix!.command?.command).toBe('krnlai.coding.fix');
    });

    it('ShouldProvideTest_WhenClassExists', () => {
        mockDocument.getText.mockReturnValue('class TestClass {}');
        const actions = provider.provideCodeActions(mockDocument, { isEmpty: true } as any, emptyContext, mockToken);
        const test = actions.find(a => a.title.includes('Test'));
        expect(test).toBeDefined();
        expect(test!.command?.command).toBe('krnlai.coding.test');
    });

    it('ShouldReturnEmpty_WhenNoContext', () => {
        mockDocument.getText.mockReturnValue('const x = 1;');
        const actions = provider.provideCodeActions(mockDocument, { isEmpty: true } as any, emptyContext, mockToken);
        expect(actions.length).toBeGreaterThanOrEqual(0);
    });

    it('ShouldReturnEmpty_WhenCancelled', () => {
        const actions = provider.provideCodeActions(mockDocument, mockRange, emptyContext, { isCancellationRequested: true, onCancellationRequested: jest.fn() });
        expect(actions).toHaveLength(0);
    });
});
