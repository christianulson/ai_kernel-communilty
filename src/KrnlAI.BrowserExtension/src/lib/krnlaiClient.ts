import type { CommandResponse, HealthStatus, MemorySearchResult, PageContext } from './types';
import { DEFAULT_ENDPOINT } from './types';
import { getEndpoint } from './storage';

export class KrnlAiClient {
  private baseUrl: string | null = null;

  private async getBaseUrl(): Promise<string> {
    if (!this.baseUrl) {
      this.baseUrl = await getEndpoint();
    }
    return this.baseUrl;
  }

  setBaseUrl(url: string): void {
    this.baseUrl = url.replace(/\/+$/, '');
  }

  async health(): Promise<HealthStatus | null> {
    try {
      const res = await fetch(`${await this.getBaseUrl()}/health`, {
        signal: AbortSignal.timeout(5000),
      });
      if (!res.ok) return null;
      return await res.json();
    } catch {
      return null;
    }
  }

  async sendMessage(
    text: string,
    context?: PageContext,
  ): Promise<CommandResponse> {
    try {
      const body: Record<string, unknown> = { prompt: text };
      if (context) {
        body.pageContext = {
          title: context.title,
          url: context.url,
          text: context.text.substring(0, 50_000),
          metaDescription: context.metaDescription,
        };
      }
      const res = await fetch(`${await this.getBaseUrl()}/agent/run`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(body),
        signal: AbortSignal.timeout(30000),
      });
      if (!res.ok) return { error: `HTTP ${res.status}: ${res.statusText}` };
      return await res.json();
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : 'Connection failed';
      return { error: msg };
    }
  }

  async streamSendMessage(
    text: string,
    context: PageContext | undefined,
    onChunk: (chunk: string) => void,
    onComplete: (full: string) => void,
    onError: (err: Error) => void,
  ): Promise<void> {
    try {
      const body: Record<string, unknown> = { prompt: text };
      if (context) {
        body.pageContext = {
          title: context.title,
          url: context.url,
          text: context.text.substring(0, 50_000),
          metaDescription: context.metaDescription,
        };
      }
      const res = await fetch(`${await this.getBaseUrl()}/agent/run`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(body),
      });
      if (!res.ok) {
        onError(new Error(`HTTP ${res.status}: ${res.statusText}`));
        return;
      }

      const contentType = res.headers.get('content-type') || '';
      const isSSE = contentType.includes('text/event-stream');
      const reader = res.body?.getReader();
      if (!reader) {
        const json = await res.json();
        const content = json.narration || json.error || 'No response';
        onChunk(content);
        onComplete(content);
        return;
      }

      const decoder = new TextDecoder();
      let buffer = '';
      let full = '';

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
                full += chunk;
                onChunk(chunk);
              } catch {
                full += data;
                onChunk(data);
              }
            }
          }
        } else {
          full += buffer;
          onChunk(buffer);
          buffer = '';
        }
      }

      if (!isSSE && buffer) {
        full += buffer;
        onChunk(buffer);
      }

      onComplete(full);
    } catch (err: unknown) {
      onError(err instanceof Error ? err : new Error('Stream failed'));
    }
  }

  async extractToMemory(text: string, source?: string, title?: string): Promise<boolean> {
    try {
      const res = await fetch(`${await this.getBaseUrl()}/memory/upsert`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          docId: `browser-${Date.now()}`,
          title: title || source || 'Browser Extract',
          source: source || 'browser-extension',
          text,
        }),
        signal: AbortSignal.timeout(15000),
      });
      return res.ok;
    } catch {
      return false;
    }
  }

  async searchMemory(query: string): Promise<MemorySearchResult | null> {
    try {
      const res = await fetch(`${await this.getBaseUrl()}/memory/search`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ query, limit: 20 }),
        signal: AbortSignal.timeout(10000),
      });
      if (!res.ok) return null;
      const data = await res.json();
      return data as MemorySearchResult;
    } catch {
      return null;
    }
  }

  async resumePage(text: string): Promise<CommandResponse> {
    return this.sendMessage(`Please summarize this page content:\n\n${text.substring(0, 50_000)}`);
  }
}
