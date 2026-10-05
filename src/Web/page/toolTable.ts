// The workbench window. Its tooltip is not drawn by the web page from an item under the pointer: the game builds the
// whole text in C# and the page writes it with one textContent call, so the window is in no page table of the tooltip
// lines library and takes no block through it.
//
// C# puts the line at the end of that text. This moves it out of the text again and draws it as a real tooltip block,
// so the workbench window gets the same rule above the line as every window that has a tooltip of its own. Without
// this module the line still shows, as the last line of the text.
import { PREFIX, addStyle, view } from './core';
import { findTipFrame } from '../../Shared/tooltip-lines/web/tooltipLines';

const PAGE = 'ToolTable';
const TIP = '#tooltip';
const DESC = '#tt-desc';
const BLOCK_ID = 'itemtotals';
// The one property of the mod on the window of the frame, which keeps the watch of the tooltip text.
const STORE = '__itemtotalsToolTable';

interface Store {
  watched: Element | null;
  observer: MutationObserver | null;
  // The text that this module left after it took the line out, or null when it drew no block.
  left: string | null;
  // The text node of that text. The web page writes each text with a new node, so a write of the page whose text
  // happens to equal the left text is still told apart from the text of this module.
  leftNode: Node | null;
}

type WithObserver = Window & { MutationObserver: typeof MutationObserver };

function store(w: Window): Store {
  const holder = w as unknown as Record<string, Store | undefined>;
  let s = holder[STORE];
  if (!s) {
    s = { watched: null, observer: null, left: null, leftNode: null };
    holder[STORE] = s;
  }
  return s;
}

// The part of the line text before the placeholder, for example "Total: ". C# writes the same text, so this is what
// finds the line in the tooltip text. An empty one finds nothing, which leaves the text as the game wrote it.
function head(): string {
  const line = view.data && view.data.line;
  if (!line) return '';
  const at = line.indexOf('{n}');
  return at < 0 ? line : line.slice(0, at);
}

function block(tip: Element): HTMLElement | null {
  return tip.querySelector(':scope > .' + PREFIX + '-tip') as HTMLElement | null;
}

function draw(doc: Document, tip: Element, text: string): void {
  const group = doc.createElement('div');
  group.className = PREFIX + '-tip';
  group.setAttribute('data-sl-tip', BLOCK_ID);
  const line = doc.createElement('div');
  line.className = PREFIX + '-tip-line';
  line.textContent = text;
  group.appendChild(line);
  tip.appendChild(group);
}

// Takes the line out of the tooltip text and draws it as a block below it. Runs on each change of that text, because
// the web page writes the whole text again for each item the pointer reaches, and on each pass of the mod.
function apply(w: Window): void {
  const tip = w.document.querySelector(TIP);
  const desc = w.document.querySelector(DESC) as HTMLElement | null;
  if (!tip || !desc) return;

  const s = store(w);
  const text = desc.textContent || '';
  const found = block(tip);
  // The text is still the one this module left, so the line is already in the block. Each hover refreshes the
  // window, and the pass that C# sends for that refresh comes after the line left the text.
  if (found && text === s.left && desc.firstChild === s.leftNode) return;

  const mark = head();
  const at = mark ? text.lastIndexOf('\n' + mark) : -1;
  if (found) found.remove();
  s.left = null;
  s.leftNode = null;
  if (at < 0) return;
  s.left = text.slice(0, at);
  desc.textContent = s.left;
  s.leftNode = desc.firstChild;
  draw(w.document, tip, text.slice(at + 1));
}

// One watch for each frame. The nodes of the tooltip stay the same while the frame lives, and a frame that goes away
// takes its watch with it.
function watch(w: Window, desc: Element): void {
  const s = store(w);
  if (s.watched === desc) return;
  if (s.observer) s.observer.disconnect();
  s.watched = desc;
  const observer = new (w as WithObserver).MutationObserver(() => {
    apply(w);
    // The writes of this module are no change of the web page, so they are dropped before the next run.
    observer.takeRecords();
  });
  s.observer = observer;
  observer.observe(desc, { childList: true, characterData: true, subtree: true });
}

/// Installs the watch into the frame of the workbench window. Gives what happened, for the answer of the pass.
export function applyToolTable(root: Window): string {
  const w = findTipFrame(root, PAGE);
  if (!w) return 'no frame';
  const desc = w.document.querySelector(DESC);
  if (!desc) return 'no ' + DESC;
  addStyle(w.document);
  watch(w, desc);
  apply(w);
  return 'watched';
}
