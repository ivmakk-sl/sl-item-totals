using ItemTotals.Text;
using Xunit;

namespace ItemTotals.Tests;

// The hover action of the workbench window carries its item as a JSON text (JsonMsg_ItemHoverData: itemId and
// isEnter), so the mod reads the item id out of it.
public class HoverJsonTests
{
    [Fact]
    public void TheItemIdIsRead()
    {
        Assert.Equal(8590008128L, HoverJson.ItemId("{\"itemId\":8590008128,\"isEnter\":true}"));
    }

    [Fact]
    public void TheOrderOfTheFieldsDoesNotMatter()
    {
        Assert.Equal(42L, HoverJson.ItemId("{\"isEnter\":true,\"itemId\":42}"));
    }

    [Fact]
    public void SpacesAroundTheNameAndTheValueAreAllowed()
    {
        Assert.Equal(42L, HoverJson.ItemId("{ \"itemId\" : 42 , \"isEnter\" : false }"));
    }

    [Fact]
    public void ATextWithNoItemIdGivesZero()
    {
        Assert.Equal(0L, HoverJson.ItemId("{\"isEnter\":true}"));
    }

    [Fact]
    public void AnEmptyOrMissingTextGivesZero()
    {
        Assert.Equal(0L, HoverJson.ItemId(""));
        Assert.Equal(0L, HoverJson.ItemId(null));
    }

    [Fact]
    public void ATextThatIsNoJsonGivesZero()
    {
        Assert.Equal(0L, HoverJson.ItemId("not json at all"));
    }

    // The pointer leaving an item sends the same message with isEnter false, and the mod then adds no line.
    [Fact]
    public void TheEnterFlagIsRead()
    {
        Assert.True(HoverJson.IsEnter("{\"itemId\":42,\"isEnter\":true}"));
        Assert.False(HoverJson.IsEnter("{\"itemId\":42,\"isEnter\":false}"));
    }

    // A message with no flag is taken as an enter, so a game update that drops the field does not turn the line off.
    [Fact]
    public void AMessageWithNoEnterFlagCountsAsAnEnter()
    {
        Assert.True(HoverJson.IsEnter("{\"itemId\":42}"));
    }

    [Fact]
    public void AnItemIdThatIsNoNumberGivesZero()
    {
        Assert.Equal(0L, HoverJson.ItemId("{\"itemId\":\"abc\"}"));
    }
}
