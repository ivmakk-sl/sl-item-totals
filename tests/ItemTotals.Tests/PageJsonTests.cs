using System.Collections.Generic;
using ItemTotals.Totals;
using ItemTotals.Web;
using Xunit;

namespace ItemTotals.Tests;

// The JSON that the plugin sends to the page script, and the commands that carry it.
public class PageJsonTests
{
    private static TotalSet Set(params (long itemId, int configId, int count)[] items)
    {
        var pool = new PoolItem[items.Length];
        for (int i = 0; i < items.Length; i++) pool[i] = new PoolItem(items[i].itemId, items[i].configId, items[i].count);
        return TotalsLogic.Build(pool);
    }

    [Fact]
    public void HoldsTheTotalOfEachItemKeyedByTheItemConfigId()
    {
        string json = PageJson.Build(Set((9001, 101, 3), (9002, 101, 7), (9003, 202, 1)), "Total: {n}", 4);

        Assert.Contains("\"101\":10", json);
        Assert.Contains("\"202\":1", json);
    }

    [Fact]
    public void HoldsTheItemConfigIdOfEachStackKeyedByTheItemInstanceId()
    {
        string json = PageJson.Build(Set((9001, 101, 3), (9002, 202, 1)), "Total: {n}", 4);

        Assert.Contains("\"9001\":101", json);
        Assert.Contains("\"9002\":202", json);
    }

    [Fact]
    public void HoldsTheLineTextAndTheDataVersion()
    {
        string json = PageJson.Build(Set((9001, 101, 3)), "Total: {n}", 7);

        Assert.Contains("\"line\":\"Total: {n}\"", json);
        Assert.Contains("\"v\":7", json);
    }

    [Fact]
    public void HoldsTheChineseLineTextWhenTheDisplayLanguageIsChinese()
    {
        string json = PageJson.Build(Set((9001, 101, 3)), "持有: {n}", 1);

        Assert.Contains("\"line\":\"持有: {n}\"", json);
    }

    [Fact]
    public void HoldsTheListOfTheWebPagesThatShowTheLine()
    {
        string json = PageJson.Build(Set(), "Total: {n}", 1);

        foreach (var page in PageJson.Pages) Assert.Contains("\"" + page.Name + "\"", json);
    }

    [Fact]
    public void AnEmptyPoolGivesEmptyMapsAndStillTheLineText()
    {
        string json = PageJson.Build(Set(), "Total: {n}", 1);

        Assert.Contains("\"totals\":{}", json);
        Assert.Contains("\"items\":{}", json);
        Assert.Contains("\"line\":\"Total: {n}\"", json);
    }

    [Fact]
    public void AFailedReadSaysSoSoThePageDrawsNoLine()
    {
        string json = PageJson.Build(TotalSet.Failure, "Total: {n}", 1);

        Assert.Contains("\"failed\":true", json);
    }

    [Fact]
    public void AGoodReadDoesNotSayFailed()
    {
        Assert.DoesNotContain("\"failed\":true", PageJson.Build(Set((9001, 101, 3)), "Total: {n}", 1));
    }

    [Fact]
    public void TheSetDataCommandCallsThePageScriptAndFallsBackToTheNoScriptMessage()
    {
        string command = PageJson.SetDataCommand("{\"v\":1}");

        Assert.StartsWith("window.__itemtotals?window.__itemtotals.setData({\"v\":1})", command);
        Assert.Contains("window.vuplex.postMessage('slmod|itemtotals|no script')", command);
    }

    [Fact]
    public void TheApplyCommandSendsNoData()
    {
        Assert.Contains("window.__itemtotals.apply()", PageJson.ApplyCommand);
        Assert.DoesNotContain("setData", PageJson.ApplyCommand);
    }

    [Fact]
    public void TheFullSendIsTheScriptAndThenTheData()
    {
        string command = PageJson.SetDataWithScriptCommand("var page=1", "{\"v\":1}");

        Assert.StartsWith("var page=1;", command);
        Assert.Contains("window.__itemtotals.setData({\"v\":1})", command);
        // The script itself defines the interface, so the full send needs no fallback message.
        Assert.DoesNotContain("no script", command);
    }

    [Fact]
    public void TheMessagePrefixNamesThisModOnly()
    {
        Assert.Equal("slmod|itemtotals|", PageJson.MessagePrefix);
    }

    [Fact]
    public void TheLineTextIsEscapedSoAQuoteCannotBreakTheJson()
    {
        string json = PageJson.Build(Set(), "a\"b", 1);

        Assert.Contains("\"line\":\"a\\\"b\"", json);
    }

    // The page table of the library and the table of the mod name the same windows with the same page ids, so the
    // two sides cannot drift.
    [Fact]
    public void TheWebPageTableMatchesThePageTableOfTheTooltipLinesLibrary()
    {
        var inLibrary = LibraryPages.Read();

        var mine = new List<(string, string)>();
        foreach (var page in PageJson.Pages) mine.Add((page.Name, page.Id));
        Assert.Equal(inLibrary, mine);
    }

    // The three windows whose page id is not their folder name. A wrong id here means the line never installs in that
    // window, which is how the first in-game checkpoint failed.
    [Theory]
    [InlineData("Backpack", "BackpackUI")]
    [InlineData("Trade", "TradeUI")]
    [InlineData("Shop", "ShopUI")]
    [InlineData("Cooking", "Cooking")]
    public void FindsTheWindowOfAPageIdOfTheGame(string pageId, string page)
    {
        Assert.Equal(page, PageJson.NameOfPageId(pageId));
    }

    [Theory]
    [InlineData("CoreUI1")]
    [InlineData("ItemDetailPopup")]
    [InlineData("MainUI")]
    [InlineData("BackpackUI")]
    [InlineData(null)]
    public void FindsNoWindowForAPageIdThatIsNotListed(string pageId)
    {
        Assert.Null(PageJson.NameOfPageId(pageId));
    }
}
