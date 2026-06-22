const mockStartDebugging = jest.fn();
const mockStopDebugging = jest.fn();
const mockAddBreakpoints = jest.fn();
const mockRemoveBreakpoints = jest.fn();

jest.mock('vscode', () => ({
    debug: {
        startDebugging: mockStartDebugging,
        stopDebugging: mockStopDebugging,
        addBreakpoints: mockAddBreakpoints,
        removeBreakpoints: mockRemoveBreakpoints,
        onDidStartDebugSession: jest.fn(),
        onDidTerminateDebugSession: jest.fn(),
        get breakpoints() { return []; },
    },
    workspace: {
        workspaceFolders: [{ uri: { fsPath: '/test' } }],
    },
    SourceBreakpoint: class {
        constructor(public location: any) {}
    },
    Location: class {
        constructor(public uri: any, public range: any) {}
    },
    Position: class {
        constructor(public line: number, public character: number) {}
    },
    Range: class {
        constructor(public start: any, public end: any) {}
    },
    Uri: {
        file: (path: string) => ({ fsPath: path, path }),
    },
}), { virtual: true });

import { DebugManager, DebugState } from '../services/DebugManager';

describe('DebugManager', () => {
    let manager: DebugManager;

    beforeEach(() => {
        jest.clearAllMocks();
        manager = new DebugManager();
    });

    it('Constructor_ShouldSetStoppedState', () => {
        expect(manager.state).toBe(DebugState.Stopped);
    });

    it('LaunchProjectAsync_WhenStopped_ShouldReturnTrue', async () => {
        mockStartDebugging.mockResolvedValue(true);

        const result = await manager.launch();

        expect(result).toBe(true);
        expect(manager.state).toBe(DebugState.Running);
        expect(mockStartDebugging).toHaveBeenCalled();
    });

    it('LaunchProjectAsync_WhenAlreadyRunning_ShouldReturnFalse', async () => {
        mockStartDebugging.mockResolvedValue(true);
        await manager.launch();

        const result = await manager.launch();
        expect(result).toBe(false);
    });

    it('StopAsync_WhenRunning_ShouldStop', async () => {
        mockStartDebugging.mockResolvedValue(true);
        await manager.launch();

        await manager.stop();

        expect(manager.state).toBe(DebugState.Stopped);
        expect(mockStopDebugging).toHaveBeenCalled();
    });

    it('StopAsync_WhenStopped_ShouldNotThrow', async () => {
        await manager.stop();
        expect(manager.state).toBe(DebugState.Stopped);
    });

    it('SetBreakpointAsync_ShouldCallAddBreakpoints', async () => {
        const result = await manager.setBreakpoint('/test/file.ts', 42);
        expect(result).toBe(true);
        expect(mockAddBreakpoints).toHaveBeenCalled();
    });

    it('RemoveBreakpointAsync_ShouldCallRemoveBreakpoints', async () => {
        const result = await manager.removeBreakpoint('/test/file.ts', 42);
        expect(mockRemoveBreakpoints).toHaveBeenCalled();
        // Returns false because no breakpoints exist in the mock
        expect(result).toBe(false);
    });

    it('StateChanged_ShouldFireOnLaunch', async () => {
        mockStartDebugging.mockResolvedValue(true);
        let captured: DebugState | null = null;
        manager.onDidChangeState(s => captured = s);

        await manager.launch();

        expect(captured).toBe(DebugState.Running);
    });

    it('StateChanged_ShouldFireOnStop', async () => {
        mockStartDebugging.mockResolvedValue(true);
        await manager.launch();

        let captured: DebugState | null = null;
        manager.onDidChangeState(s => captured = s);
        await manager.stop();

        expect(captured).toBe(DebugState.Stopped);
    });

    it('Operations_ShouldBeTracked', () => {
        const tracker = manager.tracker;
        expect(tracker).toBeDefined();
        expect(tracker.history).toHaveLength(0);
    });

    it('Launch_ShouldTrackOperation', async () => {
        mockStartDebugging.mockResolvedValue(true);
        await manager.launch();

        expect(manager.tracker.history).toHaveLength(1);
        expect(manager.tracker.history[0].name).toBe('debug.launch');
    });

    it('FullDebugCycle_ShouldTrackAllSteps', async () => {
        mockStartDebugging.mockResolvedValue(true);

        await manager.launch();
        await manager.stop();

        const trace = manager.tracker.formatTrace();
        expect(trace).toContain('debug.launch');
        expect(trace).toContain('debug.stop');
    });
});
