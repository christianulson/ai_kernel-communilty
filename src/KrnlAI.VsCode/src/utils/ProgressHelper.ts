import * as vscode from 'vscode';

export async function withProgress<T>(
    title: string,
    task: (
        progress: vscode.Progress<{ message?: string; increment?: number }>,
        token: vscode.CancellationToken
    ) => Promise<T>,
    location: vscode.ProgressLocation = vscode.ProgressLocation.Notification,
    cancellable = true
): Promise<T> {
    return vscode.window.withProgress({
        location,
        title,
        cancellable
    }, task);
}

export async function withLongRunningOperation<T>(
    title: string,
    task: () => Promise<T>,
    options?: {
        location?: vscode.ProgressLocation;
        cancellable?: boolean;
        steps?: string[];
    }
): Promise<T> {
    const steps = options?.steps ?? ['Preparando...', 'Processando...', 'Finalizando...'];
    const location = options?.location ?? vscode.ProgressLocation.Notification;
    const cancellable = options?.cancellable ?? true;

    return vscode.window.withProgress({
        location,
        title,
        cancellable
    }, async (progress, token) => {
        token.onCancellationRequested(() => {
            vscode.window.showWarningMessage(`Operação cancelada: ${title}`);
        });

        for (let i = 0; i < steps.length; i++) {
            if (token.isCancellationRequested) break;
            progress.report({
                message: steps[i],
                increment: Math.round(100 / steps.length)
            });
            await new Promise(r => setTimeout(r, 100));
        }

        return task();
    });
}

export function createStatusBarProgress(
    text: string,
    tooltip?: string
): vscode.StatusBarItem {
    const item = vscode.window.createStatusBarItem(
        vscode.StatusBarAlignment.Left,
        100
    );
    item.text = text;
    item.tooltip = tooltip || text;
    item.show();
    return item;
}
