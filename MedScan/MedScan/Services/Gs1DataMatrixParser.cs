using System.Globalization;
using System.Text.RegularExpressions;

namespace MedScan.Services;

internal static class Gs1DataMatrixParser
{
    private const char GroupSeparator = '\u001D';

    public static bool TryExtract(string rawValue, out string? lookupBarcode, out DateOnly? expirationDate)
    {
        lookupBarcode = null;
        expirationDate = null;

        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return false;
        }

        var value = rawValue.Trim();
        if (value.StartsWith("]d2", StringComparison.Ordinal))
        {
            value = value[3..];
        }

        if (TryParseParenthesized(value, out var gtin, out var expiry) ||
            TryParseAiStream(value, out gtin, out expiry))
        {
            lookupBarcode = NormalizeLookupBarcode(gtin);
            expirationDate = expiry;
            return !string.IsNullOrWhiteSpace(lookupBarcode);
        }

        return false;
    }

    private static bool TryParseParenthesized(string value, out string? gtin, out DateOnly? expiry)
    {
        gtin = null;
        expiry = null;

        var matches = Regex.Matches(value, @"\((\d{2})\)([^\(]*)");
        if (matches.Count == 0)
        {
            return false;
        }

        foreach (Match match in matches)
        {
            var ai = match.Groups[1].Value;
            var data = match.Groups[2].Value.Trim();
            if (ai == "01" && data.Length >= 14 && data[..14].All(char.IsDigit))
            {
                gtin = data[..14];
            }
            else if (ai == "17" && data.Length >= 6 && data[..6].All(char.IsDigit))
            {
                expiry = ParseExpiry(data[..6]);
            }
        }

        return gtin is not null;
    }

    private static bool TryParseAiStream(string value, out string? gtin, out DateOnly? expiry)
    {
        gtin = null;
        expiry = null;

        var index = 0;
        while (index + 2 <= value.Length)
        {
            if (value[index] == GroupSeparator)
            {
                index++;
                continue;
            }

            var ai = value.Substring(index, 2);
            index += 2;

            switch (ai)
            {
                case "01":
                    if (index + 14 > value.Length)
                    {
                        return gtin is not null;
                    }

                    var maybeGtin = value.Substring(index, 14);
                    if (!maybeGtin.All(char.IsDigit))
                    {
                        return gtin is not null;
                    }

                    gtin = maybeGtin;
                    index += 14;
                    break;

                case "17":
                    if (index + 6 > value.Length)
                    {
                        return gtin is not null;
                    }

                    var maybeExpiry = value.Substring(index, 6);
                    if (!maybeExpiry.All(char.IsDigit))
                    {
                        return gtin is not null;
                    }

                    expiry = ParseExpiry(maybeExpiry);
                    index += 6;
                    break;

                case "10":
                case "21":
                    while (index < value.Length && value[index] != GroupSeparator)
                    {
                        index++;
                    }
                    break;

                default:
                    // Unsupported AI in this parser; keep scanning to avoid hard-failing valid payloads.
                    break;
            }
        }

        return gtin is not null;
    }

    private static string NormalizeLookupBarcode(string? gtin)
    {
        if (string.IsNullOrWhiteSpace(gtin))
        {
            return string.Empty;
        }

        return gtin.Length == 14 && gtin.StartsWith('0')
            ? gtin[1..]
            : gtin;
    }

    private static DateOnly? ParseExpiry(string yymmdd)
    {
        if (yymmdd.Length != 6)
        {
            return null;
        }

        // GS1 AI 17 is YYMMDD. "00" day may be used when day is unknown; then use month-end.
        if (!int.TryParse(yymmdd[..2], NumberStyles.None, CultureInfo.InvariantCulture, out var yy) ||
            !int.TryParse(yymmdd.Substring(2, 2), NumberStyles.None, CultureInfo.InvariantCulture, out var mm) ||
            !int.TryParse(yymmdd.Substring(4, 2), NumberStyles.None, CultureInfo.InvariantCulture, out var dd))
        {
            return null;
        }

        if (mm is < 1 or > 12)
        {
            return null;
        }

        var year = 2000 + yy;
        var day = dd == 0 ? DateTime.DaysInMonth(year, mm) : dd;

        try
        {
            return new DateOnly(year, mm, day);
        }
        catch
        {
            return null;
        }
    }
}
