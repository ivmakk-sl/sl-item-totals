using ItemTotals.Totals;
using Xunit;

namespace ItemTotals.Tests;

// The store of the item totals: the dirty bit that an item change sets, one calculation for a frame, and the data
// version that tells the push whether the web page already holds this data.
public class TotalsStoreTests
{
    private static TotalSet Set(params (long itemId, int configId, int count)[] items)
    {
        var pool = new PoolItem[items.Length];
        for (int i = 0; i < items.Length; i++) pool[i] = new PoolItem(items[i].itemId, items[i].configId, items[i].count);
        return TotalsLogic.Build(pool);
    }

    [Fact]
    public void CalculatesOnTheFirstRefreshBecauseItHoldsNothingYet()
    {
        var store = new TotalsStore();
        int reads = 0;

        Assert.True(store.Refresh(() => { reads++; return Set((1, 101, 3)); }));

        Assert.Equal(1, reads);
        Assert.Equal(3, store.Current.TotalOf(101));
    }

    [Fact]
    public void DoesNotCalculateAgainWhileNoItemChanged()
    {
        var store = new TotalsStore();
        int reads = 0;
        store.Refresh(() => { reads++; return Set((1, 101, 3)); });

        Assert.False(store.Refresh(() => { reads++; return Set((1, 101, 3)); }));
        Assert.False(store.Refresh(() => { reads++; return Set((1, 101, 3)); }));

        Assert.Equal(1, reads);
    }

    [Fact]
    public void CalculatesAgainAfterAnItemChanged()
    {
        var store = new TotalsStore();
        store.Refresh(() => Set((1, 101, 3)));

        store.MarkDirty();
        Assert.True(store.Refresh(() => Set((1, 101, 2))));

        Assert.Equal(2, store.Current.TotalOf(101));
    }

    [Fact]
    public void CalculatesOnceForManyChangesInOneFrame()
    {
        var store = new TotalsStore();
        int reads = 0;
        store.Refresh(() => { reads++; return Set((1, 101, 3)); });

        store.MarkDirty();
        store.MarkDirty();
        store.MarkDirty();
        store.Refresh(() => { reads++; return Set((1, 101, 9)); });

        Assert.Equal(2, reads);
    }

    [Fact]
    public void RaisesTheVersionOnlyWhenTheTotalsDiffer()
    {
        var store = new TotalsStore();
        store.Refresh(() => Set((1, 101, 3)));
        int first = store.Version;

        // An item changed, but the totals read the same: the web page needs no new data.
        store.MarkDirty();
        store.Refresh(() => Set((1, 101, 3)));
        Assert.Equal(first, store.Version);

        store.MarkDirty();
        store.Refresh(() => Set((1, 101, 4)));
        Assert.NotEqual(first, store.Version);
    }

    [Fact]
    public void TheVersionOfTheFirstCalculationIsNotTheVersionOfNoDataYet()
    {
        var store = new TotalsStore();
        int before = store.Version;

        store.Refresh(() => Set((1, 101, 3)));

        Assert.NotEqual(before, store.Version);
    }

    [Fact]
    public void ReadsTheTotalsAtOnceWhenTheBitIsSet()
    {
        // The workbench window and the item detail popup write their text in C# and do not wait for the tick, so a
        // read while the bit is set has to calculate first or the text would carry a stale number.
        var store = new TotalsStore();
        store.Refresh(() => Set((1, 101, 3)));
        store.MarkDirty();

        var totals = store.Read(() => Set((1, 101, 1)));

        Assert.Equal(1, totals.TotalOf(101));
        Assert.False(store.IsDirty);
    }

    [Fact]
    public void AReadWithACleanBitDoesNotCalculate()
    {
        var store = new TotalsStore();
        int reads = 0;
        store.Refresh(() => { reads++; return Set((1, 101, 3)); });

        var totals = store.Read(() => { reads++; return Set((1, 101, 9)); });

        Assert.Equal(1, reads);
        Assert.Equal(3, totals.TotalOf(101));
    }

    [Fact]
    public void TheBitIsSetBeforeTheFirstCalculation()
    {
        Assert.True(new TotalsStore().IsDirty);
    }

    [Fact]
    public void AFailedCalculationLeavesTheBitSetSoTheNextTickTriesAgain()
    {
        var store = new TotalsStore();

        Assert.True(store.Refresh(() => TotalSet.Failure));

        Assert.True(store.Current.Failed);
        Assert.True(store.IsDirty);
    }

    [Fact]
    public void TheStoreHoldsAnEmptySetBeforeTheFirstCalculation()
    {
        var store = new TotalsStore();

        Assert.Empty(store.Current.ByConfigId);
        Assert.False(store.Current.Failed);
    }
}
