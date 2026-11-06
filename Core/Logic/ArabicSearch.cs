using System.Globalization;
using System.Text;

namespace AlMithaq.Core.Logic;

public static class ArabicSearch
{
    public static string Normalize(string text)
    {
        var result = new StringBuilder();
        foreach (var c in text.Normalize(NormalizationForm.FormD))
        {
            if (char.GetUnicodeCategory(c) is UnicodeCategory.NonSpacingMark or UnicodeCategory.EnclosingMark
                || c == '\u0640' || c is >= '\u06D6' and <= '\u06ED') continue;
            result.Append(c switch { 'ٱ' or 'أ' or 'إ' or 'آ' => 'ا', 'ى' => 'ي', _ => c });
        }
        return result.ToString().Normalize(NormalizationForm.FormC).Trim();
    }
}
