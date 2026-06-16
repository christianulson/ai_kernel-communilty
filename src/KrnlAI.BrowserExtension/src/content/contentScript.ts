import { extractPageContent, extractSelection } from '../lib/contextExtractor';
import type { PageContext } from '../lib/types';

let lastSelection: { text: string; contextBefore: string; contextAfter: string } | null = null;

document.addEventListener('selectionchange', () => {
  const sel = extractSelection();
  if (sel) {
    lastSelection = sel;
  }
});

chrome.runtime.onMessage.addListener(
  (message: unknown, _sender: chrome.runtime.MessageSender, sendResponse: (response?: unknown) => void) => {
    const msg = message as { type: string };
    switch (msg.type) {
      case 'GET_PAGE_CONTEXT': {
        const ctx: PageContext = extractPageContent();
        if (lastSelection) {
          ctx.selection = lastSelection.text;
        }
        sendResponse(ctx);
        break;
      }
      case 'GET_SELECTION': {
        const sel = extractSelection();
        sendResponse(sel || null);
        break;
      }
      default:
        sendResponse(null);
    }
    return true;
  },
);
