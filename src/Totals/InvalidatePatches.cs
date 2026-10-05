using GameCore.HotUpdate.Battle.Logic;
using HarmonyLib;

namespace ItemTotals.Totals
{
    // Every change of the per-owner item cache of the game reaches one of these four methods, and the pool read goes
    // to that same cache. So an item that enters it, leaves it, or changes its count in place cannot miss the dirty
    // bit.
    //
    // Each of the four returns void, which is the rule here and not a coincidence. A Harmony patch of a private
    // method of this game that returns a value gives the default value to the caller, also when the patch declares
    // __result and only reads it. The first build patched ItemManager.RemoveItemFromOwnerCache, which gives the
    // ItemData it took out, and ItemManager.OnMoveItem reads that item to tell a real move from a mirror that is out
    // of step. Every move then read as failed, the game logged "[bug][bag-mirror] OnMoveItem" and sent its correction
    // again about fifty times for one drag, and a player saw a delay of seconds on a drag inside a cooking station.
    // Declaring __result did not help: the second build read it as null in all but 6 of about 240 calls. So a
    // notification-only patch takes a void method, and these four cover the same events:
    //
    // - AddItemToOwnerCache: an item enters a container, so a gain and the second half of a move.
    // - IncreaseItemCount: a stack grows in place.
    // - DetachItemFromOwnerCaches: an item leaves every container. Its callers are RemoveItem, ForceRemoveItem, and
    //   CostItem, so it carries a removal and a use that takes the whole stack.
    // - CostItem: a use, whether it takes the whole stack or part of it. This is the one the player sees when a
    //   number drops after eating or crafting.
    //
    // Not ItemManager.OnSyncItem and not ItemManager.SetItemCount: neither has a caller in the native code of the
    // game, so SetItemCount is inlined into IncreaseItemCount and DecreaseItemCount, and a patch on either would attach
    // with no error and never run. Not ItemManager.OnMoveItem either: a move goes through the cache add.
    //
    // All but CostItem are private, which Harmony patches, and each one is attached on its own in Plugin.Load, so a
    // method missing after a game update turns off only its own hook and the log names it.

    [HarmonyPatch(typeof(ItemManager), "AddItemToOwnerCache")]
    internal static class InvalidateOnAddToOwnerCache
    {
        [HarmonyPostfix]
        private static void Postfix(long ownerId, ItemData item)
        {
            Store.MarkDirty("AddItemToOwnerCache", item, ownerId);
        }
    }

    [HarmonyPatch(typeof(ItemManager), "IncreaseItemCount")]
    internal static class InvalidateOnIncreaseItemCount
    {
        [HarmonyPostfix]
        private static void Postfix(ItemData item, int delta)
        {
            Store.MarkDirty("IncreaseItemCount +" + delta, item, item == null ? 0 : item.OwnerId);
        }
    }

    [HarmonyPatch(typeof(ItemManager), "DetachItemFromOwnerCaches")]
    internal static class InvalidateOnDetachFromOwnerCaches
    {
        [HarmonyPostfix]
        private static void Postfix(ItemData itemData, long expectedOwnerId)
        {
            Store.MarkDirty("DetachItemFromOwnerCaches", itemData, expectedOwnerId);
        }
    }

    [HarmonyPatch(typeof(ItemManager), "CostItem")]
    internal static class InvalidateOnCostItem
    {
        [HarmonyPostfix]
        private static void Postfix(long ownerId, long itemId, int count)
        {
            Store.MarkDirty("CostItem -" + count, null, ownerId, itemId);
        }
    }
}
