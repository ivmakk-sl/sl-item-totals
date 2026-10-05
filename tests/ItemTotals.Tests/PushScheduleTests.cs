using ItemTotals.Web;
using Xunit;

namespace ItemTotals.Tests;

// What the plugin sends to the page script, and when. A hover sends nothing: the number is already in the web page.
public class PushScheduleTests
{
    private const string Json = "{\"v\":1}";

    private static PushSchedule Open(out float now)
    {
        // A listed window refreshed, the page answered that it installed the line, and the page holds version 1.
        var push = new PushSchedule();
        now = 10f;
        push.RequestPage("BackpackUI", now);
        push.Tick(now, 1, () => Json);
        push.OnMessage("frames 1 version 1", now);
        return push;
    }

    [Fact]
    public void SendsNothingWhileNothingHappened()
    {
        var push = new PushSchedule();

        Assert.False(push.Pending(1f));
        Assert.Equal(PushSchedule.Kind.None, push.Tick(1f, 1, () => Json).Kind);
    }

    [Fact]
    public void AWindowRefreshSendsOnce()
    {
        var push = new PushSchedule();
        push.RequestPage("BackpackUI", 1f);

        Assert.True(push.Pending(1f));
        Assert.Equal(PushSchedule.Kind.SetData, push.Tick(1f, 1, () => Json).Kind);
        Assert.False(push.Pending(1f));
        Assert.Equal(PushSchedule.Kind.None, push.Tick(1f, 1, () => Json).Kind);
    }

    [Fact]
    public void ManyRefreshesInOneFrameSendOnce()
    {
        var push = new PushSchedule();
        push.RequestPage("BackpackUI", 1f);
        push.RequestPage("TradeUI", 1f);
        push.RequestPage("BackpackUI", 1f);

        Assert.NotEqual(PushSchedule.Kind.None, push.Tick(1f, 1, () => Json).Kind);
        Assert.Equal(PushSchedule.Kind.None, push.Tick(1f, 1, () => Json).Kind);
    }

    [Fact]
    public void SendsTheDataOnlyWhenItDiffersFromWhatThePageConfirmed()
    {
        float now;
        var push = Open(out now);

        // The items changed: the page holds version 1 and the mod holds version 2.
        push.RequestData(2, now);
        Assert.Equal(PushSchedule.Kind.SetData, push.Tick(now, 2, () => Json).Kind);
        push.OnMessage("frames 1 version 2", now);

        // Nothing changed since: no send.
        Assert.False(push.Pending(now));
    }

    [Fact]
    public void AWindowRefreshWithTheDataAlreadyThereOnlyAppliesTheLine()
    {
        float now;
        var push = Open(out now);

        push.RequestPage("Cooking", now);

        Assert.Equal(PushSchedule.Kind.Apply, push.Tick(now, 1, () => Json).Kind);
    }

    [Fact]
    public void SendsNothingWhileNoListedWindowIsOpen()
    {
        float now;
        var push = Open(out now);
        // The last send found no frame of a listed window, so each window is closed.
        push.OnMessage("frames 0", now);

        push.RequestData(2, now);

        Assert.Equal(PushSchedule.Kind.None, push.Tick(now, 2, () => Json).Kind);
    }

    [Fact]
    public void AWindowThatOpensAfterThatBringsTheSendBack()
    {
        float now;
        var push = Open(out now);
        push.OnMessage("frames 0", now);
        push.RequestData(2, now);
        push.Tick(now, 2, () => Json);

        push.RequestPage("BackpackUI", now);

        Assert.Equal(PushSchedule.Kind.SetData, push.Tick(now, 2, () => Json).Kind);
    }

    [Fact]
    public void RetriesOnceEachRealSecondWhileTheFrameIsNotThereYet()
    {
        // WebUILayer queues a message for a page that has not loaded, so the first send after a window open can find
        // no frame. The retry carries it.
        var push = new PushSchedule();
        push.RequestPage("BackpackUI", 10f);
        push.Tick(10f, 1, () => Json);
        push.OnMessage("frames 0", 10f);

        Assert.False(push.Pending(10.5f));
        Assert.True(push.Pending(11f));
        var step = push.Tick(11f, 1, () => Json);
        Assert.Equal(PushSchedule.Kind.SetData, step.Kind);
        Assert.True(step.Retry);
    }

