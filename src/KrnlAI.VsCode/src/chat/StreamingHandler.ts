export type StreamChunkHandler = (chunk: string) => void;
export type StreamCompleteHandler = (fullContent: string) => void;
export type StreamErrorHandler = (error: Error) => void;

export class StreamingHandler {
    private _abortController: AbortController | null = null;

    async streamFromUrl(
        url: string,
        body: any,
        onChunk: StreamChunkHandler,
        onComplete: StreamCompleteHandler,
        onError: StreamErrorHandler
    ): Promise<void> {
        this._abortController = new AbortController();
        let fullContent = '';

        try {
            const response = await fetch(url, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(body),
                signal: this._abortController.signal
            });

            if (!response.ok) {
                onError(new Error(`HTTP ${response.status}: ${response.statusText}`));
                return;
            }

            const reader = response.body?.getReader();
            if (!reader) {
                const text = await response.text().catch(() => '');
                if (text) {
                    fullContent = text;
                    onChunk(text);
                    onComplete(text);
                } else {
                    onError(new Error('Response body is null'));
                }
                return;
            }

            const contentType = response.headers.get('content-type') || '';
            const isSSE = contentType.includes('text/event-stream');

            const decoder = new TextDecoder();
            let buffer = '';

            while (true) {
                const { done, value } = await reader.read();
                if (done) break;

                buffer += decoder.decode(value, { stream: true });

                if (isSSE) {
                    const lines = buffer.split('\n');
                    buffer = lines.pop() || '';

                    for (const line of lines) {
                        if (line.startsWith('data: ')) {
                            const data = line.slice(6);
                            if (data === '[DONE]') continue;
                            try {
                                const parsed = JSON.parse(data);
                                const chunk = parsed.choices?.[0]?.delta?.content || parsed.content || data;
                                fullContent += chunk;
                                onChunk(chunk);
                            } catch {
                                fullContent += data;
                                onChunk(data);
                            }
                        }
                    }
                } else {
                    fullContent += buffer;
                    onChunk(buffer);
                    buffer = '';
                }
            }

            if (!isSSE && buffer) {
                fullContent += buffer;
                onChunk(buffer);
            }

            onComplete(fullContent);
        } catch (err: any) {
            if (err.name === 'AbortError') return;
            onError(err);
        }
    }

    abort() {
        this._abortController?.abort();
    }
}
