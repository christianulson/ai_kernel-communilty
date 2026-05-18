import * as vscode from 'vscode';

export class CodeActionProvider implements vscode.CodeActionProvider {
    provideCodeActions(
        document: vscode.TextDocument,
        range: vscode.Range,
        context: vscode.CodeActionContext,
        token: vscode.CancellationToken
    ): vscode.CodeAction[] {
        const actions: vscode.CodeAction[] = [];

        if (token.isCancellationRequested) return [];

        if (context.diagnostics.length > 0) {
            const fix = new vscode.CodeAction(
                'Fix with Krnl-AI',
                vscode.CodeActionKind.QuickFix
            );
            fix.command = {
                command: 'krnlai.coding.fix',
                title: 'Fix with Krnl-AI',
                arguments: [document.uri.fsPath, context.diagnostics.map(d => d.message)]
            };
            actions.push(fix);
        }

        const hasSelection = !range.isEmpty;
        if (hasSelection) {
            const explain = new vscode.CodeAction(
                'Explain with Krnl-AI',
                vscode.CodeActionKind.Refactor
            );
            explain.command = {
                command: 'krnlai.coding.explain',
                title: 'Explain with Krnl-AI',
                arguments: [document.getText(range)]
            };
            actions.push(explain);
        }

        const text = document.getText();
        if (/class\s+\w+|function\s+\w+/.test(text)) {
            const test = new vscode.CodeAction(
                'Generate Test with Krnl-AI',
                vscode.CodeActionKind.Refactor
            );
            test.command = {
                command: 'krnlai.coding.test',
                title: 'Generate Test with Krnl-AI',
                arguments: [document.uri.fsPath]
            };
            actions.push(test);
        }

        return actions;
    }
}
