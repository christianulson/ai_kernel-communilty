jest.mock('vscode', () => {
    const CodeLensMock = jest.fn().mockImplementation((range: any, cmd: any) => ({
        range,
        command: cmd
    }));
    return {
        CodeLens: CodeLensMock,
        Range: jest.fn(),
        Position: jest.fn()
    };
}, { virtual: true });

import { CodeLensProvider } from '../codingAgent/CodeLensProvider';
import * as vscode from 'vscode';

describe('CodeLensProvider', () => {
    let provider: CodeLensProvider;
    let mockDocument: any;

    beforeEach(() => {
        jest.clearAllMocks();
        provider = new CodeLensProvider();
        mockDocument = {
            getText: jest.fn(),
            positionAt: jest.fn((offset: number) => ({ line: 0, character: offset })),
            uri: { fsPath: '/test.ts' },
            languageId: 'typescript'
        };
    });

    it('ShouldReturnLenses_ForFunctionDeclaration', () => {
        mockDocument.getText.mockReturnValue('function foo() { return 1; }');
        const lenses = provider.provideCodeLenses(mockDocument);
        expect(lenses.length).toBeGreaterThanOrEqual(3);
        expect(lenses[0].command?.title).toMatch(/Explain/);
        expect(lenses[1].command?.title).toMatch(/Test/);
        expect(lenses[2].command?.title).toMatch(/Review/);
    });

    it('ShouldReturnLenses_ForClassDeclaration', () => {
        mockDocument.getText.mockReturnValue('class MyClass { }');
        const lenses = provider.provideCodeLenses(mockDocument);
        expect(lenses.length).toBeGreaterThanOrEqual(3);
        expect(lenses[0].command?.command).toBe('krnlai.coding.explain');
    });

    it('ShouldReturnLenses_ForArrowFunction', () => {
        mockDocument.getText.mockReturnValue('const fn = () => {};');
        const lenses = provider.provideCodeLenses(mockDocument);
        expect(lenses.length).toBeGreaterThanOrEqual(3);
    });

    it('ShouldReturnEmpty_ForLargeFiles', () => {
        mockDocument.getText.mockReturnValue('x'.repeat(50001));
        const lenses = provider.provideCodeLenses(mockDocument);
        expect(lenses).toHaveLength(0);
    });

    it('ShouldDebounce_WhenCalledRepeatedly', () => {
        mockDocument.getText.mockReturnValue('function a(){} function b(){}');
        provider.provideCodeLenses(mockDocument);
        const second = provider.provideCodeLenses(mockDocument);
        expect(second).toHaveLength(0);
    });

    it('ShouldReturnEmpty_ForNoMatches', () => {
        mockDocument.getText.mockReturnValue('const x = 42;');
        const lenses = provider.provideCodeLenses(mockDocument);
        expect(lenses).toHaveLength(0);
    });
});
