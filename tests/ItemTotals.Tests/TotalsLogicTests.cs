using ItemTotals.Totals;
using Xunit;

namespace ItemTotals.Tests;

// The item total: the count of one item in the backpack and in all home storage together. It counts stacks, so the
// uses left and the pollution of a stack do not change it.
public class TotalsLogicTests
{
    private static PoolItem Item(long itemId, int configId, int count) => new PoolItem(itemId, configId, count);

    [Fact]
    public void SumsTheStacksOfOneItemOverSeveralPlaces()
    {
        var totals = TotalsLogic.Build(new[]
        {
            Item(1, 101, 3),   // the backpack
            Item(2, 101, 7),   // one cabinet
            Item(3, 101, 5),   // another cabinet
        });

        Assert.Equal(15, totals.TotalOf(101));
    }

    [Fact]
    public void CountsAPlaceThatTheGameNamesTwiceOnlyOnce()
    {
        // GetAllAvailableItems can give one place more than once, so the same stack arrives twice.
        var totals = TotalsLogic.Build(new[]
        {
            Item(1, 101, 3),
            Item(2, 101, 7),
            Item(1, 101, 3),
            Item(2, 101, 7),
        });

        Assert.Equal(10, totals.TotalOf(101));
    }

    [Fact]
    public void GivesZeroForAnItemThatNoPlaceHolds()
    {
        var totals = TotalsLogic.Build(new[] { Item(1, 101, 3) });

        Assert.Equal(0, totals.TotalOf(202));
    }

    [Fact]
    public void HoldsOneEntryForEachItemThatThePlacesHold()
    {
        var totals = TotalsLogic.Build(new[]
        {
            Item(1, 101, 3),
            Item(2, 202, 1),
            Item(3, 101, 7),
            Item(4, 303, 2),
        });

        Assert.Equal(3, totals.ByConfigId.Count);
        Assert.Equal(10, totals.ByConfigId[101]);
        Assert.Equal(1, totals.ByConfigId[202]);
        Assert.Equal(2, totals.ByConfigId[303]);
    }

    [Fact]
    public void CountsAStackWithUsesLeftAsOneWhateverItsUses()
    {
        // The pool gives the count of the stack, which is what the player reads on the cell. The uses left of a
        // stack are not in it, so three Wild Rabbit of any uses are three.
        var totals = TotalsLogic.Build(new[]
        {
            Item(1, 404, 1),
            Item(2, 404, 1),
            Item(3, 404, 1),
        });

        Assert.Equal(3, totals.TotalOf(404));
    }

    [Fact]
    public void MapsEachItemInstanceToItsItemConfigId()
    {
        // The item cells of the character in the trade window and in the shop window carry the item instance id
        // only, so the page needs this map to name the item.
        var totals = TotalsLogic.Build(new[]
        {
            Item(9001, 101, 3),
            Item(9002, 202, 1),
            Item(9003, 101, 7),
        });

        Assert.Equal(3, totals.ConfigIdByItemId.Count);
        Assert.Equal(101, totals.ConfigIdByItemId[9001]);
        Assert.Equal(202, totals.ConfigIdByItemId[9002]);
        Assert.Equal(101, totals.ConfigIdByItemId[9003]);
    }

    [Fact]
    public void LeavesOutAnItemWithNoConfigIdAndAnItemWithNoCount()
    {
        var totals = TotalsLogic.Build(new[]
        {
            Item(1, 0, 3),
            Item(2, 101, 0),
            Item(3, 101, 4),
        });

        Assert.Equal(4, totals.TotalOf(101));
        Assert.Single(totals.ByConfigId);
        Assert.Single(totals.ConfigIdByItemId);
    }

    [Fact]
    public void AnEmptyPoolGivesNoEntryAndNoTotal()
    {
        var totals = TotalsLogic.Build(new PoolItem[0]);

        Assert.Empty(totals.ByConfigId);
        Assert.Empty(totals.ConfigIdByItemId);
        Assert.Equal(0, totals.TotalOf(101));
        Assert.False(totals.Failed);
    }

    [Fact]
    public void TheFailedSetIsEmptyAndSaysSo()
    {
        var totals = TotalSet.Failure;

        Assert.True(totals.Failed);
        Assert.Empty(totals.ByConfigId);
        Assert.Equal(0, totals.TotalOf(101));
    }

    [Fact]
    public void TwoSetsOfTheSameItemsAreTheSame()
    {
        var a = TotalsLogic.Build(new[] { Item(1, 101, 3), Item(2, 202, 1) });
        var b = TotalsLogic.Build(new[] { Item(2, 202, 1), Item(1, 101, 3) });

        Assert.True(a.SameAs(b));
    }

    [Fact]
    public void ASetWithAnotherCountIsNotTheSame()
    {
        var a = TotalsLogic.Build(new[] { Item(1, 101, 3) });
        var b = TotalsLogic.Build(new[] { Item(1, 101, 4) });

        Assert.False(a.SameAs(b));
    }

    [Fact]
    public void ASetWhereTheSameCountSitsOnAnotherItemInstanceIsNotTheSame()
    {
        // The totals read the same, but the page needs the id map, so this counts as a change.
        var a = TotalsLogic.Build(new[] { Item(9001, 101, 3) });
        var b = TotalsLogic.Build(new[] { Item(9002, 101, 3) });

        Assert.False(a.SameAs(b));
    }

    [Fact]
    public void AFailedSetIsNotTheSameAsAnEmptyOne()
    {
        Assert.False(TotalSet.Failure.SameAs(TotalsLogic.Build(new PoolItem[0])));
    }
}
