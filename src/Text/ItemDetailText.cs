using System;
using System.Collections.Generic;
using GameCore.HotUpdate.ReduxUI;
using HarmonyLib;
using ItemTotals.Totals;
using ItemTotals.Web;

namespace ItemTotals.Text
{
    // The item detail popup. The line goes into the head line of the popup, which the web page owns, so this code
    // sends the number and the page script writes it.
    //
    // Both actions are patched: RA_OpenUI opens the popup and RA_RefreshUI draws it again with another item. The
    // state carries the item config id, so no item id has to be read back.
    //
    // The popup is modal, so nothing the player does can change the number while it is open. Each draw of the web
    // page drops the span of the mod, and the page script writes it again by itself rather than waiting for a send
    // from here; see src/Web/page/popup.ts.
    //
    // The totals are read through Store.Read, which calculates again when an item changed, so a popup opened right
    // after a move shows the new number and does not wait for the next tick.
    internal static class ItemDetailText
    {
        private static readonly HashSet<string> warned = new HashSet<string>();

        /// <summary>The sends of the line, which the per-frame tick drives.</summary>
        internal static readonly PopupSchedule Schedule = new PopupSchedule();

        internal static void Append(State_Web_ItemDetailPopup state, string where)
        {
            try
            {
                if (state == null) return;
                // A popup with no line takes away the line of an earlier open, which the page script would otherwise
                // write again into this popup.
                int configId = state.ItemConfigId;
                if (configId == 0)
                {
                    Schedule.Open("", UnityEngine.Time.realtimeSinceStartup);
                    return;
                }

                var totals = Store.Read();
                if (totals.Failed)
                {
                    Schedule.Open("", UnityEngine.Time.realtimeSinceStartup);
                    return;
                }

                int total = totals.TotalOf(configId);
                // The line goes into the head line of the popup, beside the weight and the size, which the page of
                // the popup owns. So the mod sends the text and the page writes it: no state field of the popup
                // reaches that place, because the page builds the head line from three fields of its own.
                //
                // The send goes through the schedule and not straight out: this reducer runs before the root page
                // builds the frame of the popup, so a send from here alone finds no frame.
                string line = SlShared.I18n.I18nTexts.Fill(ModTexts.Current()[ModTexts.TotalLine], ("n", total));
                Schedule.Open(line, UnityEngine.Time.realtimeSinceStartup);
                Plugin.Debug("item detail " + where + ": config=" + configId + " total=" + total);
            }
            catch (Exception e)
            {
                if (warned.Add(e.Message)) Plugin.Log.LogWarning("Item Totals item detail failed: " + e);
            }
        }
    }

    [HarmonyPatch(typeof(Reducer_Web_ItemDetailPopup), "RA_OpenUI")]
    internal static class ItemDetailTextOnOpen
    {
        private static void Postfix(State_Web_ItemDetailPopup __result) => ItemDetailText.Append(__result, "open");
    }

    [HarmonyPatch(typeof(Reducer_Web_ItemDetailPopup), "RA_RefreshUI")]
    internal static class ItemDetailTextOnRefresh
    {
        private static void Postfix(State_Web_ItemDetailPopup __result) => ItemDetailText.Append(__result, "refresh");
    }
}
