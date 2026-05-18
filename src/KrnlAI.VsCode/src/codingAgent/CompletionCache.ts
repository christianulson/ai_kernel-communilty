export interface CacheEntry {
    completions: string[];
    timestamp: number;
    prefixStart: string;
    language: string;
}

export class CompletionCache {
    private _cache = new Map<string, CacheEntry>();
    private readonly _maxEntries: number;
    private readonly _ttlMs: number;

    constructor(maxEntries = 200, ttlMs = 30_000) {
        this._maxEntries = maxEntries;
        this._ttlMs = ttlMs;
    }

    private _makeKey(prefix: string, language: string): string {
        const normalized = prefix.replace(/\s+/g, ' ').trimEnd();
        const lang = language || 'unknown';
        const hash = this._simpleHash(normalized);
        return `${lang}:${hash}`;
    }

    private _simpleHash(s: string): string {
        let hash = 0;
        for (let i = 0; i < s.length; i++) {
            const char = s.charCodeAt(i);
            hash = ((hash << 5) - hash) + char;
            hash |= 0;
        }
        return hash.toString(36);
    }

    get(prefix: string, language: string): string[] | null {
        const key = this._makeKey(prefix, language);
        const entry = this._cache.get(key);

        if (!entry) return null;

        const age = Date.now() - entry.timestamp;
        if (age > this._ttlMs) {
            this._cache.delete(key);
            return null;
        }

        if (prefix.length >= 10 && entry.prefixStart.length >= 10 && prefix.startsWith(entry.prefixStart)) {
            return entry.completions;
        }

        if (entry.prefixStart === prefix) {
            return entry.completions;
        }

        return null;
    }

    set(prefix: string, language: string, completions: string[]): void {
        if (this._cache.size >= this._maxEntries) {
            const oldest = this._cache.entries().next();
            if (oldest.value) this._cache.delete(oldest.value[0]);
        }

        const key = this._makeKey(prefix, language);
        this._cache.set(key, {
            completions,
            timestamp: Date.now(),
            prefixStart: prefix.substring(0, Math.min(80, prefix.length)),
            language
        });
    }

    invalidate(prefix?: string, language?: string): void {
        if (!prefix && !language) {
            this._cache.clear();
            return;
        }

        if (language) {
            for (const [key, entry] of this._cache) {
                if (entry.language === language) {
                    if (!prefix || entry.prefixStart.includes(prefix)) {
                        this._cache.delete(key);
                    }
                }
            }
            return;
        }

        if (prefix) {
            for (const [key, entry] of this._cache) {
                if (entry.prefixStart.includes(prefix)) {
                    this._cache.delete(key);
                }
            }
        }
    }

    get size(): number {
        return this._cache.size;
    }

    clear(): void {
        this._cache.clear();
    }
}
