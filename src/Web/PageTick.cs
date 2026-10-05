using System;
using System.Collections.Generic;
using GameCore.HotUpdate.ReduxUI;
using HarmonyLib;
using ItemTotals.Text;
using ItemTotals.Totals;
using UnityEngine;

namespace ItemTotals.Web
{
    // The per-frame hook of the web UI layer, which runs the schedule of the sends. It returns at once while nothing
    // is pending, so a frame with no item change and no window refresh costs one bool check.
    //
    // WebUILayer.OnUpdate has no pause check, which is what this needs: a window of the game pauses the world.
    [HarmonyPatch(typeof(WebUILayer), "OnUpdate")]
    internal static class PageTick
    {
        private static readonly PushSchedule schedule = new PushSchedule();
        private static readonly HashSet<string> warned = new HashSet<string>();
        private static int lastVersion;

        /// <summary>A message of the page script, with the prefix of the mod taken off.</summary>
        internal static void OnMessage(string text)
        {
            float now = Time.realtimeSinceStartup;
            // The page drew the line of the item detail popup, which ends the sends of that line.
            if (text == PopupShown)
            {
                ItemDetailText.Schedule.Shown();
                return;
            }
            if (schedule.ShouldWarn(text)) Plugin.Log.LogWarning("Item Totals page: " + text);
            schedule.OnMessage(text, now);
        }

        /// <summary>A listed window got fresh data from the game, so the next tick applies the line to its frame.</summary>
        internal static void RequestPage(string page)
        {
            schedule.RequestPage(page, Time.realtimeSinceStartup);
        }

        /// <summary>The message with which the page reports that it drew the line of the item detail popup.</summary>
        internal const string PopupShown = "popup shown";

        private static void Postfix()
        {
            try
            {
                float now = Time.realtimeSinceStartup;
                // The item detail popup is driven on its own: it is no listed window, and its frame is built after
                // the reducer that opened it ran, so its line is sent again until the page reports that it drew it.
                string popup = ItemDetailText.Schedule.Tick(now);
                if (popup != null) PageScript.Run(PageJson.PopupCommand(popup));
                // An item change asks for a new calculation, and a new version asks for a send.
                if (Store.Totals.IsDirty)
                {
                    Store.Refresh();
                    if (Store.Totals.Version != lastVersion)
                    {
                        lastVersion = Store.Totals.Version;
                        schedule.RequestData(lastVersion, now);
                    }
                }
                // Nothing goes out before a save is loaded: the item pool cannot be read yet, and the label of the
                // line resolves to nothing because the game has not loaded its text either.
                if (Store.Totals.Current.Failed) return;
                if (!schedule.Pending(now)) return;
                var step = schedule.Tick(now, Store.Totals.Version, () =>
                    PageJson.Build(Store.Totals.Current, ModTexts.Current()[ModTexts.TotalLine], Store.Totals.Version));
                if (step.Kind == PushSchedule.Kind.None) return;
                PageScript.Send(step);
            }
            catch (Exception e)
            {
                if (warned.Add(e.Message)) Plugin.Log.LogWarning("Item Totals tick failed: " + e);
            }
        }
    }

    // The moment a window of the game got fresh data. WebUILayer.SendMessageToPage is where the layer flushes the
    // message of each page it marked dirty, so it fires once for each web page that got fresh data in a frame, and it
    // gives the page id, the proof that the page is ready, and the refresh of that page in one place.
    //
    // Not WebUILayer.ShowPage: it has no caller in the native code of the game, so it is inlined or called virtually,
    // and a patch would attach with no error and never run.
    [HarmonyPatch(typeof(WebUILayer), "SendMessageToPage")]
    internal static class PageTickOnSendMessageToPage
    {
        private static void Postfix(string targetPageId)
        {
            // The page id of the game is not the folder name of the web page for every window: the storage window is
            // Backpack, the trade window Trade, and the shop window Shop.
            // The workbench window is in no page table: its tooltip is built in C#, and the page script only moves
            // the line of that text into a block of its own. Its refresh still asks for a pass, which is what puts
            // the watch of the mod into its frame once the window is open.
            string page = targetPageId == PageJson.ToolTablePageId
                ? PageJson.ToolTablePageId
                : PageJson.NameOfPageId(targetPageId);
            if (page == null) return;
            Plugin.Debug("page refresh: " + targetPageId + " -> " + page);
            PageTick.RequestPage(page);
        }
    }
}
