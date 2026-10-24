using SlShared.ItemPool;

namespace ItemTotals.Totals
{
    /// <summary>
    /// The one store of the item totals of the mod, read by the tick of the web pages and by the two windows that
    /// build their text in C#. Neither side keeps a rule of its own: the pool, its hooks, and its store are in the
    /// library copy of the item pool library (src/Shared/item-pool/).
    /// </summary>
    internal static class Store
    {
        internal static TotalsStore Totals => ItemPool.Store;

        /// <summary>The item totals now, calculated again first when an item changed.</summary>
        internal static TotalSet Read()
        {
            return ItemPool.Read();
        }

        /// <summary>Calculates the item totals again when an item changed. True when it calculated.</summary>
        internal static bool Refresh()
        {
            return ItemPool.Refresh();
        }
    }
}
