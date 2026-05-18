import * as vscode from 'vscode';

export class CodeLensProvider implements vscode.CodeLensProvider {
    private _debounce = 0;

    provideCodeLenses(document: vscode.TextDocument): vscode.CodeLens[] {
        const now = Date.now();
        if (now - this._debounce < 300) return [];
        this._debounce = now;

        const text = document.getText();
        if (text.length > 50000) return [];

        const lenses: vscode.CodeLens[] = [];
        const functionRegex = /(?:export\s+)?(?:async\s+)?function\s+(\w+)|(?:export\s+)?class\s+(\w+)|(?:export\s+)?const\s+(\w+)\s*=\s*(?:async\s*)?\(/g;
        let match: RegExpExecArray | null;

        while ((match = functionRegex.exec(text)) !== null) {
            const name = match[1] || match[2] || match[3];
            const startPos = document.positionAt(match.index);
            const endPos = document.positionAt(match.index + match[0].length);
            const range = new vscode.Range(startPos, endPos);

            lenses.push(new vscode.CodeLens(range, {
                title: '$(light-bulb) Explain',
                command: 'krnlai.coding.explain',
                arguments: [name]
            }));

            lenses.push(new vscode.CodeLens(range, {
                title: '$(beaker) Test',
                command: 'krnlai.coding.test',
                arguments: [name]
            }));

            lenses.push(new vscode.CodeLens(range, {
                title: '$(search) Review',
                command: 'krnlai.coding.review',
                arguments: [name]
            }));
        }

        return lenses;
    }
}
