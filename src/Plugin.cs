using System;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;

namespace ItemTotals
{
    [BepInPlugin(PluginGuid, "Item Totals", "1.0.1")]
    [BepInProcess("SurvivalLog.exe")]
    public sealed class Plugin : BasePlugin
    {
        public const string PluginGuid = "com.ivmakk.survivallog.itemtotals";

        internal static new ManualLogSource Log;
        internal static Harmony Harmony;
        internal static ConfigEntry<bool> Verbose;

        public override void Load()
        {
            Log = base.Log;
            Verbose = Config.Bind(
                "General", "Verbose", false,
                "Log each item total calculation, each send to a window, and the page-script detail at Debug level. Keep off in normal play.");
            Harmony = new Harmony(PluginGuid);
            var patches = new List<Type>
            {
                typeof(ItemTotals.Totals.InvalidateOnAddToOwnerCache),
                typeof(ItemTotals.Totals.InvalidateOnIncreaseItemCount),
                typeof(ItemTotals.Totals.InvalidateOnDetachFromOwnerCaches),
                typeof(ItemTotals.Totals.InvalidateOnCostItem),
                typeof(ItemTotals.Text.ToolTableTip),
                typeof(ItemTotals.Text.ItemDetailTextOnOpen),
                typeof(ItemTotals.Text.ItemDetailTextOnRefresh),
                typeof(ItemTotals.Web.PageTick),
                typeof(ItemTotals.Web.PageTickOnSendMessageToPage),
                typeof(ItemTotals.Web.PageMessages),
            };
            // Each patch target is attached on its own, so a target missing after a game update turns
            // off only its own part.
            foreach (var type in patches)
            {
                try { Harmony.CreateClassProcessor(type).Patch(); }
                catch (Exception e) { Log.LogWarning($"patch {type.Name} failed, its target method is missing: {e.Message}"); }
            }

            Log.LogInfo("Item Totals loaded.");
        }

        internal static void Debug(string line)
        {
            if (Verbose.Value) Log.LogDebug(line);
        }
    }
}
