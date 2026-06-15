import { InlineCompletionProvider } from '../codingAgent/InlineCompletionProvider';
import { CompletionCache } from '../codingAgent/CompletionCache';

// Mock vscode types
jest.mock('vscode', () => ({
    InlineCompletionItem: jest.fn().mockImplementation((text: string) => ({ text })),
    InlineCompletionTriggerKind: { Automatic: 0, Explicit: 1 },
    Position: class { constructor(line: number, character: number) { (this as any).line = line; (this as any).character = character; } },
    Range: class { constructor(startLine: number, startChar: number, endLine: number, endChar: number) { (this as any).start = { line: startLine, character: startChar }; (this as any).end = { line: endLine, character: endChar }; } },
}), { virtual: true });

// Mock fetch
const mockFetch = jest.fn();
global.fetch = mockFetch as any;

function createMockDocument(overrides: any = {}): any {
    return {
        uri: { fsPath: '/workspace/src/app.ts' },
        fileName: '/workspace/src/app.ts',
        languageId: 'typescript',
        isUntitled: false,
        encoding: 'utf-8',
        version: 1,
        isDirty: false,
        isClosed: false,
        lineCount: 100,
        eol: 1,
        lineAt: jest.fn(),
        getText: jest.fn().mockImplementation((range?: any) => {
            if (!range) return 'const x = 1;\nconst y = 2;\n';
            return 'function hello() {\n  ';
        }),
        getWordRangeAtPosition: jest.fn(),
        offsetAt: jest.fn(),
        positionAt: jest.fn(),
        validateRange: jest.fn(),
        validatePosition: jest.fn(),
        save: jest.fn(),
        ...overrides
    };
}

describe('InlineCompletionProvider', () => {
    let provider: InlineCompletionProvider;
    let cache: CompletionCache;
    let mockDoc: any;
    let mockPos: any;
    let mockContext: any;
    let mockToken: any;
    let baseUrl: string;

    beforeEach(() => {
        baseUrl = 'http://localhost:5235';
        cache = new CompletionCache(10, 30_000);
        provider = new InlineCompletionProvider(() => baseUrl, cache);
        mockFetch.mockReset();

        mockDoc = createMockDocument();

        mockPos = { line: 10, character: 4 };

        mockContext = {
            triggerKind: 1, // Explicit
        };

        mockToken = {
            isCancellationRequested: false,
            onCancellationRequested: jest.fn(),
        };
    });

    it('InlineCompletionProvider_BlockedPath_ShouldReturnUndefined', async () => {
        const blockedDoc = createMockDocument({
            uri: { fsPath: '/workspace/node_modules/pkg/index.js' },
            fileName: '/workspace/node_modules/pkg/index.js',
            languageId: 'javascript',
        });
        const result = await provider.provideInlineCompletionItems(
            blockedDoc as any, mockPos, mockContext, mockToken
        );
        expect(result).toBeUndefined();
    });

    it('InlineCompletionProvider_LargeFile_ShouldReturnUndefined', async () => {
        const largeDoc = createMockDocument({
            getText: jest.fn().mockReturnValue('x'.repeat(200_000))
        });
        const result = await provider.provideInlineCompletionItems(
            largeDoc as any, mockPos, mockContext, mockToken
        );
        expect(result).toBeUndefined();
    });

    it('InlineCompletionProvider_CacheHit_ShouldReturnCachedWithoutFetch', async () => {
        cache.set('function hello() {\n  ', 'typescript', ['return "world";']);

        const result = await provider.provideInlineCompletionItems(
            mockDoc as any, mockPos, mockContext, mockToken
        );

        expect(result).toBeDefined();
        expect(result!.length).toBe(1);
        expect(mockFetch).not.toHaveBeenCalled();
    });

    it('InlineCompletionProvider_CacheMiss_ShouldFetchFromBackend', async () => {
        mockFetch.mockResolvedValueOnce({
            ok: true,
            json: async () => ({ completions: ['return "hello";', 'console.log("hi");'] }),
        });

        const result = await provider.provideInlineCompletionItems(
            mockDoc as any, mockPos, mockContext, mockToken
        );

        expect(mockFetch).toHaveBeenCalledTimes(1);
        expect(mockFetch).toHaveBeenCalledWith(
            'http://localhost:5235/api/coding/complete',
            expect.objectContaining({
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
            })
        );
        expect(result).toBeDefined();
        expect(result!.length).toBe(2);
    });

    it('InlineCompletionProvider_BackendError_ShouldReturnUndefined', async () => {
        mockFetch.mockResolvedValueOnce({ ok: false });

        const result = await provider.provideInlineCompletionItems(
            mockDoc as any, mockPos, mockContext, mockToken
        );
        expect(result).toBeUndefined();
    });

    it('InlineCompletionProvider_NetworkError_ShouldReturnUndefined', async () => {
        mockFetch.mockRejectedValueOnce(new Error('Network error'));

        const result = await provider.provideInlineCompletionItems(
            mockDoc as any, mockPos, mockContext, mockToken
        );
        expect(result).toBeUndefined();
    });

    it('InlineCompletionProvider_CacheStoresAfterFetch', async () => {
        mockFetch.mockResolvedValueOnce({
            ok: true,
            json: async () => ({ completions: ['return 42;'] }),
        });

        await provider.provideInlineCompletionItems(
            mockDoc as any, mockPos, mockContext, mockToken
        );

        const cached = cache.get('function hello() {\n  ', 'typescript');
        expect(cached).toEqual(['return 42;']);
    });

    it('InlineCompletionProvider_ClearCache_ShouldWork', () => {
        cache.set('test', 'ts', ['a']);
        expect(cache.size).toBe(1);
        provider.clearCache();
        expect(cache.size).toBe(0);
    });
});
