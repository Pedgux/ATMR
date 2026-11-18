namespace ATMR.Helpers;

public static class StringHelper
{
    public static string MarkupToString(string markup)
    {
        if (string.IsNullOrEmpty(markup))
            return markup;

        int tagStart = markup.IndexOf('[');
        if (tagStart < 0)
            return markup;

        int tagEnd = markup.IndexOf(']', tagStart);
        if (tagEnd < 0)
            return markup;

        int closingTagStart = markup.IndexOf("[/]", tagEnd);
        if (closingTagStart < 0)
            return markup;

        int textStart = tagEnd + 1;
        int textEnd = closingTagStart;

        if (textStart <= textEnd)
            return markup[textStart..textEnd];

        return markup;
    }
}
