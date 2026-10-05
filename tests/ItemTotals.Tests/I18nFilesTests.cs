using SlShared.I18n;
using Xunit;

// Checks the i18n files of the mod (src/i18n/), embedded in the test assembly as in the mod DLL.
public class I18nFilesTests
{
    private static Dictionary<string, string> Files() => I18nTexts.ReadFiles(typeof(I18nFilesTests).Assembly, "ItemTotals");

    [Fact]
    public void TheI18nFilesHaveNoProblem()
    {
        Assert.Contains("en.json", Files().Keys);
        Assert.Empty(I18nCheck.Problems(Files(), null));
    }

    [GameContextFact]
    public void EachTextKeyIsAConstantTextOfTheGame()
    {
        Assert.Empty(I18nCheck.Problems(Files(), ConstantTextTsv.Keys()));
    }

    // The label takes no text of the game on purpose: the i18n library puts the game's text of a text key above the
    // text of the i18n file, and the game's own word for this number is "Owned" (WebUI_PlantPanel_11), not the
    // "Total" the line shows. A text key here would silently change the label of the mod.
    [Fact]
    public void TheModNamesNoTextKey()
    {
        Assert.DoesNotContain(I18nTexts.TextKeysFile, Files().Keys);
    }

    [Fact]
    public void EachLanguageHasTheLabelWithItsNumberPlaceholder()
    {
        var files = Files();
        foreach (string name in new[] { "en.json", "zh.json" })
        {
            Assert.Contains(name, files.Keys);
            Assert.Contains("{n}", files[name]);
        }
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
