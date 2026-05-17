import * as vscode from 'vscode';
import type { ChatMessage } from '../chat/ChatMessage';

const STORAGE_KEY = 'aikernel.sessions';
const MAX_SESSIONS = 10;

export interface ChatSession {
    id: string;
    label: string;
    createdAt: number;
    updatedAt: number;
    messageCount: number;
    messages: ChatMessage[];
}

export class SessionManager {
    private _globalState: vscode.Memento;

    constructor(context: vscode.ExtensionContext) {
        this._globalState = context.globalState;
    }

    private _getAllSessions(): ChatSession[] {
        return this._globalState.get<ChatSession[]>(STORAGE_KEY, []);
    }

    private async _saveAllSessions(sessions: ChatSession[]): Promise<void> {
        const trimmed = sessions
            .sort((a, b) => b.updatedAt - a.updatedAt)
            .slice(0, MAX_SESSIONS);
        await this._globalState.update(STORAGE_KEY, trimmed);
    }

    async saveSession(label: string, messages: ChatMessage[]): Promise<ChatSession> {
        const sessions = this._getAllSessions();

        const session: ChatSession = {
            id: `session_${Date.now()}_${Math.random().toString(36).substring(2, 6)}`,
            label,
            createdAt: Date.now(),
            updatedAt: Date.now(),
            messageCount: messages.length,
            messages: messages.slice()
        };

        sessions.push(session);
        await this._saveAllSessions(sessions);
        return session;
    }

    async updateSession(sessionId: string, messages: ChatMessage[]): Promise<boolean> {
        const sessions = this._getAllSessions();
        const idx = sessions.findIndex(s => s.id === sessionId);
        if (idx === -1) return false;

        sessions[idx].messages = messages.slice();
        sessions[idx].messageCount = messages.length;
        sessions[idx].updatedAt = Date.now();
        await this._saveAllSessions(sessions);
        return true;
    }

    async loadSession(sessionId: string): Promise<ChatSession | null> {
        const sessions = this._getAllSessions();
        return sessions.find(s => s.id === sessionId) || null;
    }

    async deleteSession(sessionId: string): Promise<boolean> {
        const sessions = this._getAllSessions();
        const filtered = sessions.filter(s => s.id !== sessionId);
        if (filtered.length === sessions.length) return false;
        await this._saveAllSessions(filtered);
        return true;
    }

    async listSessions(): Promise<ChatSession[]> {
        return this._getAllSessions()
            .sort((a, b) => b.updatedAt - a.updatedAt);
    }

    async exportSession(sessionId: string): Promise<string | null> {
        const session = await this.loadSession(sessionId);
        if (!session) return null;
        return JSON.stringify(session, null, 2);
    }

    async importSession(json: string): Promise<ChatSession | null> {
        try {
            const session = JSON.parse(json) as ChatSession;
            if (!session.id || !Array.isArray(session.messages)) return null;

            session.id = `session_${Date.now()}_${Math.random().toString(36).substring(2, 6)}`;
            session.createdAt = Date.now();
            session.updatedAt = Date.now();

            const sessions = this._getAllSessions();
            sessions.push(session);
            await this._saveAllSessions(sessions);
            return session;
        } catch {
            return null;
        }
    }

    async autoSave(label: string, messages: ChatMessage[], currentSessionId?: string): Promise<string> {
        if (currentSessionId) {
            await this.updateSession(currentSessionId, messages);
            return currentSessionId;
        }
        const session = await this.saveSession(label, messages);
        return session.id;
    }
}
