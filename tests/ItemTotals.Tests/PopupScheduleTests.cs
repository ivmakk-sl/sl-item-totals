using ItemTotals.Text;
using Xunit;

namespace ItemTotals.Tests;

// The sends of the line of the item detail popup. The reducer that opens the popup runs before the web page of the
// popup is built, so the first send finds no frame and the schedule sends again until the page reports that it drew
// the line, or until the window runs out.
public class PopupScheduleTests
{
    private const string Line = "Total: 15";

    [Fact]
    public void TheOpenSendsAtOnce()
    {
        var s = new PopupSchedule();

        s.Open(Line, 100f);

        Assert.Equal(Line, s.Tick(100f));
    }

    [Fact]
    public void NothingIsSentWithNoOpen()
    {
        Assert.Null(new PopupSchedule().Tick(100f));
    }

    [Fact]
    public void ASecondTickOfTheSameMomentSendsNothing()
    {
        var s = new PopupSchedule();
        s.Open(Line, 100f);
        s.Tick(100f);

        Assert.Null(s.Tick(100f));
    }

    [Fact]
    public void TheNextSendComesAfterTheGap()
    {
        var s = new PopupSchedule();
        s.Open(Line, 100f);
        s.Tick(100f);

        Assert.Null(s.Tick(100f + PopupSchedule.Gap / 2f));
        Assert.Equal(Line, s.Tick(100f + PopupSchedule.Gap));
    }

    // The page posts that it drew the line, which is what ends the sends. Without it the schedule would send until
    // the window runs out on each open of the popup.
    [Fact]
    public void TheReportOfThePageEndsTheSends()
    {
        var s = new PopupSchedule();
        s.Open(Line, 100f);
        s.Tick(100f);

        s.Shown();

        Assert.Null(s.Tick(100f + PopupSchedule.Gap));
    }

    // The popup can stay closed, for example when the player closes it in the same frame, so the sends end by
    // themselves.
    [Fact]
    public void TheSendsEndWhenTheWindowRunsOut()
    {
        var s = new PopupSchedule();
        s.Open(Line, 100f);
        s.Tick(100f);

        Assert.Null(s.Tick(100f + PopupSchedule.Window + 0.1f));
    }

    [Fact]
    public void ASecondOpenSendsTheNewLineAndOpensTheWindowAgain()
    {
        var s = new PopupSchedule();
        s.Open(Line, 100f);
        s.Tick(100f);
        s.Shown();

        s.Open("Total: 14", 200f);

        Assert.Equal("Total: 14", s.Tick(200f));
    }

    [Fact]
    public void AnEmptyLineIsSentToo()
    {
        var s = new PopupSchedule();

        s.Open("", 100f);

        Assert.Equal("", s.Tick(100f));
    }

    // The report of the page answers the line of an earlier open when it comes after a new open and before its first
    // send, so it must not end the sends of the new line.
    [Fact]
    public void AReportBeforeTheFirstSendOfANewOpenEndsNothing()
    {
        var s = new PopupSchedule();
        s.Open(Line, 100f);
        s.Tick(100f);

        s.Open("Total: 14", 100.05f);
        s.Shown();

        Assert.Equal("Total: 14", s.Tick(100.05f));
    }
}
