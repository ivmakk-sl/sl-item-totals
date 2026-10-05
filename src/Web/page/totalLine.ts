// The one line of the mod: the label and the item total of the item under the pointer.
//
// Total: 0 is a real answer and the line still shows, so the player can tell an item that the character holds none of
// from a mod that did not load. The line shows nothing at all only when the item pool of the game could not be read.
import { view } from './core';
import type { TipContext } from '../../Shared/tooltip-lines/web/tooltipLines';

export function totalLines(ctx: TipContext): string[] {
  const data = view.data;
  if (!data || data.failed) return [];
  const configId = ctx.configId || itemConfigId(data.items, ctx.itemId);
  if (!configId) return [];
  const total = data.totals[String(configId)] || 0;
  return [fill(data.line, total)];
}

// The item total in the place of the {n} of the line text. The text comes from the i18n files of the mod, so the
// word, the separator, and the order of the word and the number are a translator's, not this code's.
function fill(line: string, total: number): string {
  return line.replace(/\{n\}/g, String(total));
}

// The item config id of a stack that a web page named by its item instance id only. 0 when the pool holds no such
// stack, which means the character holds none of that item.
function itemConfigId(items: Record<string, number>, itemId: number): number {
  if (!itemId) return 0;
  return items[String(itemId)] || 0;
}
