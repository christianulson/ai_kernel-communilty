import * as assert from 'assert';
import * as vscode from 'vscode';
import { activateExtension, waitForExtension } from '../fixtures/setup';
import { SidebarPage } from '../pages/SidebarPage';

suite('Navigation', () => {
    let sidebar: SidebarPage;

    suiteSetup(async () => {
        await activateExtension();
        await waitForExtension();
        sidebar = new SidebarPage();
    });

    test('NavTree_ShouldShowSections', async () => {
        const commands = await vscode.commands.getCommands(true);
        const panelCommands = [
            'krnlai.chat',
            'krnlai.dashboard',
            'krnlai.policies',
            'krnlai.episodes',
            'krnlai.memory',
            'krnlai.settings',
        ];
        for (const cmd of panelCommands) {
            assert.ok(commands.includes(cmd),
                `Command ${cmd} should be registered`);
        }
    });

    test('Navigate_Chat_ShouldBeCallable', async () => {
        await sidebar.clickChat();
        // Command should not throw
        assert.ok(true);
    });

    test('Navigate_Dashboard_ShouldBeCallable', async () => {
        await sidebar.clickDashboard();
        assert.ok(true);
    });

    test('Navigate_Policies_ShouldBeCallable', async () => {
        await sidebar.clickPolicies();
        assert.ok(true);
    });
});
