jest.mock('child_process', () => {
    const mockSpawn = jest.fn();
    return { spawn: mockSpawn };
});

jest.mock('vscode', () => ({
    workspace: {
        workspaceFolders: [{ uri: { fsPath: '/workspace' } }],
    },
}), { virtual: true });

import { GitManager } from '../codingAgent/GitManager';
import { spawn } from 'child_process';

function createMockChildProcess(result?: string) {
    const stdout = { on: jest.fn() };
    const stderr = { on: jest.fn() };
    const child = {
        stdout,
        stderr,
        on: jest.fn(),
        kill: jest.fn(),
    };

    stdout.on.mockImplementation((event: string, cb: (data: Buffer) => void) => {
        if (event === 'data' && result !== undefined) {
            cb(Buffer.from(result, 'utf-8'));
        }
        return stdout;
    });

    stderr.on.mockImplementation(() => stderr);

    child.on.mockImplementation((event: string, cb: (code: number) => void) => {
        if (event === 'close') {
            cb(0);
        }
        return child;
    });

    return child;
}

describe('GitManager', () => {
    let manager: GitManager;

    beforeEach(() => {
        jest.clearAllMocks();
        manager = new GitManager();
    });

    describe('isDangerous', () => {
        it('isDangerous_NormalCommit_ShouldNotBeDangerous', () => {
            const result = manager.isDangerous('git commit -m "fix"');
            expect(result.dangerous).toBe(false);
        });

        it('isDangerous_PushForce_ShouldBeDangerous', () => {
            const result = manager.isDangerous('git push --force origin main');
            expect(result.dangerous).toBe(true);
        });

        it('isDangerous_ResetHard_ShouldBeDangerous', () => {
            const result = manager.isDangerous('git reset --hard HEAD~1');
            expect(result.dangerous).toBe(true);
        });

        it('isDangerous_DeleteBranch_ShouldBeDangerous', () => {
            const result = manager.isDangerous('git branch -D feature/x');
            expect(result.dangerous).toBe(true);
        });

        it('isDangerous_NormalPush_ShouldNotBeDangerous', () => {
            const result = manager.isDangerous('git push origin main');
            expect(result.dangerous).toBe(false);
        });
    });

    describe('dangerousPatterns', () => {
        it('dangerousPatterns_ShouldContainKnownPatterns', () => {
            const patterns = manager.dangerousPatterns;
            expect(patterns.length).toBeGreaterThanOrEqual(6);
        });
    });

    describe('spawn based methods', () => {
        beforeEach(() => {
            const mockChild = createMockChildProcess('');
            (spawn as jest.Mock).mockReturnValue(mockChild);
        });

        it('getCurrentBranch_ShouldReturnBranch', async () => {
            const mockChild = createMockChildProcess('main\n');
            (spawn as jest.Mock).mockReturnValue(mockChild);

            const branch = await manager.getCurrentBranch();
            expect(branch).toBe('main');
            expect(spawn).toHaveBeenCalledWith('git', ['rev-parse', '--abbrev-ref', 'HEAD'], expect.any(Object));
        });

        it('getBranches_ShouldReturnList', async () => {
            const mockChild = createMockChildProcess('* main\n  feature/x\n  remotes/origin/main\n');
            (spawn as jest.Mock).mockReturnValue(mockChild);

            const branches = await manager.getBranches();
            expect(branches.length).toBe(3);
            expect(branches[0].current).toBe(true);
            expect(branches[0].name).toBe('main');
        });

        it('commit_ShouldRunAddAndCommit', async () => {
            let callCount = 0;
            const mockChild = createMockChildProcess('');
            const mockChild2 = createMockChildProcess('[main abc1234] fix: bug');
            (spawn as jest.Mock)
                .mockReturnValueOnce(mockChild)
                .mockReturnValueOnce(mockChild2);

            const result = await manager.commit('fix: bug de login');
            expect(result.success).toBe(true);
            expect(spawn).toHaveBeenNthCalledWith(1, 'git', ['add', '-A'], expect.any(Object));
            expect(spawn).toHaveBeenNthCalledWith(2, 'git', ['commit', '-m', 'fix: bug de login'], expect.any(Object));
        });
    });
});
