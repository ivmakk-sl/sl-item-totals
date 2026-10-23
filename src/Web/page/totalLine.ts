// The one line of the mod: the label and the item total of the item under the pointer.
//
// Total: 0 is a real answer and the line still shows, so the player can tell an item that the character holds none of
// from a mod that did not load. The line shows nothing at all only when the item pool of the game could not be read.
import { view } from './core';
import { findTipFrame, type TipContext } from '../../Shared/tooltip-lines/web/tooltipLines';
import { pageEntry } from '../../Shared/tooltip-lines/web/pages';

// The count at home that the web page shows itself (game 1.1.19611 and later: "Usable at home: N" in the storage
// window, the shop, and the trade window). Where it shows, the line of the mod gives way to it.
const GAME_COUNT = '.tooltip-home';

export function totalLines(ctx: TipContext): string[] {
  const data = view.data;
  if (!data || data.failed || !data.line) return [];
  if (gameShowsCount(ctx.page)) return [];
  const configId = ctx.configId || itemConfigId(data.items, ctx.itemId);
  if (!configId) return [];
  const total = data.totals[String(configId)] || 0;
  return [fill(data.line, total)];
}

// The item total in the place of the {n} of the line text. The text is the game's text of its count at home, so the
// word, the separator, and the order of the word and the number are the game's, not this code's.
function fill(line: string, total: number): string {
  return line.replace(/\{n\}/g, String(total));
}

// The item config id of a stack that a web page named by its item instance id only. 0 when the pool holds no such
// stack, which means the character holds none of that item.
function itemConfigId(items: Record<string, number>, itemId: number): number {
  if (!itemId) return 0;
  return items[String(itemId)] || 0;
}

// Whether the tooltip of the page shows the count of the game. The library draws the blocks after the web page drew
// its own lines, so the part of the game is there when it shows for this item.
function gameShowsCount(page: string): boolean {
  const w = findTipFrame(window, page);
  const entry = pageEntry(page);
  if (!w || !entry) return false;
  const tip = w.document.querySelector(entry.tip);
  return !!tip && !!tip.querySelector(GAME_COUNT);
}
