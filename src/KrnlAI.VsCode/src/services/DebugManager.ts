import * as vscode from 'vscode';
import { OperationTracker } from './OperationTracker';

export enum DebugState {
    Stopped = 'Stopped',
    Running = 'Running',
}

export type DebugStateListener = (state: DebugState) => void;

export class DebugManager {
    private _tracker: OperationTracker;
    private _state: DebugState = DebugState.Stopped;
    private _stateListeners: DebugStateListener[] = [];

    constructor(tracker?: OperationTracker) {
        this._tracker = tracker ?? new OperationTracker();
    }

    get tracker(): OperationTracker {
        return this._tracker;
    }

    get state(): DebugState {
        return this._state;
    }

    onDidChangeState(listener: DebugStateListener): void {
        this._stateListeners.push(listener);
    }

    async launch(projectName?: string, ct?: AbortSignal): Promise<boolean> {
        using op = this._tracker.start('debug.launch', projectName ?? undefined);

        if (this._state !== DebugState.Stopped) {
            op.setError(`Cannot launch: state is ${this._state}`);
            return false;
        }

        try {
            const folder = vscode.workspace.workspaceFolders?.[0];
            const config: string = projectName || '.NET Core Launch';
            const result = await vscode.debug.startDebugging(folder, config);
            if (result) {
                this._setState(DebugState.Running);
                op.setResult('Launched');
                return true;
            } else {
                op.setError('Debugger launch failed (returned false)');
                return false;
            }
        } catch (ex: any) {
            op.setError(ex.message ?? String(ex));
            return false;
        }
    }

    async stop(ct?: AbortSignal): Promise<void> {
        using op = this._tracker.start('debug.stop');

        if (this._state === DebugState.Stopped) {
            op.setResult('Already stopped');
            return;
        }

        try {
            await vscode.debug.stopDebugging();
            this._setState(DebugState.Stopped);
            op.setResult('Stopped');
        } catch (ex: any) {
            op.setError(ex.message ?? String(ex));
        }
    }

    async stepOver(ct?: AbortSignal): Promise<void> {
        await vscode.commands.executeCommand('workbench.action.debug.stepOver');
    }

    async stepInto(ct?: AbortSignal): Promise<void> {
        await vscode.commands.executeCommand('workbench.action.debug.stepInto');
    }

    async continue(ct?: AbortSignal): Promise<void> {
        await vscode.commands.executeCommand('workbench.action.debug.continue');
    }

    async setBreakpoint(filePath: string, lineNumber: number, ct?: AbortSignal): Promise<boolean> {
        using op = this._tracker.start('debug.breakpoint.set', `${filePath}:${lineNumber}`);

        try {
            const bp = new vscode.SourceBreakpoint(
                new vscode.Location(
                    vscode.Uri.file(filePath),
                    new vscode.Position(lineNumber - 1, 0)
                )
            );
            vscode.debug.addBreakpoints([bp]);
            op.setResult('Set');
            return true;
        } catch (ex: any) {
            op.setError(ex.message ?? String(ex));
            return false;
        }
    }

    async removeBreakpoint(filePath: string, lineNumber: number, ct?: AbortSignal): Promise<boolean> {
        using op = this._tracker.start('debug.breakpoint.remove', `${filePath}:${lineNumber}`);

        try {
            const bps = vscode.debug.breakpoints.filter(bp => {
                if (bp instanceof vscode.SourceBreakpoint) {
                    const loc = bp.location;
                    return loc.uri.fsPath === filePath && loc.range.start.line === lineNumber - 1;
                }
                return false;
            });
            vscode.debug.removeBreakpoints(bps);
            op.setResult(bps.length > 0 ? 'Removed' : 'Not found');
            return bps.length > 0;
        } catch (ex: any) {
            op.setError(ex.message ?? String(ex));
            return false;
        }
    }

    private _setState(newState: DebugState): void {
        if (this._state === newState) return;
        this._state = newState;
        for (const listener of this._stateListeners) {
            listener(newState);
        }
    }
}
