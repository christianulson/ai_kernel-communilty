jest.mock('vscode', () => ({
    ExtensionContext: class {},
    Memento: class {
        private _data: any = {};
        get(key: string, defaultVal?: any) { return this._data[key] ?? defaultVal ?? []; }
        update(key: string, value: any) { this._data[key] = value; return Promise.resolve(); }
    },
}), { virtual: true });

import { UsageTracker } from '../services/UsageTracker';

describe('UsageTracker', () => {
    let tracker: UsageTracker;
    let mockContext: any;

    beforeEach(() => {
        jest.clearAllMocks();
        const { Memento } = require('vscode');
        mockContext = { globalState: new Memento() };
        tracker = new UsageTracker(mockContext);
    });

    it('UsageTracker_Empty_ShouldReturnZeroStats', () => {
        const stats = tracker.getStats();
        expect(stats.totalActions).toBe(0);
        expect(stats.totalTokens).toBe(0);
        expect(stats.totalCost).toBe(0);
    });

    it('UsageTracker_TrackCommand_ShouldRecordAction', async () => {
        await tracker.trackCommand('/explain');
        const stats = tracker.getStats();
        expect(stats.totalActions).toBe(1);
        expect(stats.commandCounts['/explain']).toBe(1);
    });

    it('UsageTracker_TrackTokens_ShouldCalculateCost', async () => {
        await tracker.trackTokens('/explain', 1000, 500);
        const stats = tracker.getStats();
        expect(stats.totalInput).toBe(1000);
        expect(stats.totalOutput).toBe(500);
        expect(stats.totalCost).toBeGreaterThan(0);
    });

    it('UsageTracker_MultipleCommands_ShouldAggregate', async () => {
        await tracker.trackCommand('/explain');
        await tracker.trackCommand('/fix');
        await tracker.trackCommand('/explain');
        const stats = tracker.getStats();
        expect(stats.totalActions).toBe(3);
        expect(stats.commandCounts['/explain']).toBe(2);
        expect(stats.commandCounts['/fix']).toBe(1);
        expect(stats.topActions[0].action).toBe('/explain');
        expect(stats.topActions[0].count).toBe(2);
    });

    it('UsageTracker_ExportAll_ShouldReturnJSON', async () => {
        await tracker.trackCommand('/explain');
        const json = await tracker.exportAll();
        expect(json).toContain('/explain');
        expect(json).toContain('exportedAt');
        expect(json).toContain('stats');
    });

    it('UsageTracker_FormatStats_ShouldReturnReadable', async () => {
        await tracker.trackCommand('/explain');
        await tracker.trackTokens('/fix', 2000, 1000);
        const formatted = tracker.formatStats();
        expect(formatted).toContain('Usage Statistics');
        expect(formatted).toContain('/explain');
        expect(formatted).toContain('/fix');
    });
});
