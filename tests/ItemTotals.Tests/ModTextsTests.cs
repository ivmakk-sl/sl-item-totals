using SlShared.I18n;
using Xunit;

namespace ItemTotals.Tests;

// The one mod text of the mod, the label of the line, read from the i18n files through the library. The game-facing
// part (the display language and the warnings to the log) is in src/ModTexts.cs and cannot be unit-tested.
public class ModTextsTests
{
    private const int Chinese = 0;
    private const int English = 1;

    private static I18nTexts Texts() => I18nTexts.Load(typeof(ModTextsTests).Assembly, "ItemTotals");

    [Fact]
    public void TheEnglishLabelIsTheWordTheUserChose()
    {
        Assert.Equal("Total: {n}", Texts().For(English, null)["total"]);
    }

    [Fact]
    public void TheChineseLabelIsTheWordOfTheGameForThisNumber()
    {
        Assert.Equal("持有: {n}", Texts().For(Chinese, null)["total"]);
    }

    // The game's own word for this number is "Owned" (WebUI_PlantPanel_11). The mod names no text key, so a game text
    // cannot take the place of the label whatever the game says.
    [Fact]
    public void AGameTextDoesNotTakeThePlaceOfTheLabel()
    {
        var set = Texts().For(English, key => "Owned");

        Assert.Equal("Total: {n}", set["total"]);
        Assert.Empty(set.GameTextNames);
    }

    [Fact]
    public void ADisplayLanguageWithNoFileFallsBackToEnglish()
    {
        var set = Texts().For(7, null, "Klingon");

        Assert.Equal("Total: {n}", set["total"]);
        Assert.Equal("en", set.Language);
    }

    [Fact]
    public void ADisplayLanguageWithNoFileWarnsOnceWithItsNumberAndName()
    {
        var texts = Texts();
        texts.For(7, null, "Klingon");

        Assert.Contains(texts.Warnings, w => w.Contains("7") && w.Contains("Klingon"));
    }

    [Fact]
    public void TheLabelCarriesTheNumberAsAPlaceholderSoATranslatorOwnsTheOrder()
    {
        Assert.Contains("{n}", Texts().For(English, null)["total"]);
        Assert.Contains("{n}", Texts().For(Chinese, null)["total"]);
    }

    [Fact]
    public void FillPutsTheItemTotalIntoTheLabel()
    {
        Assert.Equal("Total: 15", I18nTexts.Fill(Texts().For(English, null)["total"], ("n", 15)));
        Assert.Equal("持有: 15", I18nTexts.Fill(Texts().For(Chinese, null)["total"], ("n", 15)));
    }

    [Fact]
    public void ZeroIsARealAnswerAndFillsTheSameWay()
    {
        Assert.Equal("Total: 0", I18nTexts.Fill(Texts().For(English, null)["total"], ("n", 0)));
    }

    [Fact]
    public void TheModHasOneModTextOnly()
    {
        Assert.Equal(new[] { "total" }, Texts().For(English, null).ToDictionary().Keys);
    }
}
