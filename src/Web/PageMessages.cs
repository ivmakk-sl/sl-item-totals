using System;
using System.Collections.Generic;
using HarmonyLib;
using Vuplex.WebView.Internal;

namespace ItemTotals.Web
{
    // The messages of the page script (window.vuplex.postMessage('slmod|itemtotals|<text>')). The Prefix takes only
    // the messages with the prefix of this mod, so the game's handler and the other mods never get them, and lets
    // each other message through.
    //
    // The texts: "no script" and "frames <n> version <v>" go to the push schedule, and each other text is a status
    // that logs one warning, which is how a missing part of a web page reaches the log once.
    [HarmonyPatch(typeof(BaseWebView), "HandleMessageEmitted")]
    internal static class PageMessages
    {
        private static readonly HashSet<string> failed = new HashSet<string>();

        private static bool Prefix(string serializedMessage)
        {
            if (serializedMessage == null || !serializedMessage.StartsWith(PageJson.MessagePrefix, StringComparison.Ordinal))
                return true;
            try
            {
                string text = serializedMessage.Substring(PageJson.MessagePrefix.Length);
                Plugin.Debug("page message: " + text);
                PageTick.OnMessage(text);
            }
            catch (Exception e)
            {
                if (failed.Add(e.Message)) Plugin.Log.LogWarning("Item Totals message failed: " + e);
            }
            return false;
        }
    }
}
