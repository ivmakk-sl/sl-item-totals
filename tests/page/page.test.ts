// Runs the page script bundle against the game's own web pages, so a game update that renames or removes a part the
// script depends on shows up here instead of only in the game.
//
// The script runs in the root page and reaches a window through iframe.contentWindow, which is the shape here: a
// blank root window with the iframe list given, and the real web page of the game loaded in its own jsdom window.
import assert from 'node:assert/strict';
import fs from 'node:fs';
import path from 'node:path';
import url from 'node:url';
import vm from 'node:vm';
import { JSDOM } from 'jsdom';
import { test, type TestContext } from 'vitest';

// The jsdom windows carry the page globals of the game, which have no types.
type Win = any;

const HERE = path.dirname(url.fileURLToPath(import.meta.url));
// The web pages of the game: SL_WEBUI_DIR when it is set, else a web UI mirror (game/webui) two folders above the mod
// when there is one, else the web UI folder of the installed game (SL_GAME_DIR, or the default Steam path).
const MIRROR = path.join(HERE, '..', '..', '..', '..', 'game', 'webui');
const GAME_DIR = process.env.SL_GAME_DIR || 'C:\\Program Files (x86)\\Steam\\steamapps\\common\\Survival Log';
const WEB_UI = process.env.SL_WEBUI_DIR
  || (fs.existsSync(MIRROR) ? MIRROR : path.join(GAME_DIR, 'SurvivalLog_Data', 'StreamingAssets', 'WebUI'));
const PAGE_JS = path.join(HERE, '..', '..', 'obj', 'page', 'page.js');

const pageJs = fs.readFileSync(PAGE_JS, 'utf8');

function gamePage(name: string): string {
  return path.join(WEB_UI, 'UI', name, name + '.html');
}

// A real web page of the game, with its own scripts run.
async function loadFrame(t: TestContext, name: string): Promise<Win> {
  const file = gamePage(name);
  const dom = new JSDOM(fs.readFileSync(file, 'utf8'), {
    url: url.pathToFileURL(file).href,
    runScripts: 'dangerously',
    resources: 'usable',
    pretendToBeVisual: true,
  });
  t.onTestFinished(() => dom.window.close());
  await new Promise<void>((resolve, reject) => {
    if (dom.window.document.readyState === 'complete') return resolve();
    dom.window.addEventListener('load', () => resolve());
    setTimeout(() => reject(new Error(name + '.html did not fire load within 10s')), 10000);
  });
  return dom.window;
}

// The root page that C# runs the script in. The iframe list is given, because jsdom loads no iframe from a file URL.
function rootWindow(t: TestContext, frames: Record<string, Win>): Win {
  const root = new JSDOM('<!doctype html><html><body></body></html>', { url: 'file:///Root.html', pretendToBeVisual: true }).window as Win;
  t.onTestFinished(() => root.close());
  const list = Object.keys(frames).map((name) => ({ contentWindow: frames[name] }));
  const real = root.document.querySelectorAll.bind(root.document);
  root.document.querySelectorAll = (sel: string) => (sel === 'iframe' ? list : real(sel));
  root.posted = [] as string[];
  root.vuplex = { postMessage: (text: string) => root.posted.push(text) };
  vm.createContext(root);
  return root;
}

// vm.runInContext, not root.eval, so the bare window identifier of the script resolves to the root window, the same
// as it does for a real page in a browser context.
function runPageJs(root: Win, call: string): string {
  return vm.runInContext(pageJs + ';' + call, root, { filename: 'page.js' });
}

function setData(root: Win, data: object): string {
  return runPageJs(root, 'window.__itemtotals.setData(' + JSON.stringify(data) + ')');
}

function data(over: Record<string, unknown> = {}): Record<string, unknown> {
  return {
    v: 1,
    line: 'Total: {n}',
    pages: ['BackpackUI'],
    totals: { '101': 15 },
    items: { '9001': 101 },
    ...over,
  };
}

function setupOf(frame: Win): Record<string, any> {
  return frame.document.getElementById('app')._vnode.component.setupState;
}

