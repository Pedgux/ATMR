namespace ATMR.Helpers;

public static class StringHelper
{
    // probably works
    public static string MarkupToString(string markup)
    {
        string trueString = markup;

        if (markup.StartsWith("[") && markup.EndsWith("[/]"))
        {
            // remove [color] part
            int end = markup.IndexOf(']');
            if (end >= 0)
                trueString = markup.Substring(end + 1);
        }
        trueString = trueString.Replace("[/]", "");

        return trueString;
    }
}