    // After a save load the script and the data go out before any listed window is open. The page answers that it
    // holds that version, so a retry only looks for the frame again and does not carry the data a second time.
    [Fact]
    public void ARetryForDataThePageHoldsOnlyAppliesTheLine()
    {
        // The order of a save load in the log: the first totals ask for a send, the root page has no script yet, and
        // the script goes out with the data.
        var push = new PushSchedule();
        push.RequestData(1, 10f);
        Assert.Equal(PushSchedule.Kind.SetData, push.Tick(10f, 1, () => Json).Kind);
        push.OnMessage(PushSchedule.NoScript, 10f);
        Assert.Equal(PushSchedule.Kind.Full, push.Tick(10f, 1, () => Json).Kind);
        push.OnMessage("frames 0 version 1", 10f);

        var step = push.Tick(11f, 1, () => Json);

        Assert.Equal(PushSchedule.Kind.Apply, step.Kind);
        Assert.Null(step.Json);
        Assert.True(step.Retry);
    }

    // The items changed while the retry waited, so the retry carries the new data.
    [Fact]
    public void ARetryForNewerDataStillSendsTheData()
    {
        var push = new PushSchedule();
        push.RequestPage("BackpackUI", 10f);
        push.Tick(10f, 1, () => Json);
        push.OnMessage("frames 0 version 1", 10f);

        var step = push.Tick(11f, 2, () => Json);

        Assert.Equal(PushSchedule.Kind.SetData, step.Kind);
        Assert.True(step.Retry);
    }

    [Fact]
    public void StopsRetryingAfterThreeRealSecondsAndWaitsForTheNextRefresh()
    {
        var push = new PushSchedule();
        push.RequestPage("BackpackUI", 10f);
        push.Tick(10f, 1, () => Json);
        for (float t = 11f; t <= 13f; t += 1f)
        {
            push.OnMessage("frames 0", t);
            push.Tick(t, 1, () => Json);
        }
        push.OnMessage("frames 0", 14f);

        Assert.Equal(PushSchedule.Kind.None, push.Tick(14f, 1, () => Json).Kind);
        Assert.Equal(PushSchedule.Kind.None, push.Tick(20f, 1, () => Json).Kind);
    }

    [Fact]
    public void APageWithNoScriptSendsTheScriptWithTheData()
    {
        float now;
        var push = Open(out now);

        push.OnMessage(PushSchedule.NoScript, now);

        Assert.True(push.Pending(now));
        var step = push.Tick(now, 1, () => Json);
        Assert.Equal(PushSchedule.Kind.Full, step.Kind);
        Assert.Equal(Json, step.Json);
    }

    [Fact]
    public void OneFullSendAtATime()
    {
        float now;
        var push = Open(out now);
        push.OnMessage(PushSchedule.NoScript, now);
        push.Tick(now, 1, () => Json);

        // A second answer of the same round.
        push.OnMessage(PushSchedule.NoScript, now + 1f);

        Assert.Equal(PushSchedule.Kind.None, push.Tick(now + 1f, 1, () => Json).Kind);
    }

    [Fact]
    public void AfterTheTimeLimitTheScriptCanGoAgainBecauseTheBrowserCanDropIt()
    {
        float now;
        var push = Open(out now);
        push.OnMessage(PushSchedule.NoScript, now);
        push.Tick(now, 1, () => Json);

        push.OnMessage(PushSchedule.NoScript, now + 6f);

        Assert.Equal(PushSchedule.Kind.Full, push.Tick(now + 6f, 1, () => Json).Kind);
    }

    [Fact]
    public void WarnsOnceForEachDistinctStatusTextAndNotForARepeat()
    {
        var push = new PushSchedule();

        Assert.True(push.ShouldWarn("BackpackUI has no onItemEnter"));
        Assert.False(push.ShouldWarn("BackpackUI has no onItemEnter"));
        Assert.True(push.ShouldWarn("ShopUI has no div.tooltip"));
        Assert.False(push.ShouldWarn("ShopUI has no div.tooltip"));
        Assert.False(push.ShouldWarn("BackpackUI has no onItemEnter"));
    }

    [Fact]
    public void AFrameCountMessageIsNotAWarning()
    {
        var push = new PushSchedule();

        Assert.False(push.ShouldWarn("frames 1 version 2"));
        Assert.False(push.ShouldWarn(PushSchedule.NoScript));
    }

    [Fact]
    public void ABuildThatThrowsRetriesOneRealSecondLaterAndRethrows()
    {
        var push = new PushSchedule();
        push.RequestPage("BackpackUI", 10f);

        Assert.Throws<System.InvalidOperationException>(() => push.Tick(10f, 1, () => throw new System.InvalidOperationException("the window closed")));

        Assert.False(push.Pending(10.5f));
        Assert.True(push.Pending(11f));
    }
}
