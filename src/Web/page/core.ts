// The parts that the page script shares: the stored data, the messages to C#, and the style node of the mod.
import tokensCss from '../tokens.css?inline';
import pageCss from '../page.css?inline';
import { tooltipLinesCss } from '../../Shared/tooltip-lines/web/tooltipLines';
import type { PageData } from './types';

// The prefix of the class names and the CSS variables of this mod.
export const PREFIX = 'it';

// The data of the last setData; null until C# sends it.
export const view: { data: PageData | null } = { data: null };

// The texts that the script posted to C# since the root page has it, so each is posted once.
const posted = new Set<string>();

// Tells C# what it must act on, by message (C# sends each call with no callback).
export function post(text: string): void {
  // The answer goes out on a later task, not inside the ExecuteJavaScript call that asked for it. That call is
  // synchronous and C# makes it from the per-frame update of the web UI layer, so a message posted inside it reaches
  // the Vuplex dispatcher while the game is still in its own update and in the middle of a flush to the pages.
  setTimeout(() => {
    try {
      window.vuplex?.postMessage('slmod|itemtotals|' + text);
    } catch {
      // The root page has no Vuplex bridge.
    }
  }, 0);
}

// Posts a status text once for each distinct text, which is what makes the warn-once rule of C# work even while the
// page keeps the script across many passes.
export function postOnce(text: string): void {
  if (posted.has(text)) return;
  posted.add(text);
  post(text);
}

// The one style node of the mod in a frame: the tokens of the mod, then the rules of the library, then the rules of
// the mod, so a later rule can override an earlier one.
export function addStyle(doc: Document): void {
  if (doc.getElementById('itemtotals-style')) return;
  const style = doc.createElement('style');
  style.id = 'itemtotals-style';
  style.textContent = [tokensCss, tooltipLinesCss(PREFIX), pageCss].join('\n');
  doc.head.appendChild(style);
}
