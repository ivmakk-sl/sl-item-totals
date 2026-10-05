// The shape that C# sends. Its other side is PageJson.Build in src/Web/PageJson.cs.
export interface PageData {
  // The data version. The page sends it back, so C# knows which data the page holds.
  v: number;
  // The text of the line in the display language of the game, with the item total as the placeholder {n}. A
  // translator owns the separator and the order of the word and the number, so the page fills the placeholder and
  // never builds the text itself.
  line: string;
  // True when the item pool of the game could not be read. The mod then shows no line at all.
  failed?: boolean;
  // The web pages to install the line into.
  pages: string[];
  // The item total of each item, keyed by the item config id.
  totals: Record<string, number>;
  // The item config id of each stack of the pool, keyed by the item instance id. The item cells of the character in
  // the trade window and in the shop window carry the item instance id only.
  items: Record<string, number>;
}

declare global {
  interface Window {
    __itemtotals?: {
      setData: (data: PageData) => string;
      apply: () => string;
      // The line of the item detail popup. An empty text takes it out again.
      popup: (text: string) => string;
    };
    vuplex?: { postMessage: (text: string) => void };
  }
}