function vueTick(frame: Win): Promise<void> {
  return new Promise((resolve) => frame.Vue.nextTick(() => frame.requestAnimationFrame(() => frame.setTimeout(resolve, 0))));
}

async function hover(frame: Win, item: object): Promise<void> {
  setupOf(frame).onItemEnter({ clientX: 40, clientY: 50 }, 'A', item);
  await vueTick(frame);
}

// The page posts its answer on a later task, not inside the call that asked for it, so a test that reads the answer
// waits for that task first.
function posts(): Promise<void> {
  return new Promise((resolve) => setTimeout(resolve, 0));
}

function block(frame: Win): HTMLElement | null {
  return frame.document.querySelector('div.tooltip > .it-tip');
}

test('the line shows with the text of the display language and the item total', async (t) => {
  const frame = await loadFrame(t, 'BackpackUI');
  const root = rootWindow(t, { BackpackUI: frame });
  setData(root, data());

  await hover(frame, { id: 9001, cfgId: 101, name: 'Hardtack' });

  assert.equal(block(frame)!.textContent, 'Total: 15');
});

test('the line shows Total: 0 for an item that the character holds none of', async (t) => {
  const frame = await loadFrame(t, 'BackpackUI');
  const root = rootWindow(t, { BackpackUI: frame });
  setData(root, data());

  await hover(frame, { id: 9002, cfgId: 202, name: 'Canned Fish' });

  assert.equal(block(frame)!.textContent, 'Total: 0');
});

// The shop window sends an item cell of the character with its item instance id only, so the line has to find the
// item config id in the id map of the push.
test('an item cell named by its item instance id finds its item through the id map', async (t) => {
  const frame = await loadFrame(t, 'ShopUI');
  const root = rootWindow(t, { ShopUI: frame });
  setData(root, data({ pages: ['ShopUI'] }));

  setupOf(frame).onItemEnter({ clientX: 40, clientY: 50 }, { id: 9001, name: 'Hardtack' });
  await vueTick(frame);

  assert.equal(block(frame)!.textContent, 'Total: 15');
});

test('an item of the stock of the shop is named by its own item config id', async (t) => {
  const frame = await loadFrame(t, 'ShopUI');
  const root = rootWindow(t, { ShopUI: frame });
  setData(root, data({ pages: ['ShopUI'] }));

  setupOf(frame).onItemEnter({ clientX: 40, clientY: 50 }, { itemConfigId: 101, remain: 2, name: 'Hardtack' });
  await vueTick(frame);

  assert.equal(block(frame)!.textContent, 'Total: 15');
});

test('an item cell whose stack the pool does not hold draws no line, because the mod cannot name the item', async (t) => {
  const frame = await loadFrame(t, 'ShopUI');
  const root = rootWindow(t, { ShopUI: frame });
  setData(root, data({ pages: ['ShopUI'] }));

  setupOf(frame).onItemEnter({ clientX: 40, clientY: 50 }, { id: 7777, name: 'Hardtack' });
  await vueTick(frame);

  assert.equal(block(frame), null);
});

test('the lines of the game keep their place above the line of the mod', async (t) => {
  const frame = await loadFrame(t, 'BackpackUI');
  const root = rootWindow(t, { BackpackUI: frame });
  setData(root, data());

  await hover(frame, { id: 9001, cfgId: 101, name: 'Hardtack' });

  const tip = frame.document.querySelector('div.tooltip');
  assert.equal(tip.firstElementChild.className, 'tooltip-name');
  assert.equal(tip.lastElementChild.className, 'it-tip');
});

test('the mod draws no line while the item pool of the game could not be read', async (t) => {
  const frame = await loadFrame(t, 'BackpackUI');
  const root = rootWindow(t, { BackpackUI: frame });
  setData(root, data({ failed: true, totals: {}, items: {} }));

  await hover(frame, { id: 9001, cfgId: 101, name: 'Hardtack' });

  assert.equal(block(frame), null);
});

