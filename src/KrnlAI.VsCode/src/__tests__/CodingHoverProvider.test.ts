jest.mock('vscode', () => ({
    Hover: jest.fn().mockImplementation((content: any, range?: any) => ({ content, range })),
    MarkdownString: jest.fn().mockImplementation(() => ({
        appendMarkdown: jest.fn(),
        isTrusted: false,
    })),
    Position: class { constructor(line: number, char: number) { (this as any).line = line; (this as any).character = char; } },
    Range: class { constructor(start: any, end: any) { (this as any).start = start; (this as any).end = end; } },
}), { virtual: true });

import { CodingHoverProvider } from '../codingAgent/CodingHoverProvider';

const mockFetch = jest.fn();
global.fetch = mockFetch as any;

describe('CodingHoverProvider', () => {
    let provider: CodingHoverProvider;
    let mockDoc: any;
    let mockPos: any;
    let mockToken: any;

    beforeEach(() => {
        jest.clearAllMocks();
        provider = new CodingHoverProvider(() => 'http://localhost:5235');
        mockFetch.mockReset();

        mockDoc = {
            uri: { fsPath: '/workspace/src/app.ts' },
            languageId: 'typescript',
            getText: jest.fn().mockImplementation((range?: any) => {
                if (!range) return 'const x = 1;';
                return 'calculateTotal';
            }),
            getWordRangeAtPosition: jest.fn().mockReturnValue({ start: { line: 5, character: 4 }, end: { line: 5, character: 18 } }),
            lineAt: jest.fn().mockReturnValue({ text: '  const total = calculateTotal(items);' }),
        };

        mockPos = { line: 5, character: 14 };
        mockToken = {
            isCancellationRequested: false,
            onCancellationRequested: jest.fn(),
        };
    });

    it('CodingHoverProvider_NoWordRange_ShouldReturnUndefined', async () => {
        mockDoc.getWordRangeAtPosition = jest.fn().mockReturnValue(null);
        const result = await provider.provideHover(mockDoc, mockPos, mockToken);
        expect(result).toBeUndefined();
    });

    it('CodingHoverProvider_NumericSymbol_ShouldReturnUndefined', async () => {
        mockDoc.getText = jest.fn().mockReturnValue('12345');
        mockDoc.getWordRangeAtPosition = jest.fn().mockReturnValue({ start: { line: 0, character: 0 }, end: { line: 0, character: 5 } });
        const result = await provider.provideHover(mockDoc, mockPos, mockToken);
        expect(result).toBeUndefined();
    });

    it('CodingHoverProvider_BlockedPath_ShouldReturnUndefined', async () => {
        mockDoc.uri.fsPath = '/workspace/node_modules/pkg/index.js';
        const result = await provider.provideHover(mockDoc, mockPos, mockToken);
        expect(result).toBeUndefined();
    });

    it('CodingHoverProvider_LargeFile_ShouldReturnUndefined', async () => {
        mockDoc.getText = jest.fn().mockImplementation((range?: any) => {
            if (!range) return 'x'.repeat(200_000);
            return 'calculateTotal';
        });
        const result = await provider.provideHover(mockDoc, mockPos, mockToken);
        expect(result).toBeUndefined();
    });

    it('CodingHoverProvider_CacheHit_ShouldReturnWithoutFetch', async () => {
        provider['_setCache']('typescript:calculateTotal', 'A function that calculates total.', 'calculateTotal');

        const result = await provider.provideHover(mockDoc, mockPos, mockToken);

        expect(result).toBeDefined();
        expect(mockFetch).not.toHaveBeenCalled();
    });

    it('CodingHoverProvider_ApiSuccess_ShouldReturnHover', async () => {
        mockFetch.mockResolvedValueOnce({
            ok: true,
            json: async () => ({ narration: 'This function calculates the total of items.' }),
        });

        const result = await provider.provideHover(mockDoc, mockPos, mockToken);

        expect(result).toBeDefined();
        expect(mockFetch).toHaveBeenCalledTimes(1);
    });

    it('CodingHoverProvider_ApiError_ShouldReturnUndefined', async () => {
        mockFetch.mockResolvedValueOnce({ ok: false });

        const result = await provider.provideHover(mockDoc, mockPos, mockToken);
        expect(result).toBeUndefined();
    });

    it('CodingHoverProvider_ClearCache_ShouldWork', () => {
        provider['_setCache']('key', 'val', 'sym');
        expect(provider['_cache'].size).toBe(1);
        provider.clearCache();
        expect(provider['_cache'].size).toBe(0);
    });
});
