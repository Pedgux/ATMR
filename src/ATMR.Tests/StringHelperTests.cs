using Xunit;
using ATMR.Helpers;

namespace ATMR.Tests;

public class StringHelperTests
{
    [Fact]
    public void MarkupToString_WithValidMarkup_ReturnsInnerText()
    {
        // Arrange
        string markup = "[red]testtext[/]";

        // Act
        string result = StringHelper.MarkupToString(markup);

        // Assert
        Assert.Equal("testtext", result);
    }

    [Fact]
    public void MarkupToString_WithMarkupAndExtraText_ReturnsInnerText()
    {
        // Arrange
        string markup = "[red]testtext[/] spews out testtext";

        // Act
        string result = StringHelper.MarkupToString(markup);

        // Assert
        Assert.Equal("testtext", result);
    }

    [Fact]
    public void MarkupToString_WithoutMarkup_ReturnsOriginal()
    {
        // Arrange
        string markup = "plain text";

        // Act
        string result = StringHelper.MarkupToString(markup);

        // Assert
        Assert.Equal("plain text", result);
    }

    [Fact]
    public void MarkupToString_WithEmptyString_ReturnsEmpty()
    {
        // Arrange
        string markup = "";

        // Act
        string result = StringHelper.MarkupToString(markup);

        // Assert
        Assert.Equal("", result);
    }

    [Fact]
    public void MarkupToString_WithOnlyOpeningTag_ReturnsOriginal()
    {
        // Arrange
        string markup = "[red]testtext";

        // Act
        string result = StringHelper.MarkupToString(markup);

        // Assert
        Assert.Equal("[red]testtext", result);
    }

    [Fact]
    public void MarkupToString_WithOnlyClosingTag_ReturnsOriginal()
    {
        // Arrange
        string markup = "testtext[/]";

        // Act
        string result = StringHelper.MarkupToString(markup);

        // Assert
        Assert.Equal("testtext[/]", result);
    }

    // Different colors
    [Fact]
    public void MarkupToString_WithBlueColor_ReturnsInnerText()
    {
        string markup = "[blue]hello world[/]";
        string result = StringHelper.MarkupToString(markup);
        Assert.Equal("hello world", result);
    }

    [Fact]
    public void MarkupToString_WithGreenColor_ReturnsInnerText()
    {
        string markup = "[green]success message[/]";
        string result = StringHelper.MarkupToString(markup);
        Assert.Equal("success message", result);
    }

    [Fact]
    public void MarkupToString_WithYellowColor_ReturnsInnerText()
    {
        string markup = "[yellow]warning text[/]";
        string result = StringHelper.MarkupToString(markup);
        Assert.Equal("warning text", result);
    }

    [Fact]
    public void MarkupToString_WithCyanColor_ReturnsInnerText()
    {
        string markup = "[cyan]info text[/]";
        string result = StringHelper.MarkupToString(markup);
        Assert.Equal("info text", result);
    }

    [Fact]
    public void MarkupToString_WithMagentaColor_ReturnsInnerText()
    {
        string markup = "[magenta]debug output[/]";
        string result = StringHelper.MarkupToString(markup);
        Assert.Equal("debug output", result);
    }

    [Fact]
    public void MarkupToString_WithWhiteColor_ReturnsInnerText()
    {
        string markup = "[white]normal text[/]";
        string result = StringHelper.MarkupToString(markup);
        Assert.Equal("normal text", result);
    }

    [Fact]
    public void MarkupToString_WithGreyColor_ReturnsInnerText()
    {
        string markup = "[grey]muted text[/]";
        string result = StringHelper.MarkupToString(markup);
        Assert.Equal("muted text", result);
    }

    // Small texts
    [Fact]
    public void MarkupToString_WithSingleCharacter_ReturnsInnerText()
    {
        string markup = "[red]x[/]";
        string result = StringHelper.MarkupToString(markup);
        Assert.Equal("x", result);
    }

    [Fact]
    public void MarkupToString_WithTwoCharacters_ReturnsInnerText()
    {
        string markup = "[blue]ok[/]";
        string result = StringHelper.MarkupToString(markup);
        Assert.Equal("ok", result);
    }

    [Fact]
    public void MarkupToString_WithShortWord_ReturnsInnerText()
    {
        string markup = "[green]hi[/]";
        string result = StringHelper.MarkupToString(markup);
        Assert.Equal("hi", result);
    }

    [Fact]
    public void MarkupToString_WithShortPhrase_ReturnsInnerText()
    {
        string markup = "[yellow]yes[/]";
        string result = StringHelper.MarkupToString(markup);
        Assert.Equal("yes", result);
    }

    // Long texts
    [Fact]
    public void MarkupToString_WithLongText_ReturnsInnerText()
    {
        string longText = new string('a', 1000);
        string markup = $"[red]{longText}[/]";
        string result = StringHelper.MarkupToString(markup);
        Assert.Equal(longText, result);
    }

    [Fact]
    public void MarkupToString_WithVeryLongText_ReturnsInnerText()
    {
        string longText = "Lorem ipsum dolor sit amet, consectetur adipiscing elit. ".Repeat(50);
        string markup = $"[blue]{longText}[/]";
        string result = StringHelper.MarkupToString(markup);
        Assert.Equal(longText, result);
    }

    [Fact]
    public void MarkupToString_WithLongTextAndExtraContent_ReturnsInnerText()
    {
        string longText = new string('x', 500);
        string markup = $"[green]{longText}[/] some trailing text";
        string result = StringHelper.MarkupToString(markup);
        Assert.Equal(longText, result);
    }

