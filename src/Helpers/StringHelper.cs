namespace ATMR.Helpers;

public static class StringHelper
{
    // probably works
    public static string MarkupToString(string markup)
    {
        if (markup.StartsWith('[') && markup.EndsWith("[/]"))
        {
            int tagEnd = markup.IndexOf(']');
            int textStart = tagEnd + 1;
            int textEnd = markup.Length - 3;

            if (tagEnd >= 0 && textStart <= textEnd)
                return markup[textStart..textEnd];
        }

        return markup;
    }
}
