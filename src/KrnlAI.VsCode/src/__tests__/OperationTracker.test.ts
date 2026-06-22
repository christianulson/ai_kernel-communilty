jest.mock('vscode', () => ({}), { virtual: true });

import { OperationState, OperationCall, OperationTracker } from '../services/OperationTracker';

describe('OperationState', () => {
    it('OperationState_ShouldHaveFourValues', () => {
        expect(Object.keys(OperationState).length).toBe(4);
        expect(OperationState.Running).toBe('Running');
        expect(OperationState.Completed).toBe('Completed');
        expect(OperationState.Failed).toBe('Failed');
        expect(OperationState.Cancelled).toBe('Cancelled');
    });
});

describe('OperationCall', () => {
    it('OperationCall_Constructor_ShouldSetProperties', () => {
        const call: OperationCall = {
            id: 'op-1',
            name: 'test',
            arguments: 'args',
            state: OperationState.Running,
            result: null,
            error: null,
            elapsedMs: 0,
            startedAt: Date.now(),
            children: null,
        };
        expect(call.id).toBe('op-1');
        expect(call.name).toBe('test');
        expect(call.arguments).toBe('args');
        expect(call.state).toBe(OperationState.Running);
    });
});

describe('OperationTracker', () => {
    it('Start_ShouldCreateRunningOperation', () => {
        const tracker = new OperationTracker();
        using scope = tracker.start('test.op', 'arg1');

        expect(tracker.history).toHaveLength(1);
        expect(tracker.history[0].name).toBe('test.op');
        expect(tracker.history[0].arguments).toBe('arg1');
        expect(tracker.history[0].state).toBe(OperationState.Running);
        expect(tracker.history[0].id).toBeTruthy();
        expect(tracker.history[0].children).toBeNull();
    });

    it('Start_NullArgs_ShouldStoreNull', () => {
        const tracker = new OperationTracker();
        tracker.start('noargs');
        expect(tracker.history[0].arguments).toBeNull();
    });

    it('Dispose_CompletedScope_ShouldMarkCompleted', () => {
        const tracker = new OperationTracker();
        {
            using scope = tracker.start('op');
            scope.setResult('done');
        }

        expect(tracker.history[0].state).toBe(OperationState.Completed);
        expect(tracker.history[0].result).toBe('done');
        expect(tracker.history[0].error).toBeNull();
        expect(tracker.history[0].elapsedMs).toBeGreaterThanOrEqual(0);
    });

    it('SetError_ShouldMarkFailed', () => {
        const tracker = new OperationTracker();
        {
            using scope = tracker.start('op');
            scope.setError('something broke');
        }

        expect(tracker.history[0].state).toBe(OperationState.Failed);
        expect(tracker.history[0].error).toBe('something broke');
        expect(tracker.history[0].result).toBeNull();
    });

    it('Dispose_WithoutSetResultOrError_ShouldMarkCompleted', () => {
        const tracker = new OperationTracker();
        {
            using scope = tracker.start('op');
        }

        expect(tracker.history[0].state).toBe(OperationState.Completed);
    });

    it('StartChild_ShouldNestOperation', () => {
        const tracker = new OperationTracker();
        {
            using parent = tracker.start('parent');
            {
                using child = parent.startChild('child', 'child-arg');
                child.setResult('child-result');
            }
            parent.setResult('done');
        }

        const op = tracker.history[0];
        expect(op.children).not.toBeNull();
        expect(op.children).toHaveLength(1);
        expect(op.children![0].name).toBe('child');
        expect(op.children![0].arguments).toBe('child-arg');
        expect(op.children![0].result).toBe('child-result');
        expect(op.children![0].state).toBe(OperationState.Completed);
    });

    it('MultipleOperations_ShouldAllBeInHistory', () => {
        const tracker = new OperationTracker();
        {
            using a = tracker.start('first'); a.setResult('a');
        }
        {
            using b = tracker.start('second'); b.setResult('b');
        }
        {
            using c = tracker.start('third'); c.setResult('c');
        }

        expect(tracker.history).toHaveLength(3);
        expect(tracker.history[0].name).toBe('first');
        expect(tracker.history[1].name).toBe('second');
        expect(tracker.history[2].name).toBe('third');
    });

    it('Clear_ShouldRemoveAllOperations', () => {
        const tracker = new OperationTracker();
        {
            using op = tracker.start('op'); op.setResult('ok');
        }
        expect(tracker.history).toHaveLength(1);

        tracker.clear();
        expect(tracker.history).toHaveLength(0);
    });

    it('MultipleStartChild_ShouldTrackAllChildren', () => {
        const tracker = new OperationTracker();
        {
            using parent = tracker.start('parent');
            {
                using c1 = parent.startChild('child1'); c1.setResult('r1');
            }
            {
                using c2 = parent.startChild('child2'); c2.setResult('r2');
            }
            {
                using c3 = parent.startChild('child3'); c3.setResult('r3');
            }
            parent.setResult('done');
        }

        expect(tracker.history[0].children).toHaveLength(3);
        expect(tracker.history[0].children![0].name).toBe('child1');
        expect(tracker.history[0].children![1].name).toBe('child2');
        expect(tracker.history[0].children![2].name).toBe('child3');
    });

    it('Children_ShouldNotBeInHistory', () => {
        const tracker = new OperationTracker();
        {
            using parent = tracker.start('parent');
            {
                using child = parent.startChild('child');
                child.setResult('ok');
            }
            parent.setResult('done');
        }

        expect(tracker.history).toHaveLength(1);
        expect(tracker.history[0].name).toBe('parent');
    });

    it('SetResultAfterDispose_ShouldNotThrow', () => {
        const tracker = new OperationTracker();
        let scope = tracker.start('op');
        scope.setResult('ok');
        scope[Symbol.dispose]();
        // Should not throw
        scope.setResult('again');
        scope.setError('late error');
    });

    it('DebugTrace_ShouldFormatHistory', () => {
        const tracker = new OperationTracker();
        {
            using op = tracker.start('test.op', 'arg1');
            op.setResult('done');
        }

        const trace = tracker.formatTrace();
        expect(trace).toContain('test.op');
        expect(trace).toContain('arg1');
    });

    it('DebugTrace_Empty_ShouldReturnMessage', () => {
        const tracker = new OperationTracker();
        expect(tracker.formatTrace()).toContain('No operations');
    });
});
