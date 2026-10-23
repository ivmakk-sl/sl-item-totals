using GameCore.HotUpdate;
using ItemTotals.Text;

namespace ItemTotals
{
    // The label of the line: the game's own text of its count at home (GameLabel.TextKey), read through the text
    // lookup of the game, which follows the display language and each language pack that the game reads. The mod has
    // no text of its own. No other code of the mod reads the language.
    internal static class ModTexts
    {
        private static string loggedText;
        private static bool warned;

        // The pattern of the line with the item total as {n}, or null when the game has no text of the key (a game
        // before 1.1.19611), which shows no line. The language can change while the game runs, so the text is read
        // again at each push.
        internal static string TotalLine()
        {
            string gameText = ConstantTextTools.ToConstantTextOrEmpty(GameLabel.TextKey);
            string line = GameLabel.LinePattern(gameText);
            if (line == null)
            {
                if (!warned)
                {
                    warned = true;
                    Plugin.Log.LogWarning("Item Totals: the game has no text for the text key " + GameLabel.TextKey + ": the line does not show.");
                }
                return null;
            }
            if (Plugin.Verbose.Value && loggedText != gameText)
            {
                loggedText = gameText;
                Plugin.Log.LogDebug("Item Totals label: " + GameLabel.TextKey + "=\"" + gameText + "\"");
            }
            return line;
        }
    }
}
