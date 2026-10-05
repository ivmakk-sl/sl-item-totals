using GameCore.HotUpdate.Battle.Logic;

namespace ItemTotals.Totals
{
    /// <summary>
    /// The one store of the item totals of the mod, read by the tick of the web pages and by the two windows that
    /// build their text in C#. Neither side keeps a rule of its own.
    /// </summary>
    internal static class Store
    {
        internal static readonly TotalsStore Totals = new TotalsStore();

        /// <summary>An item changed. Names the hook that fired and the item, for the log of a checkpoint.</summary>
        internal static void MarkDirty(string why, ItemData item, long ownerId, long itemId = 0)
        {
            Totals.MarkDirty();
            if (!Plugin.Verbose.Value) return;
            int configId = item == null ? 0 : item.ItemConfigId;
            long id = item != null ? item.InstanceId : itemId;
            Plugin.Debug("invalidate: " + why + " item=" + id + " config=" + configId + " owner=" + ownerId
                         + " frame=" + UnityEngine.Time.frameCount);
        }

        /// <summary>The item totals now, calculated again first when an item changed.</summary>
        internal static TotalSet Read()
        {
            return Totals.Read(Pool.Read);
        }

        /// <summary>Calculates the item totals again when an item changed. True when it calculated.</summary>
        internal static bool Refresh()
        {
            return Totals.Refresh(Pool.Read);
        }
    }
}
