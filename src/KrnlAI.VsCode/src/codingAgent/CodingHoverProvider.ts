import * as vscode from 'vscode';

interface HoverCacheEntry {
    content: string;
    timestamp: number;
    symbol: string;
}

export class CodingHoverProvider implements vscode.HoverProvider {
    private _cache = new Map<string, HoverCacheEntry>();
    private readonly _cacheTtlMs = 5 * 60 * 1000;
    private readonly _maxCacheSize = 100;
    private readonly _getBaseUrl: () => string;

    constructor(getBaseUrl: () => string) {
        this._getBaseUrl = getBaseUrl;
    }

    async provideHover(
        document: vscode.TextDocument,
        position: vscode.Position,
        token: vscode.CancellationToken
    ): Promise<vscode.Hover | undefined> {
        const range = document.getWordRangeAtPosition(position);
        if (!range) return undefined;

        const symbol = document.getText(range);
        if (!symbol || symbol.length > 80 || /^[\d\s]+$/.test(symbol)) return undefined;

        const filePath = document.uri.fsPath;
        if (this._isBlocked(filePath)) return undefined;

        if (document.getText().length > 100_000) return undefined;

        const cacheKey = `${document.languageId}:${symbol}`;
        const cached = this._checkCache(cacheKey, symbol);
        if (cached) return cached;

        if (token.isCancellationRequested) return undefined;

        try {
            const line = document.lineAt(position.line);
            const lineStart = Math.max(0, position.line - 3);
            const contextRange = new vscode.Range(lineStart, 0, position.line + 1, 0);
            const contextCode = document.getText(contextRange);

            const response = await fetch(`${this._getBaseUrl()}/agent/run`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({
                    prompt: `Explain this ${document.languageId} symbol briefly (1-2 sentences):\n\nContext:\`\`\`${document.languageId}\n${contextCode}\n\`\`\`\n\nSymbol: ${symbol}`,
                    mode: 'gateway'
                }),
                signal: AbortSignal.timeout(5000)
            });

            if (!response.ok) return undefined;

            const data = await response.json();
            const text = data?.narration || data?.error;
            if (!text) return undefined;

            const hoverContent = new vscode.MarkdownString();
            hoverContent.appendMarkdown(`**${symbol}**\n\n${text}`);
            hoverContent.isTrusted = true;

            const hover = new vscode.Hover(hoverContent, range);
            this._setCache(cacheKey, text, symbol);

            return hover;
        } catch {
            return undefined;
        }
    }

    private _isBlocked(filePath: string): boolean {
        const normalized = filePath.replace(/\\/g, '/');
        const blocked = ['node_modules', '.git', 'bin', 'obj', 'dist', 'build', '.next', 'venv', '__pycache__'];
        return blocked.some(p => normalized.includes(`/${p}/`));
    }

    private _checkCache(key: string, symbol: string): vscode.Hover | undefined {
        const entry = this._cache.get(key);
        if (!entry) return undefined;
        if (Date.now() - entry.timestamp > this._cacheTtlMs) {
            this._cache.delete(key);
            return undefined;
        }
        if (entry.symbol !== symbol) return undefined;
        const content = new vscode.MarkdownString();
        content.appendMarkdown(`**${symbol}**\n\n${entry.content}`);
        content.isTrusted = true;
        return new vscode.Hover(content);
    }

    private _setCache(key: string, content: string, symbol: string): void {
        if (this._cache.size >= this._maxCacheSize) {
            const firstKey = this._cache.keys().next().value;
            if (firstKey) this._cache.delete(firstKey);
        }
        this._cache.set(key, { content, timestamp: Date.now(), symbol });
    }

    clearCache(): void {
        this._cache.clear();
    }
}
