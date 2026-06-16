import type { PageContext } from './types';

function getMetaContent(name: string): string | undefined {
  const el = document.querySelector(`meta[name="${name}"], meta[property="${name}"]`);
  return el?.getAttribute('content') || undefined;
}

export function extractPageContent(): PageContext {
  const text = document.body?.innerText || '';
  return {
    title: document.title || '',
    url: window.location.href || '',
    text: text.substring(0, 100_000),
    metaDescription: getMetaContent('description') || getMetaContent('og:description'),
  };
}

export function extractSelection(): { text: string; contextBefore: string; contextAfter: string } | null {
  const selection = window.getSelection();
  if (!selection || selection.isCollapsed || !selection.rangeCount) return null;

  const range = selection.getRangeAt(0);
  const selectedText = selection.toString().trim();
  if (!selectedText) return null;

  const container = range.commonAncestorContainer;
  const fullText = container.textContent || '';
  const startOffset = range.startOffset;
  const endOffset = range.endOffset;

  const contextBefore = fullText.substring(Math.max(0, startOffset - 500), startOffset).trim();
  const contextAfter = fullText.substring(endOffset, Math.min(fullText.length, endOffset + 500)).trim();

  return { text: selectedText, contextBefore, contextAfter };
}
