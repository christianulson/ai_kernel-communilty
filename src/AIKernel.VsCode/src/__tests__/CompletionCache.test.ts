import { CompletionCache } from '../codingAgent/CompletionCache';

describe('CompletionCache', () => {
    let cache: CompletionCache;

    beforeEach(() => {
        cache = new CompletionCache(10, 30_000);
    });

    it('CompletionCache_SetAndGet_ShouldReturnCachedValue', () => {
        cache.set('function hello() {', 'typescript', ['  return "world";', '  console.log("hello");']);
        const result = cache.get('function hello() {', 'typescript');
        expect(result).toEqual(['  return "world";', '  console.log("hello");']);
    });

    it('CompletionCache_MissingKey_ShouldReturnNull', () => {
        const result = cache.get('nonexistent', 'typescript');
        expect(result).toBeNull();
    });

    it('CompletionCache_DifferentLanguage_ShouldReturnNull', () => {
        cache.set('console.log', 'typescript', ['  .toString()']);
        const result = cache.get('console.log', 'python');
        expect(result).toBeNull();
    });

    it('CompletionCache_ExpiredEntry_ShouldReturnNull', () => {
        const fastCache = new CompletionCache(10, -1);
        fastCache.set('test prefix', 'text', ['completion']);
        const result = fastCache.get('test prefix', 'text');
        expect(result).toBeNull();
    });

    it('CompletionCache_Size_ShouldTrackEntries', () => {
        expect(cache.size).toBe(0);
        cache.set('alpha long prefix', 'ts', ['1']);
        expect(cache.size).toBe(1);
        cache.set('beta long prefix', 'ts', ['2']);
        expect(cache.size).toBe(2);
    });

    it('CompletionCache_MaxEntries_ShouldEvictOldest', () => {
        const smallCache = new CompletionCache(2, 30_000);
        smallCache.set('a long enough prefix', 'ts', ['1']);
        smallCache.set('b long enough prefix', 'ts', ['2']);
        smallCache.set('c long enough prefix', 'ts', ['3']);
        expect(smallCache.size).toBe(2);
        const a = smallCache.get('a long enough prefix', 'ts');
        expect(a).toBeNull();
    });

    it('CompletionCache_Clear_ShouldRemoveAll', () => {
        cache.set('a long prefix', 'ts', ['1']);
        cache.set('b long prefix', 'ts', ['2']);
        cache.clear();
        expect(cache.size).toBe(0);
    });

    it('CompletionCache_InvalidateByLanguage_ShouldRemoveMatching', () => {
        cache.set('function hello() {', 'ts', ['return 1;']);
        cache.set('def hello():', 'py', ['return 2']);
        cache.invalidate(undefined, 'ts');
        expect(cache.get('function hello() {', 'ts')).toBeNull();
        expect(cache.get('def hello():', 'py')).toEqual(['return 2']);
    });

    it('CompletionCache_InvalidateAll_ShouldClearCache', () => {
        cache.set('prefix one', 'ts', ['1']);
        cache.set('prefix two', 'py', ['2']);
        cache.invalidate();
        expect(cache.size).toBe(0);
    });

    it('CompletionCache_PrefixStartsWith_ShouldReturnCached', () => {
        const longPrefix = 'function calculateTotal(items: number[]): number {';
        cache.set(longPrefix, 'typescript', ['  return items.reduce((sum, i) => sum + i, 0);']);
        const result = cache.get(longPrefix, 'typescript');
        expect(result).toBeDefined();
        expect(result!.length).toBe(1);
    });

    it('CompletionCache_ShortPrefix_ShouldStillWork', () => {
        cache.set('ab', 'ts', ['result']);
        const result = cache.get('ab', 'ts');
        expect(result).toEqual(['result']);
    });

    it('CompletionCache_InvalidateByPrefix_ShouldRemoveMatching', () => {
        cache.set('function hello() {', 'ts', ['return 1;']);
        cache.set('function world() {', 'ts', ['return 2;']);
        cache.invalidate('function hello');
        expect(cache.get('function hello() {', 'ts')).toBeNull();
        expect(cache.get('function world() {', 'ts')).toEqual(['return 2;']);
    });
});
