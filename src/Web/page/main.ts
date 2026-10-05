// Runs in the root page, and reaches the frame of each window through iframe.contentWindow. Defines
// window.__itemtotals once. C# calls setData(data) when the item totals change or the root page has no script, and
// apply() at each other refresh of a listed window. Each call runs one pass and does not retry: when the frame of a
// window is not there yet, C# sends again later. A second full send of the script keeps the first interface and its
// data, so two sends never build two registries.
import { post, postOnce, view } from './core';
import { run } from './install';
import { showPopupLine } from './popup';
import type { PageData } from './types';

// Stores the data and applies it in one pass.
function setData(data: PageData): string {
  view.data = data || null;
  return run();
}

// The item detail popup, which C# drives on its own: it opens and draws again on actions of its own, and it carries
// no item tooltip, so it is no part of the pass over the listed windows.
//
// A head line that the web page no longer has is a break of the contract and goes to the log once, the way a missing
// part of a listed window does. A popup that is not open is no break: C# sends from the action that opens it, so the
// frame can still be on its way.
function popup(text: string): string {
  const answer = showPopupLine(window, text);
  // C# sends again until this message comes, because the frame of the popup is built after the action that opened
  // it ran.
  if (answer === 'shown' || answer === 'cleared') post('popup shown');
  else if (answer.indexOf('no .') === 0) postOnce('popup ' + answer);
  return answer;
}

window.__itemtotals = window.__itemtotals || { setData, apply: run, popup };
