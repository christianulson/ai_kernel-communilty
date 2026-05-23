import * as vscode from 'vscode';

export class SidebarPage {
    async clickKanban(): Promise<void> {
        await vscode.commands.executeCommand('krnlai.kanban');
        await sleep(500);
    }

    async clickChat(): Promise<void> {
        await vscode.commands.executeCommand('krnlai.chat');
        await sleep(500);
    }

    async clickDashboard(): Promise<void> {
        await vscode.commands.executeCommand('krnlai.dashboard');
        await sleep(500);
    }

    async clickPolicies(): Promise<void> {
        await vscode.commands.executeCommand('krnlai.policies');
        await sleep(500);
    }

    async clickStartSidecar(): Promise<void> {
        await vscode.commands.executeCommand('krnlai.start');
        await sleep(2000);
    }

    async clickStopSidecar(): Promise<void> {
        await vscode.commands.executeCommand('krnlai.stop');
        await sleep(1000);
    }
}

function sleep(ms: number): Promise<void> {
    return new Promise(resolve => setTimeout(resolve, ms));
}