test('a new push shows the new number on the next hover', async (t) => {
  const frame = await loadFrame(t, 'BackpackUI');
  const root = rootWindow(t, { BackpackUI: frame });
  setData(root, data());
  await hover(frame, { id: 9001, cfgId: 101, name: 'Hardtack' });

  setData(root, data({ v: 2, totals: { '101': 14 } }));
  await hover(frame, { id: 9001, cfgId: 101, name: 'Hardtack' });

  assert.equal(block(frame)!.textContent, 'Total: 14');
});

test('the pass answers with the count of frames and the data version', async (t) => {
  const frame = await loadFrame(t, 'BackpackUI');
  const root = rootWindow(t, { BackpackUI: frame });

  const answer = setData(root, data({ v: 7 }));
  await posts();

  assert.equal(answer, 'frames 1 version 7');
  assert.ok(root.posted.includes('slmod|itemtotals|frames 1 version 7'));
});

test('the pass answers frames 0 while no listed window is open', async (t) => {
  const root = rootWindow(t, {});

  const answer = setData(root, data());

  assert.equal(answer, 'frames 0 version 1');
});

test('a web page with a renamed tooltip part reaches C# with its name and the missing part', async (t) => {
  const frame = await loadFrame(t, 'BackpackUI');
  // A game update that renamed the hover part of the page.
  delete setupOf(frame).onItemEnter;
  const root = rootWindow(t, { BackpackUI: frame });

  setData(root, data());
  await posts();

  const missing = root.posted.filter((text: string) => text.indexOf('has no') >= 0);
  assert.equal(missing.length, 1);
  assert.match(missing[0], /BackpackUI has no onItemEnter/);
});

test('a missing part is posted once however many passes run', async (t) => {
  const frame = await loadFrame(t, 'BackpackUI');
  delete setupOf(frame).onItemEnter;
  const root = rootWindow(t, { BackpackUI: frame });

  setData(root, data());
  setData(root, data({ v: 2 }));
  runPageJs(root, 'window.__itemtotals.apply()');
  await posts();

  assert.equal(root.posted.filter((text: string) => text.indexOf('has no') >= 0).length, 1);
});

test('a second full send keeps the first interface and its data', async (t) => {
  const frame = await loadFrame(t, 'BackpackUI');
  const root = rootWindow(t, { BackpackUI: frame });
  setData(root, data({ v: 3, totals: { '101': 12 } }));
  const first = root.__itemtotals;

  // C# sends the script again after a browser rebuild; the guard of main.ts keeps the first object.
  runPageJs(root, '1');

  assert.equal(root.__itemtotals, first);
  await hover(frame, { id: 9001, cfgId: 101, name: 'Hardtack' });
  assert.equal(block(frame)!.textContent, 'Total: 12');
});

