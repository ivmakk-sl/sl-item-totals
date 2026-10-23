using ItemTotals.Text;
using Xunit;

namespace ItemTotals.Tests;

// The label of the line is the game's own text of its count at home (SR_Web_HomeStock_Usable, game 1.1.19611 and
// later), so the line reads as the game's line in each display language and each language pack of the game. The mod
// keeps no text of its own. The game-facing part (the text lookup and the warning to the log) is in src/ModTexts.cs.
public class GameLabelTests
{
    [Fact]
    public void TheEnglishTextOfTheGameGivesTheLine()
    {
        string line = GameLabel.LinePattern("Usable at home: {0}");

        Assert.Equal("Usable at home: 15", TextLogic.Fill(line, 15));
    }

    [Fact]
    public void TheChineseTextOfTheGameGivesTheLine()
    {
        Assert.Equal("家里可用：15", TextLogic.Fill(GameLabel.LinePattern("家里可用：{0}"), 15));
    }

    [Fact]
    public void TheItemTotalTakesThePlaceOfThePlaceholderWhereverItSits()
    {
        Assert.Equal("15 usable at home", TextLogic.Fill(GameLabel.LinePattern("{0} usable at home"), 15));
    }

    // The pattern of the line inside the mod: the page script, the workbench text, and the item detail popup fill
    // its {n}.
    [Fact]
    public void ThePatternCarriesThePlaceholderOfTheMod()
    {
        Assert.Equal("Usable at home: {n}", GameLabel.LinePattern("Usable at home: {0}"));
    }

    // A game before 1.1.19611 has no text of the key: the lookup gives an empty text, or the key itself.
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(GameLabel.TextKey)]
    public void AGameWithNoTextOfTheKeyGivesNoLine(string gameText)
    {
        Assert.Null(GameLabel.LinePattern(gameText));
    }

    [Fact]
    public void NoLineLeavesTheTextOfTheGameAsItIs()
    {
        Assert.Equal("A tin of beans.", TextLogic.Append("A tin of beans.", GameLabel.LinePattern(""), 15));
    }

    [GameContextFact]
    public void TheTextKeyIsAConstantTextOfTheGame()
    {
        Assert.Contains(GameLabel.TextKey, ConstantTextTsv.Keys());
    }
}

internal static class ConstantTextTsv
{
    // game/config/views/constant_text.tsv, found by a walk up from the test folder, or null.
    public static readonly string Path = Find();

    // The key column of each row.
    public static HashSet<string> Keys() =>
        new HashSet<string>(File.ReadLines(Path).Skip(1).Select(line => line.Split('\t')[0]));

    private static string Find()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir != null; dir = dir.Parent)
        {
            string path = System.IO.Path.Combine(dir.FullName, "game", "config", "views", "constant_text.tsv");
            if (File.Exists(path)) return path;
        }
        return null;
    }
}

internal sealed class GameContextFactAttribute : FactAttribute
{
    public GameContextFactAttribute()
    {
        if (ConstantTextTsv.Path == null) Skip = "The game context (game/config/views/constant_text.tsv) is missing.";
    }
}
