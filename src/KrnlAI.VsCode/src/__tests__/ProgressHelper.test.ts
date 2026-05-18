jest.mock('vscode', () => ({
    window: {
        createStatusBarItem: jest.fn().mockReturnValue({
            show: jest.fn(),
            dispose: jest.fn(),
            text: '',
            tooltip: '',
        }),
        withProgress: jest.fn().mockImplementation(async (_opts: any, task: (p: any, t: any) => Promise<any>) => {
            const progress = { report: jest.fn() };
            const token = { isCancellationRequested: false, onCancellationRequested: jest.fn() };
            return task(progress, token);
        }),
        showWarningMessage: jest.fn(),
    },
    StatusBarAlignment: { Left: 1, Right: 2 },
    ProgressLocation: { Notification: 1, Window: 2 },
}), { virtual: true });

import { withProgress, withLongRunningOperation, createStatusBarProgress } from '../utils/ProgressHelper';

describe('ProgressHelper', () => {
    beforeEach(() => {
        jest.clearAllMocks();
    });

    it('withProgress_ShouldExecuteTask', async () => {
        const result = await withProgress('Test', async (progress, token) => {
            progress.report({ message: 'working', increment: 50 });
            return 42;
        });
        expect(result).toBe(42);
    });

    it('withProgress_ShouldUseNotificationLocation', async () => {
        const { window } = require('vscode');
        await withProgress('Test', async () => 'done');
        expect(window.withProgress).toHaveBeenCalledWith(
            expect.objectContaining({ location: 1 }),
            expect.any(Function)
        );
    });

    it('withLongRunningOperation_ShouldExecuteTask', async () => {
        const result = await withLongRunningOperation('Long task', async () => 'completed');
        expect(result).toBe('completed');
    });

    it('withLongRunningOperation_ShouldReportSteps', async () => {
        const { window } = require('vscode');
        const progress = { report: jest.fn() };
        const token = { isCancellationRequested: false, onCancellationRequested: jest.fn() };
        window.withProgress.mockImplementationOnce(async (_opts: any, task: any) => task(progress, token));

        await withLongRunningOperation('Task', async () => 'done', {
            steps: ['Step 1', 'Step 2', 'Step 3']
        });

        expect(progress.report).toHaveBeenCalled();
    });

    it('createStatusBarProgress_ShouldCreateAndShow', () => {
        const item = createStatusBarProgress('$(sync) Working...', 'Krnl-AI task in progress');
        expect(item).toBeDefined();
        expect(item.text).toBe('$(sync) Working...');
        expect(item.tooltip).toBe('Krnl-AI task in progress');
    });

    it('createStatusBarProgress_DefaultTooltip', () => {
        const item = createStatusBarProgress('Loading');
        expect(item.tooltip).toBe('Loading');
    });
});
