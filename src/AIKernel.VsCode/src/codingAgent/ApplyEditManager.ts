import * as vscode from 'vscode';

export interface FileChange {
    filePath: string;
    originalContent: string;
    newContent: string;
    label: string;
}

export interface ApprovalGate {
    requestApproval(action: string, details: string[]): Promise<'allowed' | 'rejected'>;
}

export class ApplyEditManager {
    async applyChange(
        change: FileChange,
        approvalGate?: ApprovalGate
    ): Promise<boolean> {
        const approved = await this._requestApproval(change, approvalGate);
        if (!approved) {
            vscode.window.showInformationMessage(`Alteração rejeitada: ${change.label}`);
            return false;
        }

        const uri = vscode.Uri.file(change.filePath);
        return this._applyEdit(uri, change.originalContent, change.newContent);
    }

    async applyMultiFile(
        changes: FileChange[],
        label: string,
        approvalGate?: ApprovalGate
    ): Promise<boolean> {
        if (changes.length === 0) return true;

        const details = changes.map(c => `${c.filePath} (${c.label})`);
        if (approvalGate) {
            const decision = await approvalGate.requestApproval(
                `Aplicar ${changes.length} alteração(ões): ${label}`,
                details
            );
            if (decision === 'rejected') return false;
        }

        let allApplied = true;
        for (const change of changes) {
            const uri = vscode.Uri.file(change.filePath);
            const applied = await this._applyEdit(uri, change.originalContent, change.newContent);
            if (!applied) allApplied = false;
        }

        return allApplied;
    }

    private async _requestApproval(
        change: FileChange,
        approvalGate?: ApprovalGate
    ): Promise<boolean> {
        if (!approvalGate) return true;

        const decision = await approvalGate.requestApproval(
            `Modificar: ${change.label}`,
            [
                `Arquivo: ${change.filePath}`,
                `Tipo: ${change.label}`,
                `Linhas: ~${change.originalContent.split('\n').length} → ~${change.newContent.split('\n').length}`
            ]
        );
        return decision !== 'rejected';
    }

    private async _applyEdit(
        uri: vscode.Uri,
        originalContent: string,
        newContent: string
    ): Promise<boolean> {
        try {
            const document = await vscode.workspace.openTextDocument(uri);

            const edit = new vscode.WorkspaceEdit();

            const fullRange = new vscode.Range(
                document.positionAt(0),
                document.positionAt(document.getText().length)
            );

            edit.replace(uri, fullRange, newContent);

            const applied = await vscode.workspace.applyEdit(edit);
            if (!applied) {
                vscode.window.showErrorMessage(`Falha ao aplicar edição em ${uri.fsPath}`);
                return false;
            }

            return true;
        } catch (err: any) {
            vscode.window.showErrorMessage(`Erro ao aplicar edição: ${err.message}`);
            return false;
        }
    }

    async showDiff(
        filePath: string,
        originalContent: string,
        newContent: string,
        title: string
    ): Promise<void> {
        const uri = vscode.Uri.file(filePath);

        const originalDoc = await vscode.workspace.openTextDocument({ content: originalContent });
        const newDoc = await vscode.workspace.openTextDocument({ content: newContent });

        await vscode.commands.executeCommand('vscode.diff',
            originalDoc.uri,
            newDoc.uri,
            `${title}: ${filePath.split(/[\\/]/).pop()}`
        );
    }

    async applyWithDiff(
        change: FileChange,
        approvalGate?: ApprovalGate
    ): Promise<boolean> {
        await this.showDiff(
            change.filePath,
            change.originalContent,
            change.newContent,
            change.label
        );

        return this.applyChange(change, approvalGate);
    }

    async applyMultiFileWithDiff(
        changes: FileChange[],
        label: string,
        approvalGate?: ApprovalGate
    ): Promise<boolean> {
        if (changes.length === 1) {
            return this.applyWithDiff(changes[0], approvalGate);
        }

        for (const change of changes) {
            await this.showDiff(
                change.filePath,
                change.originalContent,
                change.newContent,
                `${label} - ${change.label}`
            );
        }

        return this.applyMultiFile(changes, label, approvalGate);
    }
}
