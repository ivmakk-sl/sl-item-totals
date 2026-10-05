using System;
using System.Collections.Generic;
using System.Text;
using SlShared.Json;
using ItemTotals.Totals;

namespace ItemTotals.Web
{
    // The JSON text that the plugin sends to the page script, built by hand: netstandard2.1 has no
    // System.Text.Json, and the game's own Newtonsoft.Json is not usable from mod code. No game or BepInEx type
    // here. The page side of this shape is PageData in src/Web/page/types.ts.
    public static class PageJson
    {
        /// <summary>One window that shows the line: the folder name of its web page, and the page id the game uses.</summary>
        public readonly struct Page
        {
            // The folder name of the web page, which the page script uses to find the frame of the window.
            public readonly string Name;
            // The name the game itself calls the page, which is the argument of WebUILayer.SendMessageToPage. It is
            // not always the folder name: the storage window is Backpack, the trade window Trade, the shop Shop.
            public readonly string Id;

            public Page(string name, string id)
            {
                Name = name; Id = id;
            }
        }

        // The windows that show the line. The same table is the page table of the tooltip lines library
        // (src/Shared/tooltip-lines/web/pages.ts), and a test asserts that the two name the same pages with the same
        // page ids, so the two sides cannot drift.
        public static readonly Page[] Pages =
        {
            new Page("BackpackUI", "Backpack"),
            new Page("TradeUI", "Trade"),
            new Page("ShopUI", "Shop"),
            new Page("Cooking", "Cooking"),
            new Page("MaterialRack", "MaterialRack"),
            new Page("AidDepot", "AidDepot"),
            new Page("BrewPanel", "BrewPanel"),
            new Page("DailyFert", "DailyFert"),
            new Page("DroneHub", "DroneHub"),
            new Page("DyePanel", "DyePanel"),
            new Page("RatCage", "RatCage"),
            new Page("Compressor", "Compressor"),
            new Page("Shredder", "Shredder"),
            new Page("GreenhouseBuild", "GreenhouseBuild"),
        };

        /// <summary>
        /// The workbench window. It is in no page table, because its tooltip is built in C# and takes no block
        /// through the tooltip lines library, but its refresh asks for a pass so the page script reaches its frame.
        /// </summary>
        public const string ToolTablePageId = "ToolTable";

        /// <summary>The window of a page id that the game gave, or null when no listed window has that id.</summary>
        public static string NameOfPageId(string pageId)
        {
            if (pageId == null) return null;
            for (int i = 0; i < Pages.Length; i++)
                if (string.Equals(Pages[i].Id, pageId, StringComparison.Ordinal)) return Pages[i].Name;
            return null;
        }

        /// <summary>
        /// The data of one push: the item total of each item, the item config id of each stack, the text of the line
        /// in the display language, the web pages to install into, and the data version.
        ///
        /// The line text is a pattern with the item total as the placeholder {n}, so a translator owns the separator
        /// and the order of the word and the number. The page fills it.
        ///
        /// The second map is for the item cells of the character in the trade window and in the shop window, which
        /// the game names by the item instance id only.
        /// </summary>
        public static string Build(TotalSet totals, string line, int version)
        {
            var sb = new StringBuilder(256 + totals.ConfigIdByItemId.Count * 16);
            sb.Append("{\"v\":").Append(JsonText.Num(version));
            sb.Append(",\"line\":").AppendStr(line);
            if (totals.Failed) sb.Append(",\"failed\":true");
            sb.Append(",\"pages\":[");
            for (int i = 0; i < Pages.Length; i++)
            {
                if (i > 0) sb.Append(',');
                sb.AppendStr(Pages[i].Name);
            }
            sb.Append("],\"totals\":{");
            bool first = true;
            foreach (var pair in totals.ByConfigId)
            {
                if (!first) sb.Append(',');
                first = false;
                sb.AppendStr(JsonText.Num(pair.Key)).Append(':').Append(JsonText.Num(pair.Value));
            }
            sb.Append("},\"items\":{");
            first = true;
            foreach (var pair in totals.ConfigIdByItemId)
            {
                if (!first) sb.Append(',');
                first = false;
                sb.AppendStr(JsonText.Num(pair.Key)).Append(':').Append(JsonText.Num(pair.Value));
            }
            sb.Append("}}");
            return sb.ToString();
        }

        // The prefix of each message from the page script to the plugin (window.vuplex.postMessage).
        public const string MessagePrefix = "slmod|itemtotals|";

        // Each command has no result callback: the page answers only by message, and a root page with no script
        // posts the no-script message.
        private const string NoScriptMessage = "window.vuplex.postMessage('" + MessagePrefix + PushSchedule.NoScript + "')";

        /// <summary>The push of the data when the page script is already in the root page.</summary>
        public static string SetDataCommand(string json) =>
            "window.__itemtotals?window.__itemtotals.setData(" + json + "):" + NoScriptMessage;

        /// <summary>One pass with the stored data, which installs the line into a frame that just opened.</summary>
        public const string ApplyCommand = "window.__itemtotals?window.__itemtotals.apply():" + NoScriptMessage;

        /// <summary>
        /// The line of the item detail popup, which the page writes into the head line of the popup. An empty text
        /// takes the line out again. The popup is driven on its own and not by the pass over the listed windows.
        /// </summary>
        public static string PopupCommand(string line)
        {
            var sb = new System.Text.StringBuilder("window.__itemtotals&&window.__itemtotals.popup(");
            JsonText.AppendStr(sb, line ?? "");
            return sb.Append(')').ToString();
        }

        /// <summary>The page script, then the push of the data. Sent only after a no-script message.</summary>
        public static string SetDataWithScriptCommand(string script, string json) =>
            script + ";window.__itemtotals.setData(" + json + ");";
    }
}
