using System.Text.RegularExpressions;

namespace Apify.Utils;

public static class StubManager
{
    private static readonly Regex _placeholderRe = new Regex(@"\{\{\s*(.+?)\s*\}\}",
        RegexOptions.Compiled);

    /// <summary>
    /// Replaces all {{path.to.value}} stubs in <paramref name="template"/> by
    /// looking up nested dictionaries in <paramref name="variables"/>.
    /// </summary>
    public static string Replace(
        string template,
        Dictionary<string, object> vars)
    {
        return _placeholderRe.Replace(template, match =>
        {
            // split "users.posts.comment.id" → ["users","posts","comment","id"]
            var parts = match.Groups[1]
            .Value
            .Split('.', StringSplitOptions.RemoveEmptyEntries);

            object current = vars;
            foreach (var part in parts)
            {
                if (current is Dictionary<string, object> dict
                    && dict.TryGetValue(part, out var next))
                {
                    current = next;
                } else if (current is Dictionary<string, string> dict1
                           && dict1.TryGetValue(part, out var nextStr))
                {
                    current = nextStr;
                }
                else
                {
                    // missing key or wrong type → leave {{…}} as-is
                    return match.Value;
                }
            }

            // found leaf value!
            return current?.ToString() ?? "";
        });
    }
}