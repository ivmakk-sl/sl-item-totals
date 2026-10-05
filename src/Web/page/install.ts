// One pass over the open windows: add the block of the mod to the tooltip of each listed web page that is open.
//
// The pass does not retry: when the frame of a window is not there yet, C# sends again. The answer of each pass tells
// C# the count of frames the line went into and the data version the page holds, so C# knows whether a listed window
// is open at all and whether the page already has this data.
import { PREFIX, addStyle, post, postOnce, view } from './core';
import { totalLines } from './totalLine';
import { applyToolTable } from './toolTable';
import { addTipLines, findTipFrame } from '../../Shared/tooltip-lines/web/tooltipLines';

// The place of the line among the blocks of the mods: last, below what the item is and what it is worth.
const RANK = 40;

const BLOCK_ID = 'itemtotals';

export function run(): string {
  const data = view.data;
  if (!data) return 'frames 0';
  let frames = 0;
  for (const page of data.pages) {
    try {
      const report = addTipLines(window, { page, id: BLOCK_ID, rank: RANK, prefix: PREFIX, lines: totalLines });
      // Null means the window of that page is not open.
      if (!report) continue;
      if (report.missing.length) {
        postOnce(report.page + ' has no ' + report.missing.join(', '));
        continue;
      }
      const frame = findTipFrame(window, page);
      if (frame) addStyle(frame.document);
      frames++;
    } catch (e) {
      postOnce('error: ' + page + ': ' + e);
    }
  }
  // The workbench window takes no block through the library: its tooltip is built in C#, and this moves the line of
  // that text into a block of its own. It counts as a frame when its window is open.
  try {
    const answer = applyToolTable(window);
    if (answer === 'watched') frames++;
    else if (answer.indexOf('no #') === 0) postOnce('ToolTable has ' + answer);
  } catch (e) {
    postOnce('error: ToolTable: ' + e);
  }
  const text = 'frames ' + frames + ' version ' + data.v;
  post(text);
  return text;
}
