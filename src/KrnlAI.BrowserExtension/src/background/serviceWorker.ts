import { KrnlAiClient } from '../lib/krnlaiClient';
import { getEndpoint } from '../lib/storage';

const client = new KrnlAiClient();

let healthInterval: ReturnType<typeof setInterval> | null = null;

async function refreshEndpoint(): Promise<void> {
  const ep = await getEndpoint();
  client.setBaseUrl(ep);
}

async function checkHealth(): Promise<void> {
  await refreshEndpoint();
  const health = await client.health();
  const status = health?.ok ? 'connected' : 'disconnected';
  await chrome.storage.local.set({ krnlai_connection_status: status });
}

function startHealthCheck(): void {
  if (healthInterval) clearInterval(healthInterval);
  checkHealth();
  healthInterval = setInterval(checkHealth, 30_000);
}

// Context Menus
chrome.runtime.onInstalled.addListener(() => {
  chrome.contextMenus.create({
    id: 'explain-page',
    title: 'Krnl-AI: Explain this page',
    contexts: ['page'],
  });
  chrome.contextMenus.create({
    id: 'extract-memory',
    title: 'Krnl-AI: Extract to memory',
    contexts: ['page', 'selection'],
  });
  chrome.contextMenus.create({
    id: 'separator-1',
    type: 'separator',
    contexts: ['page', 'selection'],
  });
  startHealthCheck();
});

chrome.contextMenus.onClicked.addListener(async (info, tab) => {
  if (!tab?.id) return;
  await refreshEndpoint();

  if (info.menuItemId === 'explain-page') {
    const ctx = await chrome.tabs.sendMessage(tab.id, { type: 'GET_PAGE_CONTEXT' });
    const response = await client.sendMessage(
      'Explain the content of this page in a concise summary.',
      ctx,
    );
    await chrome.sidePanel.open({ windowId: tab.windowId });
    await chrome.runtime.sendMessage({
      type: 'SIDEBAR_RESPONSE',
      content: response.narration || response.error || 'No response',
    });
  }

  if (info.menuItemId === 'extract-memory') {
    const ctx = await chrome.tabs.sendMessage(tab.id, { type: 'GET_PAGE_CONTEXT' });
    const textToExtract = info.selectionText || ctx?.text || '';
    const title = ctx?.title || tab.title || 'Unknown page';
    const ok = await client.extractToMemory(textToExtract, tab.url, title);
    await chrome.runtime.sendMessage({
      type: 'EXTRACT_RESULT',
      ok,
      title,
    });
  }
});

// Handle messages from popup / sidebar
chrome.runtime.onMessage.addListener(
  (message: unknown, _sender: chrome.runtime.MessageSender, sendResponse: (response?: unknown) => void) => {
    const msg = message as { type: string; [key: string]: unknown };

    switch (msg.type) {
      case 'GET_PAGE_CONTEXT': {
        chrome.tabs.query({ active: true, currentWindow: true }, async ([tab]) => {
          if (!tab?.id) {
            sendResponse(null);
            return;
          }
          try {
            const ctx = await chrome.tabs.sendMessage(tab.id, { type: 'GET_PAGE_CONTEXT' });
            sendResponse(ctx);
          } catch {
            sendResponse(null);
          }
        });
        return true;
      }

      case 'SEND_MESSAGE': {
        (async () => {
          const text = msg.text as string;
          const context = msg.context as Record<string, string> | undefined;
          const response = await client.sendMessage(text, context as never);
          sendResponse(response);
        })();
        return true;
      }

      case 'HEALTH_CHECK': {
        (async () => {
          const health = await client.health();
          sendResponse(health);
        })();
        return true;
      }

      case 'EXTRACT_TO_MEMORY': {
        (async () => {
          const text = msg.text as string;
          const source = msg.source as string | undefined;
          const title = msg.title as string | undefined;
          const ok = await client.extractToMemory(text, source, title);
          sendResponse({ ok });
        })();
        return true;
      }

      case 'SEARCH_MEMORY': {
        (async () => {
          const query = msg.query as string;
          const result = await client.searchMemory(query);
          sendResponse(result);
        })();
        return true;
      }

      case 'STREAM_MESSAGE': {
        (async () => {
          const text = msg.text as string;
          const context = msg.context as Record<string, string> | undefined;
          try {
            const body: Record<string, unknown> = { prompt: text };
            if (context) body.pageContext = context;
            const res = await fetch(`${(await getEndpoint()).replace(/\/+$/, '')}/agent/run`, {
              method: 'POST',
              headers: { 'Content-Type': 'application/json' },
              body: JSON.stringify(body),
            });
            if (!res.ok) {
              sendResponse({ error: `HTTP ${res.status}` });
              return;
            }
            const json = await res.json();
            sendResponse({ narration: json.narration || json.error || 'No response' });
          } catch (err: unknown) {
            sendResponse({ error: err instanceof Error ? err.message : 'Connection failed' });
          }
        })();
        return true;
      }

      default:
        sendResponse(null);
    }
  },
);

// Command listener
chrome.commands.onCommand.addListener((command) => {
  if (command === 'toggle-popup') {
    chrome.action.openPopup();
  }
});

// Alarm for health check
chrome.alarms.create('krnlai-health', { periodInMinutes: 0.5 });
chrome.alarms.onAlarm.addListener((alarm) => {
  if (alarm.name === 'krnlai-health') {
    checkHealth();
  }
});
