// The item detail popup. It is the one window the mod touches that has no item tooltip, so it goes through neither
// the tooltip lines library nor the page table: the mod writes one span into the head line of the popup, after the
// weight and the size of the item.
//
// The number comes from C#, which knows the item config id when the popup opens. The page keeps the text only to
// write it again, so it can never show a number of its own.
//
// The web page builds the head line from three fields of its own, as the one text child of the node, so each draw of
// the popup sets textContent and drops the span. The popup is modal, so no action of the player and no send of C#
// comes in between: the page watches the node and writes the line again itself.
import { PREFIX } from './core';
import { findTipFrame } from '../../Shared/tooltip-lines/web/tooltipLines';

const PAGE = 'ItemDetailPopup';
const META = '.hero-meta';
const MARK = PREFIX + '-popup';
// The one property of the mod on the window of the frame: the text of the line and the watch of the head line.
const STORE = '__itemtotalsPopup';

interface Store {
  text: string;
  watched: Element | null;
  observer: MutationObserver | null;
}

// The watch of a frame is built with the constructor of that frame, which the Window type does not carry.
type WithObserver = Window & { MutationObserver: typeof MutationObserver };

function store(w: Window): Store {
  const holder = w as unknown as Record<string, Store | undefined>;
  let s = holder[STORE];
  if (!s) {
    s = { text: '', watched: null, observer: null };
    holder[STORE] = s;
  }
  return s;
}

function span(meta: HTMLElement): HTMLElement | null {
  return meta.querySelector(':scope > .' + MARK) as HTMLElement | null;
}

function write(w: Window, meta: HTMLElement, text: string): void {
  const found = span(meta);
  if (!text) {
    if (found) found.remove();
    return;
  }
  let node = found;
  if (!node) {
    node = w.document.createElement('span');
    node.className = MARK;
    meta.appendChild(node);
  }
  // The separator of the head line is a middle dot, which the web page uses between the weight and the size.
  node.textContent = ' · ' + text;
}

// Watches the head line for the draw that drops the span, and writes the line again. One watch for each frame: the
// node of the head line stays the same while the frame lives, and a frame that goes away takes its watch with it.
function watch(w: Window, meta: HTMLElement): void {
  const s = store(w);
  if (s.watched === meta) return;
  if (s.observer) s.observer.disconnect();
  s.watched = meta;
  const observer = new (w as WithObserver).MutationObserver(() => {
    if (!s.text) return;
    if (span(meta)) return;
    write(w, meta, s.text);
  });
  s.observer = observer;
  observer.observe(meta, { childList: true, characterData: true, subtree: true });
}

// Writes the line into the head line of the popup. An empty text takes the line out again and keeps it out. Returns
// what happened, which C# logs behind Verbose.
export function showPopupLine(root: Window, text: string): string {
  // The frame is looked up again on each call: a popup that opens again can be a new frame.
  const w = findTipFrame(root, PAGE);
  if (!w) return 'no frame';
  const s = store(w);
  s.text = text || '';
  const meta = w.document.querySelector(META) as HTMLElement | null;
  // A popup with no line asks for nothing in the head line, so a popup without one is no break of the contract.
  if (!meta) return s.text ? 'no ' + META : 'cleared';

  watch(w, meta);
  write(w, meta, s.text);
  return s.text ? 'shown' : 'cleared';
}
