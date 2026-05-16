import * as vscode from 'vscode';

export interface EditorContext {
    activeFile?: string;
    language?: string;
    content?: string;
    selection?: string | null;
    visibleFiles: string[];
    diagnostics: { message: string; severity: string; source?: string }[];
}

export class EditorContextProvider {
    private _lastDiagTime = 0;
    private _cachedDiags: { message: string; severity: string; source?: string }[] = [];

    getActiveEditorContent(): string | undefined {
        return vscode.window.activeTextEditor?.document.getText();
    }

    getSelection(): string | null {
        const editor = vscode.window.activeTextEditor;
        if (!editor || editor.selection.isEmpty) return null;
        return editor.document.getText(editor.selection);
    }

    getVisibleFiles(): string[] {
        return vscode.window.visibleTextEditors.map(e => e.document.uri.fsPath);
    }

    getWorkspaceDiagnostics(): { message: string; severity: string; source?: string }[] {
        const now = Date.now();
        if (now - this._lastDiagTime < 2000) return this._cachedDiags;
        this._lastDiagTime = now;

        const activeUri = vscode.window.activeTextEditor?.document.uri;
        const result: { message: string; severity: string; source?: string }[] = [];

        for (const [uri, diags] of vscode.languages.getDiagnostics()) {
            if (activeUri && uri.toString() !== activeUri.toString()) continue;
            for (const d of diags) {
                result.push({
                    message: d.message,
                    severity: d.severity === vscode.DiagnosticSeverity.Error ? 'error'
                        : d.severity === vscode.DiagnosticSeverity.Warning ? 'warning' : 'info',
                    source: d.source
                });
            }
        }
        this._cachedDiags = result;
        return result;
    }

    invalidateDiagCache(): void {
        this._lastDiagTime = 0;
    }

    async resolveFileReference(ref: string): Promise<string | undefined> {
        const match = ref.match(/^@file:(.+)$/);
        if (!match) return undefined;
        const fileName = match[1].trim();

        if (/[<>|:;*?{}[\]!()&@#$%^=+~`'"]/.test(fileName)) return undefined;

        if (fileName.includes('..') || fileName.startsWith('/') || fileName.startsWith('\\')) return undefined;

        const files = await vscode.workspace.findFiles(`**/${fileName}`, undefined, 1);
        if (files.length === 0) return undefined;
        const doc = await vscode.workspace.openTextDocument(files[0]);
        return doc.getText();
    }

    getCurrentFilePath(): string | undefined {
        return vscode.window.activeTextEditor?.document.uri.fsPath;
    }

    getCurrentLanguage(): string | undefined {
        return vscode.window.activeTextEditor?.document.languageId;
    }

    async getFullContext(maxContentLength = 50000): Promise<EditorContext> {
        const content = this.getActiveEditorContent();
        return {
            activeFile: this.getCurrentFilePath(),
            language: this.getCurrentLanguage(),
            content: content ? content.substring(0, maxContentLength) : undefined,
            selection: this.getSelection(),
            visibleFiles: this.getVisibleFiles(),
            diagnostics: this.getWorkspaceDiagnostics().slice(0, 50),
        };
    }
}
