using System.Text;

namespace AlMithaq.Core.Logic;

public static class NumberInput
{
    public static string Normalize(string value)
    {
        var result = new StringBuilder(value.Length);
        foreach (var c in value.Trim())
        {
            if (c is >= '٠' and <= '٩') result.Append((char)('0' + c - '٠'));
            else if (c is >= '۰' and <= '۹') result.Append((char)('0' + c - '۰'));
            else if (c is '\u200e' or '\u200f' or '\u061c') continue;
            else if (c is '٫' or ',' or '،') result.Append('.');
            else if (c == '−') result.Append('-');
            else if (c != '٬') result.Append(c);
        }
        return result.ToString();
    }
}
