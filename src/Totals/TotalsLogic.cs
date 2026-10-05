using System.Collections.Generic;

namespace ItemTotals.Totals
{
    /// <summary>One stack of the item pool, as plain data: which stack it is, which item, and how many.</summary>
    public readonly struct PoolItem
    {
        public readonly long ItemId;
        public readonly int ConfigId;
        public readonly int Count;

        public PoolItem(long itemId, int configId, int count)
        {
            ItemId = itemId;
            ConfigId = configId;
            Count = count;
        }
    }

    /// <summary>
    /// The item totals of one calculation: the count of each item in the backpack and in all home storage together,
    /// and the item config id of each stack. The second map is for the item cells of the character in the trade
    /// window and in the shop window, which the game names by the item instance id only.
    /// </summary>
    public sealed class TotalSet
    {
        /// <summary>The answer when the item pool of the game cannot be read: no total, and the failure stated.</summary>
        public static readonly TotalSet Failure = new TotalSet(new Dictionary<int, int>(), new Dictionary<long, int>(), true);

        public static readonly TotalSet Empty = new TotalSet(new Dictionary<int, int>(), new Dictionary<long, int>(), false);

        private readonly Dictionary<int, int> _byConfigId;
        private readonly Dictionary<long, int> _configIdByItemId;

        internal TotalSet(Dictionary<int, int> byConfigId, Dictionary<long, int> configIdByItemId, bool failed)
        {
            _byConfigId = byConfigId;
            _configIdByItemId = configIdByItemId;
            Failed = failed;
        }

        public bool Failed { get; }

        public IReadOnlyDictionary<int, int> ByConfigId => _byConfigId;

        public IReadOnlyDictionary<long, int> ConfigIdByItemId => _configIdByItemId;

        /// <summary>The item total of one item. 0 is a real answer: the character holds none of it.</summary>
        public int TotalOf(int configId)
        {
            int total;
            return _byConfigId.TryGetValue(configId, out total) ? total : 0;
        }

        /// <summary>True when the other set holds the same totals and the same stacks, so no push is needed.</summary>
        public bool SameAs(TotalSet other)
        {
            if (other == null || other.Failed != Failed) return false;
            if (other._byConfigId.Count != _byConfigId.Count) return false;
            if (other._configIdByItemId.Count != _configIdByItemId.Count) return false;
            foreach (var pair in _byConfigId)
            {
                int theirs;
                if (!other._byConfigId.TryGetValue(pair.Key, out theirs) || theirs != pair.Value) return false;
            }
            foreach (var pair in _configIdByItemId)
            {
                int theirs;
                if (!other._configIdByItemId.TryGetValue(pair.Key, out theirs) || theirs != pair.Value) return false;
            }
            return true;
        }
    }

    /// <summary>The count of the stacks of the item pool, with no game type in it.</summary>
    public static class TotalsLogic
    {
        /// <summary>
        /// The item totals of the stacks of the pool. A stack that the game names twice counts once, because the
        /// place list of the game can hold one place more than once. A stack with no item and a stack with no count
        /// are left out.
        /// </summary>
        public static TotalSet Build(IEnumerable<PoolItem> items)
        {
            var byConfigId = new Dictionary<int, int>();
            var configIdByItemId = new Dictionary<long, int>();
            if (items == null) return new TotalSet(byConfigId, configIdByItemId, false);
            foreach (var item in items)
            {
                if (item.ConfigId <= 0 || item.Count <= 0) continue;
                if (configIdByItemId.ContainsKey(item.ItemId)) continue;
                configIdByItemId[item.ItemId] = item.ConfigId;
                int total;
                byConfigId[item.ConfigId] = byConfigId.TryGetValue(item.ConfigId, out total) ? total + item.Count : item.Count;
            }
            return new TotalSet(byConfigId, configIdByItemId, false);
        }
    }
}
