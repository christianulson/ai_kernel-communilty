import * as assert from 'assert';
import { activateExtension, waitForExtension } from '../fixtures/setup';
import { SidebarPage } from '../pages/SidebarPage';

suite('Sidecar', () => {
    let sidebar: SidebarPage;

    suiteSetup(async () => {
        await activateExtension();
        await waitForExtension();
        sidebar = new SidebarPage();
    });

    test('Extension_Activate_ShouldRegisterCommands', async () => {
        const commands = await import('vscode').then(vs =>
            vs.commands.getCommands(true));
        const krnlaiCommands = commands.filter((c: string) =>
            c.startsWith('krnlai.'));
        assert.ok(krnlaiCommands.length >= 8,
            `Expected 8+ commands, got ${krnlaiCommands.length}: ${krnlaiCommands.join(', ')}`);
    });

    test('Sidecar_StartStop_ShouldWork', async () => {
        await sidebar.clickStartSidecar();
        // After starting, the stop command should be available
        const commands = await import('vscode').then(vs =>
            vs.commands.getCommands(true));
        assert.ok(commands.includes('krnlai.stop'));
    });
});
