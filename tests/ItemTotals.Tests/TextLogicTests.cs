using ItemTotals.Text;
using Xunit;

namespace ItemTotals.Tests;

// The line appended to a text that the game built, for the workbench window, which builds its text in C# rather than
// in a web page.
public class TextLogicTests
{
    private const string Line = "Total: {n}";

    // One line break, so the line is the last line of the text. The page script of the mod moves it out of that text
    // again and draws it as a tooltip block with a rule above it, which no C# surface can write.
    private const string Sep = "\n";

    [Fact]
    public void TheLineGoesBelowTheTextOfTheGame()
    {
        Assert.Equal("A tin of beans." + Sep + "Total: 15", TextLogic.Append("A tin of beans.", Line, 15));
    }

    // The page script finds the line by the text before the number, as the last line of the text.
    [Fact]
    public void TheLineIsTheLastLineOfTheText()
    {
        string text = TextLogic.Append("First.\nSecond.", Line, 15);

        Assert.Equal("Total: 15", text.Substring(text.LastIndexOf('\n') + 1));
    }

    [Fact]
    public void ATextOfSeveralLinesKeepsItsLinesAndTheirOrder()
    {
        Assert.Equal("First.\nSecond." + Sep + "Total: 4", TextLogic.Append("First.\nSecond.", Line, 4));
    }

    // With no text of the game there is nothing to set the line apart from, so the rule stays away.
    [Fact]
    public void AnEmptyTextGivesTheLineAlone()
    {
        Assert.Equal("Total: 7", TextLogic.Append("", Line, 7));
    }

    [Fact]
    public void ANullTextGivesTheLineAlone()
    {
        Assert.Equal("Total: 7", TextLogic.Append(null, Line, 7));
    }

    [Fact]
    public void ZeroIsARealAnswerAndShows()
    {
        Assert.Equal("A tin of beans." + Sep + "Total: 0", TextLogic.Append("A tin of beans.", Line, 0));
    }

    [Fact]
    public void TheChineseLabelFillsTheSameWay()
    {
        Assert.Equal("罐头。" + Sep + "持有: 15", TextLogic.Append("罐头。", "持有: {n}", 15));
    }

    // A translator owns the separator and the order of the word and the number, so the page and this code only fill
    // the placeholder. A text with none shows as it is, rather than losing the number without a sign.
    [Fact]
    public void ALineWithNoPlaceholderShowsAsItIs()
    {
        Assert.Equal("A tin of beans." + Sep + "Total", TextLogic.Append("A tin of beans.", "Total", 15));
    }

    [Fact]
    public void NoLineTextLeavesTheTextOfTheGameUntouched()
    {
        Assert.Equal("A tin of beans.", TextLogic.Append("A tin of beans.", "", 15));
        Assert.Equal("A tin of beans.", TextLogic.Append("A tin of beans.", null, 15));
    }

    // The reducer of a window can run again for the same item, so the text it gives can already end with the line.
    // A second append would show the number twice.
    [Fact]
    public void TheLineIsNotAppendedTwiceWhenTheTextAlreadyEndsWithIt()
    {
        string once = TextLogic.Append("A tin of beans.", Line, 15);

        Assert.Equal(once, TextLogic.Append(once, Line, 15));
    }

    [Fact]
    public void ATextThatEndsWithAnOldNumberGetsTheNewOne()
    {
        string old = TextLogic.Append("A tin of beans.", Line, 15);

        Assert.Equal("A tin of beans." + Sep + "Total: 14", TextLogic.Append(old, Line, 14));
    }

    // The page script finds the line as the last line, so a second run must leave one line and not two.
    [Fact]
    public void ASecondRunLeavesOneLineAndNotTwo()
    {
        string old = TextLogic.Append("A tin of beans.", Line, 15);

        string again = TextLogic.Append(old, Line, 14);

        Assert.Equal(1, again.Split("Total: ").Length - 1);
    }

    [Fact]
    public void ATrailingLineBreakOfTheGameDoesNotMakeAnEmptyLine()
    {
        Assert.Equal("A tin of beans." + Sep + "Total: 15", TextLogic.Append("A tin of beans.\n", Line, 15));
    }
}
