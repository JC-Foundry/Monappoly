using System.Globalization;
using System.Text;

namespace UltimateMonopoly.Helpers;

/// <summary>
/// Canonicalises a string for profanity matching. The SAME transform is applied to both the seed words
/// (stored as <c>BlockedWord.NormalisedWord</c>) and to user input before matching, so the two sides are
/// always comparable — uppercasing is just its final step.
/// </summary>
public static class ProfanityNormaliser
{
    // Common leetspeak / symbol substitutions → the letter they stand in for. Applied after uppercasing.
    private static readonly Dictionary<char, char> LeetMap = new()
    {
        ['0'] = 'O', ['1'] = 'I', ['3'] = 'E', ['4'] = 'A', ['5'] = 'S',
        ['7'] = 'T', ['8'] = 'B', ['9'] = 'G', ['@'] = 'A', ['$'] = 'S',
        ['!'] = 'I', ['|'] = 'I', ['+'] = 'T', ['('] = 'C',
    };

    /// <summary>
    /// Returns the canonical form: strip diacritics (é→e), map leetspeak, drop non-letters EXCEPT
    /// whitespace (so intra-word punctuation vanishes — defeating "f.u.c.k" / "sh1t" — while word
    /// boundaries survive so the library's whole-word matcher still works and "passing" isn't flagged for
    /// "ass"), collapse whitespace runs to a single space, then collapse runs of 3+ identical letters to
    /// one ("fuuuuck"→"fuck"). No English word has 3 identical letters in a row, so that collapse never
    /// mangles a real word, while genuine doubles ("bookkeeper") survive. Note: space-separated single
    /// letters ("f u c k") are deliberately NOT collapsed — that evasion is left to slip, since the gate
    /// is biased to under-block and usernames (which disallow spaces) can't use it anyway.
    /// </summary>
    public static string Normalise(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        // 1. Decompose accented chars so each diacritic becomes a separate, droppable mark.
        var decomposed = input.Normalize(NormalizationForm.FormD);

        // 2. Uppercase, leet-map, keep letters and whitespace (whitespace preserves word boundaries;
        //    runs are collapsed to a single space below).
        var cleaned = new StringBuilder(decomposed.Length);
        foreach (var ch in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.NonSpacingMark)
                continue; // diacritic

            if (char.IsWhiteSpace(ch))
            {
                cleaned.Append(' ');
                continue;
            }

            var upper = char.ToUpperInvariant(ch);
            if (LeetMap.TryGetValue(upper, out var mapped))
                upper = mapped;

            if (char.IsLetter(upper))
                cleaned.Append(upper);
        }

        // 3. Collapse runs of 3+ identical letters to one, and runs of whitespace to a single space.
        //    Trim so leading/trailing boundary spaces don't linger.
        var s = cleaned.ToString();
        if (s.Length == 0)
            return s;

        var result = new StringBuilder(s.Length);
        var i = 0;
        while (i < s.Length)
        {
            var c = s[i];
            var j = i;
            while (j < s.Length && s[j] == c)
                j++;

            var runLength = j - i;
            if (c == ' ')
                result.Append(' ');                              // any whitespace run → one space
            else
                result.Append(c, runLength >= 3 ? 1 : runLength); // 3+ identical letters → one
            i = j;
        }

        return result.ToString().Trim();
    }
}