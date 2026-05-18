import * as vscode from 'vscode';
import { CompletionCache } from './CompletionCache';

const MAX_CONTEXT_LINES = 80;
const MAX_FILE_SIZE = 100_000;
const TRIGGER_CHARS = new Set(['.', ' ', '\n', '\t', '(', ')', '{', '}', '[', ']', ';', ':', '=', '+', '-', '*', '/', '>', '<', '!', '~', '&', '|', '%', ',']);
const BLOCKED_PATHS = ['node_modules', '.git', 'bin', 'obj', 'dist', 'build', '.next', '.nuxt', 'venv', '.venv', '__pycache__'];

export class InlineCompletionProvider implements vscode.InlineCompletionItemProvider {
    private readonly _cache: CompletionCache;
    private _pendingRequest: AbortController | null = null;
    private _lastContextHash = '';

    constructor(
        private readonly _getBaseUrl: () => string,
        cache?: CompletionCache
    ) {
        this._cache = cache || new CompletionCache();
    }

    private _isBlockedPath(filePath: string): boolean {
        const normalized = filePath.replace(/\\/g, '/');
        return BLOCKED_PATHS.some(p => normalized.includes(`/${p}/`) || normalized.startsWith(`${p}/`));
    }

    private _getContextHash(document: vscode.TextDocument, position: vscode.Position): string {
        const startLine = Math.max(0, position.line - 5);
        const contextRange = new vscode.Range(startLine, 0, position.line, position.character);
        const text = document.getText(contextRange);
        const lang = document.languageId;
        return `${lang}:${text.length}:${text.substring(Math.max(0, text.length - 80))}`;
    }

    async provideInlineCompletionItems(
        document: vscode.TextDocument,
        position: vscode.Position,
        context: vscode.InlineCompletionContext,
        token: vscode.CancellationToken
    ): Promise<vscode.InlineCompletionItem[] | undefined> {
        const filePath = document.uri.fsPath;

        if (this._isBlockedPath(filePath)) return undefined;
        if (document.getText().length > MAX_FILE_SIZE) return undefined;

        const contextHash = this._getContextHash(document, position);
        if (contextHash === this._lastContextHash && context.triggerKind === vscode.InlineCompletionTriggerKind.Automatic) {
            this._lastContextHash = contextHash;
            return undefined;
        }
        this._lastContextHash = contextHash;

        const startLine = Math.max(0, position.line - MAX_CONTEXT_LINES);
        const prefix = document.getText(new vscode.Range(startLine, 0, position.line, position.character));
        const language = document.languageId;

        const cached = this._cache.get(prefix, language);
        if (cached) {
            return cached.map(text => new vscode.InlineCompletionItem(text));
        }

        if (this._pendingRequest) {
            this._pendingRequest.abort();
            this._pendingRequest = null;
        }

        const abortController = new AbortController();
        this._pendingRequest = abortController;

        token.onCancellationRequested(() => {
            abortController.abort();
            this._pendingRequest = null;
        });

        try {
            const response = await this._fetchCompletions(prefix, language, filePath, abortController.signal);

            if (token.isCancellationRequested) return undefined;

            if (response?.completions?.length) {
                this._cache.set(prefix, language, response.completions);
                return response.completions.map(text => new vscode.InlineCompletionItem(text));
            }

            return undefined;
        } catch {
            return undefined;
        } finally {
            if (this._pendingRequest === abortController) {
                this._pendingRequest = null;
            }
        }
    }

    private async _fetchCompletions(
        prefix: string,
        language: string,
        filePath: string,
        signal: AbortSignal
    ): Promise<{ completions: string[] } | null> {
        const url = `${this._getBaseUrl()}/api/coding/complete`;

        const response = await fetch(url, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                codeContext: prefix.substring(Math.max(0, prefix.length - 2000)),
                language,
                filePath
            }),
            signal
        });

        if (!response.ok) return null;

        return await response.json();
    }

    clearCache(): void {
        this._cache.clear();
    }
}