    // Different text content (not "testtext")
    [Fact]
    public void MarkupToString_WithNumbers_ReturnsInnerText()
    {
        string markup = "[red]12345[/]";
        string result = StringHelper.MarkupToString(markup);
        Assert.Equal("12345", result);
    }

    [Fact]
    public void MarkupToString_WithSpecialCharacters_ReturnsInnerText()
    {
        string markup = "[blue]!@#$%^&*()[/]";
        string result = StringHelper.MarkupToString(markup);
        Assert.Equal("!@#$%^&*()", result);
    }

    [Fact]
    public void MarkupToString_WithUnicodeCharacters_ReturnsInnerText()
    {
        string markup = "[green]🎉🎊🎈[/]";
        string result = StringHelper.MarkupToString(markup);
        Assert.Equal("🎉🎊🎈", result);
    }

    [Fact]
    public void MarkupToString_WithNewlines_ReturnsInnerText()
    {
        string markup = "[yellow]line1\nline2\nline3[/]";
        string result = StringHelper.MarkupToString(markup);
        Assert.Equal("line1\nline2\nline3", result);
    }

    [Fact]
    public void MarkupToString_WithTabs_ReturnsInnerText()
    {
        string markup = "[cyan]col1\tcol2\tcol3[/]";
        string result = StringHelper.MarkupToString(markup);
        Assert.Equal("col1\tcol2\tcol3", result);
    }

    [Fact]
    public void MarkupToString_WithJsonLikeContent_ReturnsInnerText()
    {
        string markup = "[magenta]{\"key\": \"value\", \"num\": 42}[/]";
        string result = StringHelper.MarkupToString(markup);
        Assert.Equal("{\"key\": \"value\", \"num\": 42}", result);
    }

    [Fact]
    public void MarkupToString_WithXmlLikeContent_ReturnsInnerText()
    {
        string markup = "[white]<tag attr=\"value\">content</tag>[/]";
        string result = StringHelper.MarkupToString(markup);
        Assert.Equal("<tag attr=\"value\">content</tag>", result);
    }

    // Edge cases
    [Fact]
    public void MarkupToString_WithMultipleTags_FirstTagOnly()
    {
        string markup = "[red]first[/] and [blue]second[/]";
        string result = StringHelper.MarkupToString(markup);
        Assert.Equal("first", result);
    }

    [Fact]
    public void MarkupToString_WithNestedTags_ReturnsInnerTextUntilFirstClosingTag()
    {
        string markup = "[red]outer [blue]inner[/] outer[/]";
        string result = StringHelper.MarkupToString(markup);
        Assert.Equal("outer [blue]inner", result);
    }

    [Fact]
    public void MarkupToString_WithTagAtStartOfString_ReturnsInnerText()
    {
        string markup = "[red]start[/]middle";
        string result = StringHelper.MarkupToString(markup);
        Assert.Equal("start", result);
    }

    [Fact]
    public void MarkupToString_WithTagInMiddle_ReturnsInnerText()
    {
        string markup = "prefix [green]middle[/] suffix";
        string result = StringHelper.MarkupToString(markup);
        Assert.Equal("middle", result);
    }

    [Fact]
    public void MarkupToString_WithTagAtEnd_ReturnsInnerText()
    {
        string markup = "prefix [blue]end[/]";
        string result = StringHelper.MarkupToString(markup);
        Assert.Equal("end", result);
    }

    [Fact]
    public void MarkupToString_WithWhitespaceAroundTag_ReturnsInnerText()
    {
        string markup = "  [red]spaced[/]  ";
        string result = StringHelper.MarkupToString(markup);
        Assert.Equal("spaced", result);
    }

    [Fact]
    public void MarkupToString_WithUppercaseColor_ReturnsInnerText()
    {
        string markup = "[RED]uppercase[/]";
        string result = StringHelper.MarkupToString(markup);
        Assert.Equal("uppercase", result);
    }

    [Fact]
    public void MarkupToString_WithMixedCaseColor_ReturnsInnerText()
    {
        string markup = "[ReD]mixedcase[/]";
        string result = StringHelper.MarkupToString(markup);
        Assert.Equal("mixedcase", result);
    }

    [Fact]
    public void MarkupToString_WithColorContainingNumbers_ReturnsInnerText()
    {
        string markup = "[color123]numbered[/]";
        string result = StringHelper.MarkupToString(markup);
        Assert.Equal("numbered", result);
    }

    [Fact]
    public void MarkupToString_WithColorContainingUnderscore_ReturnsInnerText()
    {
        string markup = "[dark_blue]underscore[/]";
        string result = StringHelper.MarkupToString(markup);
        Assert.Equal("underscore", result);
    }

    [Fact]
    public void MarkupToString_WithColorContainingDash_ReturnsInnerText()
    {
        string markup = "[light-blue]dash[/]";
        string result = StringHelper.MarkupToString(markup);
        Assert.Equal("dash", result);
    }

    [Fact]
    public void MarkupToString_NullInput_ReturnsNull()
    {
        string? markup = null;
        string result = StringHelper.MarkupToString(markup!);
        Assert.Null(result);
    }
}

// Helper extension for tests
file static class StringExtensions
{
    public static string Repeat(this string s, int count)
    {
        return string.Concat(Enumerable.Repeat(s, count));
    }
}