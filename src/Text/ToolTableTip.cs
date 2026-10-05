using System;
using System.Collections.Generic;
using GameCore.HotUpdate.ReduxUI;
using HarmonyLib;
using ItemTotals.Totals;

namespace ItemTotals.Text
{
    // The tooltip of the workbench window. It builds its text in C# and not in a web page, so the line goes on the
    // text of the state and not through the tooltip library.
    //
    // Reducer_Web_ToolTable.RA_ItemHover is a private static method that gives the new state, and a patch declares
    // __result to read it. This is the shape that Project Cook already uses on Reducer_Web_Cooking.RA_Open, so a
    // reducer action is safe to patch this way; an instance method of ItemManager that gives a value is not.
    //
    // The totals are read through Store.Read, which calculates again when an item changed, so the popup of a hover
    // right after a move shows the new number and does not wait for the next tick.
    [HarmonyPatch(typeof(Reducer_Web_ToolTable), "RA_ItemHover")]
    internal static class ToolTableTip
    {
        private static readonly HashSet<string> warned = new HashSet<string>();

        private static void Postfix(Ac_ToolTable_ItemHover ac, State_Web_ToolTable __result)
        {
            try
            {
                if (ac == null || __result == null) return;
                string json = ac.JsonData;
                // The pointer leaving an item sends the same action, and then the tooltip has nothing to say.
                if (!HoverJson.IsEnter(json)) return;
                long itemId = HoverJson.ItemId(json);
                if (itemId == 0) return;

                var totals = Store.Read();
                if (totals.Failed) return;
                int configId;
                if (!totals.ConfigIdByItemId.TryGetValue(itemId, out configId)) return;

                var desc = __result.TipsDescText;
                if (desc == null) return;
                int total = totals.TotalOf(configId);
                string before = desc.Value;
                desc.Value = TextLogic.Append(before, ModTexts.Current()[ModTexts.TotalLine], total);
                Plugin.Debug("workbench tip: item=" + itemId + " config=" + configId + " total=" + total);
            }
            catch (Exception e)
            {
                if (warned.Add(e.Message)) Plugin.Log.LogWarning("Item Totals workbench tip failed: " + e);
            }
        }
    }
}
