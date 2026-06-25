
using System.Text;

namespace sticergen.Services;

public class PackNameGenerator
{
    private static readonly Dictionary<char, string> Transliteration = new()
    {
        ['а'] = "a", ['б'] = "b", ['в'] = "v", ['г'] = "g", ['д'] = "d",
        ['е'] = "e", ['ё'] = "e", ['ж'] = "zh", ['з'] = "z", ['и'] = "i",
        ['й'] = "y", ['к'] = "k", ['л'] = "l", ['м'] = "m", ['н'] = "n",
        ['о'] = "o", ['п'] = "p", ['р'] = "r", ['с'] = "s", ['т'] = "t",
        ['у'] = "u", ['ф'] = "f", ['х'] = "h", ['ц'] = "ts", ['ч'] = "ch",
        ['ш'] = "sh", ['щ'] = "sch", ['ъ'] = "", ['ы'] = "y", ['ь'] = "",
        ['э'] = "e", ['ю'] = "yu", ['я'] = "ya"
    };

    public string Generate(string title, long userId, int draftId, string botUsername)
    {
        var slug = Transliterate(title);

        if (string.IsNullOrWhiteSpace(slug))
        {
            slug = "pack";
        }

        return $"{slug}_{userId}_{draftId}_by_{botUsername}";
    }

    private static string Transliterate(string value)
    {
        var result = new StringBuilder();
        var previousWasUnderscore = false;

        foreach (var originalChar in value.ToLowerInvariant())
        {
            string part;

            if (Transliteration.TryGetValue(originalChar, out var transliterated))
            {
                part = transliterated;
            }
            else if (originalChar is >= 'a' and <= 'z' or >= '0' and <= '9')
            {
                part = originalChar.ToString();
            }
            else
            {
                part = "_";
            }

            if (part == "_")
            {
                if (!previousWasUnderscore)
                {
                    result.Append('_');
                    previousWasUnderscore = true;
                }

                continue;
            }

            result.Append(part);
            previousWasUnderscore = false;
        }

        return result.ToString().Trim('_');
    }
}