test('the style node of the mod goes into the frame once', async (t) => {
  const frame = await loadFrame(t, 'BackpackUI');
  const root = rootWindow(t, { BackpackUI: frame });

  setData(root, data());
  runPageJs(root, 'window.__itemtotals.apply()');

  assert.equal(frame.document.querySelectorAll('#itemtotals-style').length, 1);
  const css = frame.document.getElementById('itemtotals-style').textContent;
  assert.match(css, /\.it-tip \{/);
  assert.match(css, /--it-tip-fg:/);
});

test('apply with no data yet answers frames 0 and draws nothing', async (t) => {
  const frame = await loadFrame(t, 'BackpackUI');
  const root = rootWindow(t, { BackpackUI: frame });

  const answer = runPageJs(root, 'window.__itemtotals.apply()');

  assert.equal(answer, 'frames 0');
  await hover(frame, { id: 9001, cfgId: 101, name: 'Hardtack' });
  assert.equal(block(frame), null);
});

// The line text comes from the i18n files of the mod with the item total as {n}, so the word, the separator, and the
// order of the word and the number are a translator's.
test('the page fills {n} with the item total, wherever it sits in the text', async (t) => {
  const frame = await loadFrame(t, 'BackpackUI');
  const root = rootWindow(t, { BackpackUI: frame });
  setData(root, data({ line: '{n} in all' }));

  await hover(frame, { id: 9001, cfgId: 101, name: 'Hardtack' });

  assert.equal(block(frame)!.textContent, '15 in all');
});

test('the Chinese text of the line fills the same way', async (t) => {
  const frame = await loadFrame(t, 'BackpackUI');
  const root = rootWindow(t, { BackpackUI: frame });
  setData(root, data({ line: '持有: {n}' }));

  await hover(frame, { id: 9001, cfgId: 101, name: 'Hardtack' });

  assert.equal(block(frame)!.textContent, '持有: 15');
});

test('a text with no placeholder shows as it is, so a broken i18n file cannot break the tooltip', async (t) => {
  const frame = await loadFrame(t, 'BackpackUI');
  const root = rootWindow(t, { BackpackUI: frame });
  setData(root, data({ line: 'Total' }));

  await hover(frame, { id: 9001, cfgId: 101, name: 'Hardtack' });

  assert.equal(block(frame)!.textContent, 'Total');
});

// Each window of the mod, against its own real web page of the game. The library has the tests of the two tooltip
// shapes; these say that the pass of this mod reaches every listed window and finds each part it needs, so a game
// update that renames a part fails here and not in the game.
const WINDOWS = [
  'BackpackUI', 'TradeUI', 'ShopUI', 'Cooking', 'MaterialRack', 'AidDepot', 'BrewPanel',
  'DailyFert', 'DroneHub', 'DyePanel', 'RatCage', 'Compressor', 'Shredder', 'GreenhouseBuild',
];

for (const name of WINDOWS) {
  test('the line installs into the real page of ' + name, async (t) => {
    const frame = await loadFrame(t, name);
    const root = rootWindow(t, { [name]: frame });

    const answer = setData(root, data({ pages: [name] }));
    await posts();

    assert.equal(answer, 'frames 1 version 1');
    const missing = root.posted.filter((text: string) => text.indexOf('has no') >= 0 || text.indexOf('error: ') >= 0);
    assert.deepEqual(missing, []);
  });
}

test('one pass installs into every window of the mod at once', async (t) => {
  const frames: Record<string, Win> = {};
  for (const name of WINDOWS) frames[name] = await loadFrame(t, name);
  const root = rootWindow(t, frames);

  const answer = setData(root, data({ pages: WINDOWS }));
  await posts();

  assert.equal(answer, 'frames ' + WINDOWS.length + ' version 1');
});

// The item detail popup. It carries no item tooltip, so the mod writes one span into the head line of the popup,
// after the weight and the size, and C# drives it with each open and each redraw of the popup.
function meta(frame: Win): HTMLElement | null {
  return frame.document.querySelector('.hero-meta');
}

test('the line of the popup sits in the head line, after the weight and the size', async (t) => {
  const frame = await loadFrame(t, 'ItemDetailPopup');
  const root = rootWindow(t, { ItemDetailPopup: frame });
  meta(frame)!.textContent = '0.10kg · 1×1';

  const answer = runPageJs(root, "window.__itemtotals.popup('Total: 147')");

  assert.equal(answer, 'shown');
  assert.equal(meta(frame)!.textContent, '0.10kg · 1×1 · Total: 147');
});

test('a second open writes one span and the new number, not a second span', async (t) => {
  const frame = await loadFrame(t, 'ItemDetailPopup');
  const root = rootWindow(t, { ItemDetailPopup: frame });
  meta(frame)!.textContent = '0.10kg · 1×1';

  runPageJs(root, "window.__itemtotals.popup('Total: 147')");
  runPageJs(root, "window.__itemtotals.popup('Total: 146')");

  assert.equal(meta(frame)!.querySelectorAll('.it-popup').length, 1);
  assert.equal(meta(frame)!.textContent, '0.10kg · 1×1 · Total: 146');
});

// The web page owns the text of the head line and builds it from three fields of its own, as the one text child of
// the node, so each draw of the popup sets textContent and drops the span of the mod. The popup is modal, so no send
// of C# comes in between: the page itself must write the line again.
test('a redraw of the head line puts the line back with no new send', async (t) => {
  const frame = await loadFrame(t, 'ItemDetailPopup');
  const root = rootWindow(t, { ItemDetailPopup: frame });
  meta(frame)!.textContent = '0.10kg · 1×1';
  runPageJs(root, "window.__itemtotals.popup('Total: 147')");

  meta(frame)!.textContent = '0.20kg · 2×1';
  await posts();

  assert.equal(meta(frame)!.querySelectorAll('.it-popup').length, 1);
  assert.equal(meta(frame)!.textContent, '0.20kg · 2×1 · Total: 147');
});

// C# sends the number of the item of the popup on each open. A second open must not show the number of the first,
// which the write-again of the redraw would keep alive.
test('the line that was taken out stays out over a redraw', async (t) => {
  const frame = await loadFrame(t, 'ItemDetailPopup');
  const root = rootWindow(t, { ItemDetailPopup: frame });
  meta(frame)!.textContent = '0.10kg · 1×1';
  runPageJs(root, "window.__itemtotals.popup('Total: 147')");
  runPageJs(root, "window.__itemtotals.popup('')");

  meta(frame)!.textContent = '0.20kg · 2×1';
  await posts();

  assert.equal(meta(frame)!.textContent, '0.20kg · 2×1');
});

test('an empty text takes the line out of the head line again', async (t) => {
  const frame = await loadFrame(t, 'ItemDetailPopup');
  const root = rootWindow(t, { ItemDetailPopup: frame });
  meta(frame)!.textContent = '0.10kg · 1×1';
  runPageJs(root, "window.__itemtotals.popup('Total: 147')");

  const answer = runPageJs(root, "window.__itemtotals.popup('')");

  assert.equal(answer, 'cleared');
  assert.equal(meta(frame)!.textContent, '0.10kg · 1×1');
});

// C# clears the line for a popup with no total. A head line that the web page lacks is a break of the contract only
// when there is a line to write.
test('a clear of a popup with no head line is no break', async (t) => {
  const frame = await loadFrame(t, 'ItemDetailPopup');
  const root = rootWindow(t, { ItemDetailPopup: frame });
  meta(frame)!.remove();

  assert.equal(runPageJs(root, "window.__itemtotals.popup('')"), 'cleared');
  assert.equal(runPageJs(root, "window.__itemtotals.popup('Total: 147')"), 'no .hero-meta');
});

test('the popup that is not open answers no frame and changes nothing', async (t) => {
  const root = rootWindow(t, {});

  assert.equal(runPageJs(root, "window.__itemtotals.popup('Total: 147')"), 'no frame');
});

// The workbench window. Its tooltip is built in C#, which puts the line at the end of the text, and the page moves
// that line out of the text and draws it as a tooltip block with the rule of the library above it.
function tipOf(frame: Win): HTMLElement {
  return frame.document.querySelector('#tooltip') as HTMLElement;
}

function descOf(frame: Win): HTMLElement {
  return frame.document.querySelector('#tt-desc') as HTMLElement;
}

// The web page writes the whole text of the tooltip in one call, as its message handler does.
async function showTip(frame: Win, text: string): Promise<void> {
  descOf(frame).textContent = text;
  await posts();
}

test('the line of the workbench leaves the text and becomes a block', async (t) => {
  const frame = await loadFrame(t, 'ToolTable');
  const root = rootWindow(t, { ToolTable: frame });
  setData(root, data());

  await showTip(frame, 'No attribute buffs\nPermanent\nA crude metal crowbar.\nTotal: 7');

  const line = tipOf(frame).querySelector('.it-tip > .it-tip-line') as HTMLElement;
  assert.equal(line.textContent, 'Total: 7');
  assert.equal(descOf(frame).textContent, 'No attribute buffs\nPermanent\nA crude metal crowbar.');
});

// The rule above the block is the one of the library, so the workbench window reads as the windows that have a
// tooltip of their own.
test('the block of the workbench carries the rule of the library', async (t) => {
  const frame = await loadFrame(t, 'ToolTable');
  const root = rootWindow(t, { ToolTable: frame });
  setData(root, data());

  await showTip(frame, 'A crude metal crowbar.\nTotal: 7');

  const css = frame.document.getElementById('itemtotals-style')!.textContent as string;
  assert.ok(css.includes('.it-tip {'), 'the style node holds the block rules of the library');
  assert.ok(/\.it-tip \{[^}]*border-top/.test(css), 'the block has a rule above it');
});

test('a tooltip with no line of the mod is left as the game wrote it', async (t) => {
  const frame = await loadFrame(t, 'ToolTable');
  const root = rootWindow(t, { ToolTable: frame });
  setData(root, data());

  await showTip(frame, 'A crude metal crowbar.');

  assert.equal(descOf(frame).textContent, 'A crude metal crowbar.');
  assert.equal(tipOf(frame).querySelectorAll('.it-tip').length, 0);
});

// The web page writes the text again for each item the pointer reaches, so the block of the item before must go.
test('the next item draws one block with its own number', async (t) => {
  const frame = await loadFrame(t, 'ToolTable');
  const root = rootWindow(t, { ToolTable: frame });
  setData(root, data());
  await showTip(frame, 'A crude metal crowbar.\nTotal: 7');

  await showTip(frame, 'A plastic scrap.\nTotal: 147');

  assert.equal(tipOf(frame).querySelectorAll('.it-tip').length, 1);
  assert.equal(tipOf(frame).querySelector('.it-tip-line')!.textContent, 'Total: 147');
  assert.equal(descOf(frame).textContent, 'A plastic scrap.');
});

test('an item the pointer leaves takes the block with it', async (t) => {
  const frame = await loadFrame(t, 'ToolTable');
  const root = rootWindow(t, { ToolTable: frame });
  setData(root, data());
  await showTip(frame, 'A crude metal crowbar.\nTotal: 7');

  await showTip(frame, 'Another item with no line.');

  assert.equal(tipOf(frame).querySelectorAll('.it-tip').length, 0);
});

// Each hover refreshes the workbench window, and C# answers each refresh with a pass. The pass comes after the line
// left the text, so it must keep the block and not read the text as an item with no line.
test('a pass after the hover keeps the block', async (t) => {
  const frame = await loadFrame(t, 'ToolTable');
  const root = rootWindow(t, { ToolTable: frame });
  setData(root, data());
  await showTip(frame, 'A crude metal crowbar.\nTotal: 7');

  runPageJs(root, 'window.__itemtotals.apply()');
  await posts();

  assert.equal(tipOf(frame).querySelectorAll('.it-tip').length, 1);
  assert.equal(tipOf(frame).querySelector('.it-tip-line')!.textContent, 'Total: 7');
  assert.equal(descOf(frame).textContent, 'A crude metal crowbar.');
});

// The web page can write the same text again for the same item. The line comes back with it, so the block stays one.
test('the same text written again gives one block', async (t) => {
  const frame = await loadFrame(t, 'ToolTable');
  const root = rootWindow(t, { ToolTable: frame });
  setData(root, data());
  await showTip(frame, 'A crude metal crowbar.\nTotal: 7');

  await showTip(frame, 'A crude metal crowbar.\nTotal: 7');
  runPageJs(root, 'window.__itemtotals.apply()');
  await posts();

  assert.equal(tipOf(frame).querySelectorAll('.it-tip').length, 1);
  assert.equal(descOf(frame).textContent, 'A crude metal crowbar.');
});

// C# gives no line for an item it cannot count. The text of such an item can equal the text that this module left
// for the item before, and it still is a new write of the web page, so the block of the item before goes.
test('an item with no line and the same text takes the block away', async (t) => {
  const frame = await loadFrame(t, 'ToolTable');
  const root = rootWindow(t, { ToolTable: frame });
  setData(root, data());
  await showTip(frame, 'A crude metal crowbar.' + String.fromCharCode(10) + 'Total: 7');

  await showTip(frame, 'A crude metal crowbar.');

  assert.equal(tipOf(frame).querySelectorAll('.it-tip').length, 0);
  assert.equal(descOf(frame).textContent, 'A crude metal crowbar.');
});
