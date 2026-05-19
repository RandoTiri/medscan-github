using System.Globalization;
using System.Text.RegularExpressions;

namespace MedScan.MAUI.Services.Scanning;

public static class Gs1DataMatrixParser {
    private const char GroupSeparator = '';
    private const string SymbologyIdentifier = "]d2";
    private const string AiGtin = "01";
    private const string AiExpirationYymmdd = "17";
    private const string AiBatchNumber = "10";
    private const string AiSerialNumber = "21";

    private const int GtinLength = 14;
    private const int YymmddLength = 6;
    private const int AiPrefixLength = 2;

    public static bool TryExtract(string rawValue,out Gs1ParseResult result) {
        result = default;

        if (string.IsNullOrWhiteSpace(rawValue)) 
            return false;

        var value = rawValue.Trim();
        if (value.StartsWith(SymbologyIdentifier,StringComparison.Ordinal))
            value = value[SymbologyIdentifier.Length..];

        if (!TryParseParenthesized(value,out var parsed) &&
            !TryParseAiStream(value,out parsed)) 
            return false;

        var lookupBarcode = NormalizeLookupBarcode(parsed.Gtin);
        if (string.IsNullOrWhiteSpace(lookupBarcode)) 
            return false;

        result = new Gs1ParseResult(
            lookupBarcode,
            parsed.Expiry,
            parsed.Lot,
            parsed.Serial);
        return true;
    }

    private static bool TryParseParenthesized(string value,out Gs1RawValues parsed) {
        parsed = default;

        var matches = Regex.Matches(value,@"\((\d{2})\)([^\(]*)");
        if (matches.Count == 0) 
            return false;

        string? gtin = null;
        DateOnly? expiry = null;
        string? lot = null;
        string? serial = null;

        foreach (Match match in matches) {
            var ai = match.Groups[1].Value;
            var data = match.Groups[2].Value.Trim();

            if (ai == AiGtin && data.Length >= GtinLength && data[..GtinLength].All(char.IsDigit)) {
                gtin = data[..GtinLength];
            } else if (ai == AiExpirationYymmdd && data.Length >= YymmddLength && data[..YymmddLength].All(char.IsDigit)) {
                expiry = ParseExpiry(data[..YymmddLength]);
            } else if (ai == AiBatchNumber) {
                lot = string.IsNullOrWhiteSpace(data) ? null : data;
            } else if (ai == AiSerialNumber) {
                serial = string.IsNullOrWhiteSpace(data) ? null : data;
            }
        }

        if (gtin is null) 
            return false;

        parsed = new Gs1RawValues(gtin,expiry,lot,serial);
        return true;
    }

    private static bool TryParseAiStream(string value,out Gs1RawValues parsed) {
        parsed = default;

        string? gtin = null;
        DateOnly? expiry = null;
        string? lot = null;
        string? serial = null;

        var index = 0;
        while (index + AiPrefixLength <= value.Length) {
            if (value[index] == GroupSeparator) {
                index++;
                continue;
            }

            var ai = value.Substring(index,AiPrefixLength);
            index += AiPrefixLength;

            switch (ai) {
                case AiGtin:
                if (index + GtinLength > value.Length) {
                    return TryFinalize(gtin,expiry,lot,serial,out parsed);
                }

                var maybeGtin = value.Substring(index,GtinLength);
                if (!maybeGtin.All(char.IsDigit)) {
                    return TryFinalize(gtin,expiry,lot,serial,out parsed);
                }

                gtin = maybeGtin;
                index += GtinLength;
                break;

                case AiExpirationYymmdd:
                if (index + YymmddLength > value.Length) {
                    return TryFinalize(gtin,expiry,lot,serial,out parsed);
                }

                var maybeExpiry = value.Substring(index,YymmddLength);
                if (!maybeExpiry.All(char.IsDigit)) {
                    return TryFinalize(gtin,expiry,lot,serial,out parsed);
                }

                expiry = ParseExpiry(maybeExpiry);
                index += YymmddLength;
                break;

                case AiBatchNumber:
                lot = ReadVariableLengthSegment(value,ref index);
                break;

                case AiSerialNumber:
                serial = ReadVariableLengthSegment(value,ref index);
                break;

                default:
                // Unsupported AI in this parser; keep scanning to avoid hard-failing valid payloads.
                break;
            }
        }

        return TryFinalize(gtin,expiry,lot,serial,out parsed);
    }

    private static bool TryFinalize(string? gtin,DateOnly? expiry,string? lot,string? serial,out Gs1RawValues parsed) {
        if (gtin is null) {
            parsed = default;
            return false;
        }

        parsed = new Gs1RawValues(gtin,expiry,lot,serial);
        return true;
    }

    private static string? ReadVariableLengthSegment(string value,ref int index) {
        var start = index;
        while (index < value.Length && value[index] != GroupSeparator) {
            index++;
        }

        var data = value[start..index];
        return string.IsNullOrWhiteSpace(data) ? null : data;
    }

    private static string NormalizeLookupBarcode(string? gtin) {
        if (string.IsNullOrWhiteSpace(gtin)) 
            return string.Empty;

        return new string(gtin.Where(char.IsDigit).ToArray());
    }

    private static DateOnly? ParseExpiry(string yymmdd) {
        if (yymmdd.Length != YymmddLength) 
            return null;

        if (!int.TryParse(yymmdd[..2],NumberStyles.None,CultureInfo.InvariantCulture,out var yy) ||
            !int.TryParse(yymmdd.Substring(2,2),NumberStyles.None,CultureInfo.InvariantCulture,out var mm) ||
            !int.TryParse(yymmdd.Substring(4,2),NumberStyles.None,CultureInfo.InvariantCulture,out var dd)) {
            return null;
        }

        if (mm is < 1 or > 12) 
            return null;

        var year = 2000 + yy;
        var day = dd == 0 ? DateTime.DaysInMonth(year,mm) : dd;

        try {
            return new DateOnly(year,mm,day);
        } catch (ArgumentOutOfRangeException) {
            return null;
        }
    }

    private readonly record struct Gs1RawValues(string? Gtin,DateOnly? Expiry,string? Lot,string? Serial);
}

public readonly record struct Gs1ParseResult(
    string LookupBarcode,
    DateOnly? ExpirationDate,
    string? BatchNumber,
    string? SerialNumber);