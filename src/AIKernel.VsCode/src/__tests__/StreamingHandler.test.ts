import { StreamingHandler } from '../chat/StreamingHandler';

describe('StreamingHandler', () => {
    let handler: StreamingHandler;

    beforeEach(() => {
        handler = new StreamingHandler();
    });

    describe('streamFromUrl', () => {
        it('ShouldHandleSSEStream_WhenServerResponds', async () => {
            const mockStream = new ReadableStream({
                start(controller) {
                    controller.enqueue(new TextEncoder().encode('data: {"choices":[{"delta":{"content":"Hello"}}]}\n'));
                    controller.enqueue(new TextEncoder().encode('data: {"choices":[{"delta":{"content":" World"}}]}\n'));
                    controller.enqueue(new TextEncoder().encode('data: [DONE]\n'));
                    controller.close();
                }
            });

            global.fetch = jest.fn().mockResolvedValue({
                ok: true,
                body: mockStream,
                headers: new Headers({ 'content-type': 'text/event-stream' })
            } as any);

            const chunks: string[] = [];
            const onChunk = jest.fn((chunk: string) => chunks.push(chunk));
            const onComplete = jest.fn();
            const onError = jest.fn();

            await handler.streamFromUrl(
                'http://localhost:5000/api/stream',
                { prompt: 'test' },
                onChunk,
                onComplete,
                onError
            );

            expect(onChunk).toHaveBeenCalledTimes(2);
            expect(chunks).toEqual(['Hello', ' World']);
            expect(onComplete).toHaveBeenCalledWith('Hello World');
            expect(onError).not.toHaveBeenCalled();
        });

        it('ShouldHandleNonStreamingSSE_WhenContentIsPlainText', async () => {
            const mockStream = new ReadableStream({
                start(controller) {
                    controller.enqueue(new TextEncoder().encode('Hello'));
                    controller.enqueue(new TextEncoder().encode(' World'));
                    controller.close();
                }
            });

            global.fetch = jest.fn().mockResolvedValue({
                ok: true,
                body: mockStream,
                headers: new Headers({ 'content-type': 'application/json' })
            } as any);

            const chunks: string[] = [];
            const onComplete = jest.fn();

            await handler.streamFromUrl(
                'http://localhost:5000/api/stream',
                { prompt: 'test' },
                (chunk) => chunks.push(chunk),
                onComplete,
                jest.fn()
            );

            expect(chunks).toEqual(['Hello', ' World']);
            expect(onComplete).toHaveBeenCalledWith('Hello World');
        });

        it('ShouldCallOnError_WhenHttpErrorOccurs', async () => {
            global.fetch = jest.fn().mockResolvedValue({
                ok: false,
                status: 500,
                statusText: 'Internal Server Error'
            } as any);

            const onError = jest.fn();

            await handler.streamFromUrl(
                'http://localhost:5000/api/stream',
                { prompt: 'test' },
                jest.fn(),
                jest.fn(),
                onError
            );

            expect(onError).toHaveBeenCalledWith(expect.any(Error));
            expect(onError.mock.calls[0][0].message).toContain('500');
        });

        it('ShouldCallOnError_WhenResponseBodyIsNull', async () => {
            global.fetch = jest.fn().mockResolvedValue({
                ok: true,
                body: null,
                text: jest.fn().mockResolvedValue(''),
                headers: new Headers({})
            } as any);

            const onError = jest.fn();

            await handler.streamFromUrl(
                'http://localhost:5000/api/stream',
                { prompt: 'test' },
                jest.fn(),
                jest.fn(),
                onError
            );

            expect(onError).toHaveBeenCalledWith(expect.any(Error));
        });

        it('ShouldHandleFetchError', async () => {
            global.fetch = jest.fn().mockRejectedValue(new Error('Network error'));

            const onError = jest.fn();

            await handler.streamFromUrl(
                'http://localhost:5000/api/stream',
                { prompt: 'test' },
                jest.fn(),
                jest.fn(),
                onError
            );

            expect(onError).toHaveBeenCalledWith(expect.any(Error));
        });
    });

    describe('abort', () => {
        it('ShouldAbortWithoutCrashing', async () => {
            const mockStream = new ReadableStream({
                start(controller) {
                    controller.enqueue(new TextEncoder().encode('data: chunk\n'));
                    controller.close();
                }
            });

            global.fetch = jest.fn().mockResolvedValue({ ok: true, body: mockStream, headers: new Headers({ 'content-type': 'text/event-stream' }) } as any);

            const onChunk = jest.fn();
            const onComplete = jest.fn();
            const onError = jest.fn();

            handler.streamFromUrl(
                'http://localhost:5000/api/stream',
                { prompt: 'test' },
                onChunk,
                onComplete,
                onError
            );

            expect(() => handler.abort()).not.toThrow();
        });

        it('ShouldAllowMultipleAbortCalls', () => {
            expect(() => {
                handler.abort();
                handler.abort();
            }).not.toThrow();
        });

        it('ShouldAbortDuringFetchWithoutCrash', async () => {
            global.fetch = jest.fn().mockImplementationOnce((_url: string, opts: any) =>
                new Promise((_resolve, reject) => {
                    const signal = opts?.signal;
                    if (signal) {
                        signal.addEventListener('abort', () => reject(new DOMException('Aborted', 'AbortError')));
                    }
                })
            );

            const promise = handler.streamFromUrl(
                'http://localhost:5000/api/stream',
                { prompt: 'test' },
                jest.fn(), jest.fn(), jest.fn()
            );

            handler.abort();
            await expect(promise).resolves.toBeUndefined();
        });
    });

    describe('non-SSE edge cases', () => {
        it('ShouldHandleEmptyResponseBody', async () => {
            global.fetch = jest.fn().mockResolvedValue({
                ok: true,
                body: null,
                text: jest.fn().mockResolvedValue(''),
                headers: new Headers({})
            } as any);

            const onError = jest.fn();

            await handler.streamFromUrl(
                'http://localhost:5000/api/stream',
                { prompt: 'test' },
                jest.fn(),
                jest.fn(),
                onError
            );

            expect(onError).toHaveBeenCalled();
        });

        it('ShouldHandleTextResponse_WhenNoReaderAndBodyHasText', async () => {
            global.fetch = jest.fn().mockResolvedValue({
                ok: true,
                body: null,
                text: jest.fn().mockResolvedValue('Hello World'),
                headers: new Headers({})
            } as any);

            const chunks: string[] = [];
            const onComplete = jest.fn();

            await handler.streamFromUrl(
                'http://localhost:5000/api/stream',
                { prompt: 'test' },
                (chunk) => chunks.push(chunk),
                onComplete,
                jest.fn()
            );

            expect(chunks).toEqual(['Hello World']);
            expect(onComplete).toHaveBeenCalledWith('Hello World');
        });

        it('ShouldHandleSSEWithoutDataPrefix', async () => {
            const mockStream = new ReadableStream({
                start(controller) {
                    controller.enqueue(new TextEncoder().encode('event: ping\n'));
                    controller.enqueue(new TextEncoder().encode('data: {"content":"hello"}\n\n'));
                    controller.close();
                }
            });

            global.fetch = jest.fn().mockResolvedValue({
                ok: true,
                body: mockStream,
                headers: new Headers({ 'content-type': 'text/event-stream' })
            } as any);

            const chunks: string[] = [];
            const onComplete = jest.fn();

            await handler.streamFromUrl(
                'http://localhost:5000/api/stream',
                { prompt: 'test' },
                (chunk) => chunks.push(chunk),
                onComplete,
                jest.fn()
            );

            expect(chunks.length).toBeGreaterThanOrEqual(1);
            expect(onComplete).toHaveBeenCalled();
        });
    });
});
