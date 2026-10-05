using System;

namespace ItemTotals.Text
{
    /// <summary>
    /// The hover message of the workbench window (JsonMsg_ItemHoverData), which carries the item id and whether the
    /// pointer entered or left. The shape is two fields, so the mod reads them rather than pulling in a JSON reader.
    ///
    /// Game-free on purpose: it takes the text and gives the two values.
    /// </summary>
    public static class HoverJson
    {
        /// <summary>The item id of the message, or 0 when the text carries none.</summary>
        public static long ItemId(string json)
        {
            return Number(json, "itemId");
        }

        /// <summary>
        /// True when the pointer entered the item. A message with no flag counts as an enter, so a game update that
        /// drops the field does not turn the line off.
        /// </summary>
        public static bool IsEnter(string json)
        {
            int at = ValueAt(json, "isEnter");
            if (at < 0) return true;
            return string.CompareOrdinal(json, at, "false", 0, 5) != 0;
        }

        private static long Number(string json, string name)
        {
            int at = ValueAt(json, name);
            if (at < 0) return 0;
            long value = 0;
            bool any = false;
            while (at < json.Length && json[at] >= '0' && json[at] <= '9')
            {
                value = value * 10 + (json[at] - '0');
                any = true;
                at++;
            }
            return any ? value : 0;
        }

        // The place of the value of a field, past its name, its colon, and any space. -1 when the field is not there.
        private static int ValueAt(string json, string name)
        {
            if (string.IsNullOrEmpty(json)) return -1;
            int at = json.IndexOf("\"" + name + "\"", StringComparison.Ordinal);
            if (at < 0) return -1;
            at += name.Length + 2;
            while (at < json.Length && (json[at] == ' ' || json[at] == ':')) at++;
            return at;
        }
    }
}
