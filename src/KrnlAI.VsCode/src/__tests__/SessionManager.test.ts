jest.mock('vscode', () => ({
    ExtensionContext: class {},
    Memento: class {
        private _data: any = {};
        get(key: string, defaultVal?: any) { return this._data[key] ?? defaultVal ?? []; }
        update(key: string, value: any) { this._data[key] = value; return Promise.resolve(); }
    },
}), { virtual: true });

import { SessionManager, ChatSession } from '../services/SessionManager';

function createMockMessage(role: 'user' | 'assistant' | 'system', content: string, idx: number) {
    return {
        id: `msg_${idx}`,
        role,
        content,
        timestamp: Date.now() + idx,
    };
}

describe('SessionManager', () => {
    let manager: SessionManager;
    let mockContext: any;

    beforeEach(() => {
        jest.clearAllMocks();
        const { Memento } = require('vscode');
        mockContext = { globalState: new Memento() };
        manager = new SessionManager(mockContext);
    });

    it('SessionManager_SaveAndList_ShouldReturnSession', async () => {
        const msgs = [createMockMessage('user', 'hello', 1)];
        await manager.saveSession('test', msgs);
        const sessions = await manager.listSessions();
        expect(sessions.length).toBe(1);
        expect(sessions[0].label).toBe('test');
        expect(sessions[0].messageCount).toBe(1);
    });

    it('SessionManager_LoadSession_ShouldReturnMessages', async () => {
        const msgs = [createMockMessage('user', 'hello', 1)];
        const saved = await manager.saveSession('test', msgs);
        const loaded = await manager.loadSession(saved.id);
        expect(loaded).not.toBeNull();
        expect(loaded!.messages.length).toBe(1);
        expect(loaded!.messages[0].content).toBe('hello');
    });

    it('SessionManager_LoadNonexistent_ShouldReturnNull', async () => {
        const result = await manager.loadSession('nonexistent');
        expect(result).toBeNull();
    });

    it('SessionManager_DeleteSession_ShouldRemove', async () => {
        const saved = await manager.saveSession('test', []);
        expect((await manager.listSessions()).length).toBe(1);
        await manager.deleteSession(saved.id);
        expect((await manager.listSessions()).length).toBe(0);
    });

    it('SessionManager_DeleteNonexistent_ShouldReturnFalse', async () => {
        const result = await manager.deleteSession('nonexistent');
        expect(result).toBe(false);
    });

    it('SessionManager_MaxSessions_ShouldTrim', async () => {
        const smallManager = new (SessionManager as any)(mockContext);
        // override getter: make it go through the normal path
        for (let i = 0; i < 15; i++) {
            await manager.saveSession(`session ${i}`, [createMockMessage('user', `msg ${i}`, i)]);
        }
        const sessions = await manager.listSessions();
        expect(sessions.length).toBeLessThanOrEqual(10);
    });

    it('SessionManager_ExportSession_ShouldReturnJSON', async () => {
        const msgs = [createMockMessage('user', 'hello', 1)];
        const saved = await manager.saveSession('test', msgs);
        const json = await manager.exportSession(saved.id);
        expect(json).not.toBeNull();
        expect(json).toContain('hello');
        expect(json).toContain('test');
    });

    it('SessionManager_ExportNonexistent_ShouldReturnNull', async () => {
        const result = await manager.exportSession('nonexistent');
        expect(result).toBeNull();
    });

    it('SessionManager_ImportSession_ShouldRestore', async () => {
        const json = JSON.stringify({
            id: 'imported',
            label: 'imported session',
            createdAt: Date.now(),
            updatedAt: Date.now(),
            messageCount: 1,
            messages: [createMockMessage('user', 'imported msg', 1)]
        });
        const result = await manager.importSession(json);
        expect(result).not.toBeNull();
        expect(result!.label).toBe('imported session');
        expect((await manager.listSessions()).length).toBe(1);
    });

    it('SessionManager_ImportInvalidJSON_ShouldReturnNull', async () => {
        const result = await manager.importSession('{invalid}');
        expect(result).toBeNull();
    });

    it('SessionManager_AutoSave_ShouldCreateNewSession', async () => {
        const id = await manager.autoSave('auto', [createMockMessage('user', 'auto', 1)]);
        expect(id).toBeTruthy();
        const sessions = await manager.listSessions();
        expect(sessions.length).toBe(1);
    });

    it('SessionManager_AutoSave_WithExistingId_ShouldUpdate', async () => {
        const id = await manager.autoSave('existing', [createMockMessage('user', 'first', 1)]);
        const id2 = await manager.autoSave('existing', [createMockMessage('user', 'first', 1), createMockMessage('assistant', 'response', 2)], id);
        expect(id2).toBe(id);
        const loaded = await manager.loadSession(id);
        expect(loaded!.messageCount).toBe(2);
    });
});
