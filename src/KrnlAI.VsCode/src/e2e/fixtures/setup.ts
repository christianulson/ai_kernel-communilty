import * as vscode from 'vscode';

const EXTENSION_ID = 'krnlai-vscode';

export async function activateExtension(): Promise<void> {
    const ext = vscode.extensions.getExtension(EXTENSION_ID);
    if (!ext) throw new Error(`Extension ${EXTENSION_ID} not found`);
    await ext.activate();
}

export async function waitForExtension(timeoutMs = 10000): Promise<void> {
    const start = Date.now();
    while (Date.now() - start < timeoutMs) {
        const ext = vscode.extensions.getExtension(EXTENSION_ID);
        if (ext?.isActive) return;
        await sleep(200);
    }
    throw new Error('Extension activation timed out');
}

export function sleep(ms: number): Promise<void> {
    return new Promise(resolve => setTimeout(resolve, ms));
}

export async function getSidecarProcessStatus(): Promise<string | undefined> {
    const ext = vscode.extensions.getExtension(EXTENSION_ID);
    if (!ext?.exports?.sidecarStatus) return undefined;
    return ext.exports.sidecarStatus;
}
