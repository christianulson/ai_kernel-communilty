import * as vscode from 'vscode';

const STORAGE_KEY = 'aikernel.usage';

export interface UsageRecord {
    timestamp: number;
    action: string;
    provider?: string;
    model?: string;
    tokensInput?: number;
    tokensOutput?: number;
    durationMs?: number;
}

export interface UsageStats {
    totalTokens: number;
    totalInput: number;
    totalOutput: number;
    totalCost: number;
    totalActions: number;
    commandCounts: Record<string, number>;
    topActions: { action: string; count: number }[];
    byDate: Record<string, number>;
    sessionsCount: number;
}

export class UsageTracker {
    private _globalState: vscode.Memento;

    constructor(context: vscode.ExtensionContext) {
        this._globalState = context.globalState;
    }

    private _getRecords(): UsageRecord[] {
        return this._globalState.get<UsageRecord[]>(STORAGE_KEY, []);
    }

    private async _saveRecords(records: UsageRecord[]): Promise<void> {
        const trimmed = records.slice(-1000);
        await this._globalState.update(STORAGE_KEY, trimmed);
    }

    async track(record: UsageRecord): Promise<void> {
        const records = this._getRecords();
        records.push(record);
        await this._saveRecords(records);
    }

    async trackCommand(command: string, durationMs?: number): Promise<void> {
        await this.track({
            timestamp: Date.now(),
            action: command,
            durationMs,
        });
    }

    async trackTokens(command: string, input: number, output: number, provider?: string, model?: string): Promise<void> {
        await this.track({
            timestamp: Date.now(),
            action: command,
            provider,
            model,
            tokensInput: input,
            tokensOutput: output,
        });
    }

    getStats(): UsageStats {
        const records = this._getRecords();
        const commandCounts: Record<string, number> = {};
        const byDate: Record<string, number> = {};

        let totalInput = 0;
        let totalOutput = 0;
        let totalCost = 0;

        for (const r of records) {
            commandCounts[r.action] = (commandCounts[r.action] || 0) + 1;

            const date = new Date(r.timestamp).toISOString().substring(0, 10);
            byDate[date] = (byDate[date] || 0) + (r.tokensInput || 0) + (r.tokensOutput || 0);

            totalInput += r.tokensInput || 0;
            totalOutput += r.tokensOutput || 0;
        }

        const inputCost = (totalInput / 1_000_000) * 2.50;
        const outputCost = (totalOutput / 1_000_000) * 10.00;
        totalCost = inputCost + outputCost;

        const topActions = Object.entries(commandCounts)
            .sort(([, a], [, b]) => b - a)
            .slice(0, 10)
            .map(([action, count]) => ({ action, count }));

        const sessionCount = new Set(records.map(r => new Date(r.timestamp).toISOString().substring(0, 10))).size;

        return {
            totalTokens: totalInput + totalOutput,
            totalInput,
            totalOutput,
            totalCost,
            totalActions: records.length,
            commandCounts,
            topActions,
            byDate,
            sessionsCount: sessionCount,
        };
    }

    formatStats(): string {
        const s = this.getStats();
        return [
            `📊 **AI Kernel Usage Statistics**`,
            ``,
            `**General**`,
            `Total actions: ${s.totalActions}`,
            `Active days: ${s.sessionsCount}`,
            ``,
            `**Tokens**`,
            `Total: ${s.totalTokens.toLocaleString()}`,
            `Input: ${s.totalInput.toLocaleString()}`,
            `Output: ${s.totalOutput.toLocaleString()}`,
            ``,
            `**Cost (est.)**`,
            `$${s.totalCost.toFixed(4)}`,
            ``,
            `**Most Used Commands**`,
            ...s.topActions.map(a => `- ${a.action}: ${a.count}x`),
            ``,
            `**Activity by Date**`,
            ...Object.entries(s.byDate)
                .sort(([a], [b]) => b.localeCompare(a))
                .slice(0, 7)
                .map(([date, tokens]) => `- ${date}: ${tokens.toLocaleString()} tokens`),
        ].join('\n');
    }

    async exportAll(): Promise<string> {
        const records = this._getRecords();
        return JSON.stringify({
            exportedAt: new Date().toISOString(),
            version: '1.0',
            stats: this.getStats(),
            records: records.slice(-500),
        }, null, 2);
    }
}
