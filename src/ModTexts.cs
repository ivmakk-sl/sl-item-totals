using System;
using System.Collections.Generic;
using GameCore.HotUpdate;
using SlShared.I18n;

namespace ItemTotals
{
    // The mod texts (src/i18n/) through the i18n library: this adapter reads the display language and writes the
    // warnings of the library to the log. No other code of the mod reads the language.
    //
    // The mod names no text key. The library puts the game's text of a text key above the text of the i18n file, and
    // the game's own word for this number is "Owned" (WebUI_PlantPanel_11, Chinese 持有), while the line shows
    // "Total". So no game text reaches these texts, and the label is the mod's own in both languages.
    internal static class ModTexts
    {
        // The name of the one mod text: the label of the line, with the item total as the placeholder {n}.
        internal const string TotalLine = "total";

        private const int English = 1;

        private static readonly I18nTexts texts = I18nTexts.Load(typeof(ModTexts).Assembly, "ItemTotals");
        private static readonly HashSet<string> loggedLanguages = new HashSet<string>();
        private static int warningsWritten;

        // The LanguageType number of the display language (Chinese = 0, English = 1), English before the config loads.
        internal static int Language()
        {
            try
            {
                var cache = ConfigManager.Instance?.customCache;
                return cache != null ? (int)cache.LanguageType : English;
            }
            catch (Exception) { return English; }
        }

        // The mod texts in the display language. The language can change while the game runs, so the texts are read
        // again at each push.
        internal static I18nTextSet Current()
        {
            int language = Language();
            var set = texts.For(language, null, ((LanguageType)language).ToString());
            if (Plugin.Verbose.Value && loggedLanguages.Add(set.Language))
                Plugin.Log.LogDebug("Item Totals texts: language=" + set.Language + " " + TotalLine + "=\"" + set[TotalLine] + "\"");
            WriteWarnings();
            return set;
        }

        private static void WriteWarnings()
        {
            var warnings = texts.Warnings;
            while (warningsWritten < warnings.Count) Plugin.Log.LogWarning("Item Totals: " + warnings[warningsWritten++]);
        }
    }
}
