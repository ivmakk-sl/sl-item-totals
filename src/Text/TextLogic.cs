using System;

namespace ItemTotals.Text
{
    /// <summary>
    /// The line appended to a text that the game built. The workbench window builds its text in C# and not in a web
    /// page, so it gets the line here and not through the tooltip library.
    ///
    /// Game-free on purpose: it takes the text, the line of the display language, and the item total, and gives the
    /// text to show.
    /// </summary>
    public static class TextLogic
    {
        private const string Placeholder = "{n}";

        // One line break, so the line is the last line of the text. The page script of the mod moves it out of that
        // text again and draws it as a tooltip block with the rule of the library above it, as in each window that
        // has a tooltip of its own (src/Web/page/toolTable.ts). This text is what the player sees while the page
        // script has not reached the frame of the window yet, so it carries no rule of characters: the two would
        // show one after the other for a frame.
        private const string Separator = "\n";

        /// <summary>
        /// The text of the game with the line below it. The text of the game is never changed, only followed.
        ///
        /// An empty line text gives the text of the game as it is, so a mod text that did not resolve cannot take
        /// the description away from the player. An item total of 0 is a real answer and shows.
        /// </summary>
        public static string Append(string text, string line, int total)
        {
            if (string.IsNullOrEmpty(line)) return text;
            string filled = Fill(line, total);
            if (string.IsNullOrEmpty(text)) return filled;

            // The reducer of a window can run again for the same item, so the text can already carry the line of an
            // earlier run. The old one goes, which also keeps the number fresh when the item total moved.
            string body = WithoutOldLine(TrimEnd(text), line);
            return body.Length == 0 ? filled : body + Separator + filled;
        }

        private static string Fill(string line, int total)
        {
            return line.Replace(Placeholder, total.ToString());
        }

        // The text without a last line that this code wrote before. The line is recognised by the separator and the
        // part of the text before the placeholder, which a translator owns and the number does not change.
        private static string WithoutOldLine(string text, string line)
        {
            string head = Head(line);
            if (head.Length == 0) return text;
            int at = text.LastIndexOf(Separator + head, StringComparison.Ordinal);
            if (at < 0) return text.StartsWith(head, StringComparison.Ordinal) ? "" : text;
            return TrimEnd(text.Substring(0, at));
        }

        // The part of the line before the placeholder, for example "Total: ". The whole line when it has none, so a
        // line text without a placeholder is still recognised on the next run.
        private static string Head(string line)
        {
            int at = line.IndexOf(Placeholder, StringComparison.Ordinal);
            return at < 0 ? line : line.Substring(0, at);
        }

        private static string TrimEnd(string text)
        {
            return text.TrimEnd('\n', '\r');
        }
    }
}
