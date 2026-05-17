using System;
using System.Globalization;

[Serializable]
public class SatelliteTleData
{
    public string satelliteName;
    public string line1;
    public string line2;

    public bool hasNoradCatalogId;
    public int noradCatalogId;

    public bool hasClassification;
    public string classification;

    public bool hasEpoch;
    public string epoch;

    public bool hasInclination;
    public double inclination;

    public bool hasRaan;
    public double raan;

    public bool hasEccentricity;
    public double eccentricity;

    public bool hasArgumentOfPerigee;
    public double argumentOfPerigee;

    public bool hasMeanAnomaly;
    public double meanAnomaly;

    public bool hasMeanMotion;
    public double meanMotion;

    public static bool TryParse(string satelliteName, string line1, string line2, out SatelliteTleData data, out string warning)
    {
        data = new SatelliteTleData
        {
            satelliteName = (satelliteName ?? string.Empty).Trim(),
            line1 = (line1 ?? string.Empty).Trim(),
            line2 = (line2 ?? string.Empty).Trim()
        };

        warning = string.Empty;

        if (string.IsNullOrWhiteSpace(data.satelliteName))
        {
            warning = "Satellite name is empty.";
            return false;
        }

        if (!IsValidLine(data.line1, '1', 69, out warning))
        {
            warning = $"{data.satelliteName}: malformed TLE line 1. {warning}";
            return false;
        }

        if (!IsValidLine(data.line2, '2', 69, out warning))
        {
            warning = $"{data.satelliteName}: malformed TLE line 2. {warning}";
            return false;
        }

        TryParseInt(Slice(data.line1, 2, 5), out data.noradCatalogId, out data.hasNoradCatalogId);
        TryParseClassification(Slice(data.line1, 7, 1), out data.classification, out data.hasClassification);
        TryParseEpoch(Slice(data.line1, 18, 14), out data.epoch, out data.hasEpoch);

        TryParseDouble(Slice(data.line2, 8, 8), out data.inclination, out data.hasInclination);
        TryParseDouble(Slice(data.line2, 17, 8), out data.raan, out data.hasRaan);
        TryParseEccentricity(Slice(data.line2, 26, 7), out data.eccentricity, out data.hasEccentricity);
        TryParseDouble(Slice(data.line2, 34, 8), out data.argumentOfPerigee, out data.hasArgumentOfPerigee);
        TryParseDouble(Slice(data.line2, 43, 8), out data.meanAnomaly, out data.hasMeanAnomaly);
        TryParseDouble(Slice(data.line2, 52, 11), out data.meanMotion, out data.hasMeanMotion);

        if (!data.hasNoradCatalogId || !data.hasEpoch || !data.hasInclination || !data.hasRaan ||
            !data.hasEccentricity || !data.hasArgumentOfPerigee || !data.hasMeanAnomaly || !data.hasMeanMotion)
        {
            warning = $"{data.satelliteName}: TLE loaded, but one or more optional orbital fields could not be parsed.";
        }

        return true;
    }

    static bool IsValidLine(string line, char expectedPrefix, int recommendedLength, out string warning)
    {
        warning = string.Empty;

        if (string.IsNullOrWhiteSpace(line))
        {
            warning = "Line is empty.";
            return false;
        }

        if (line[0] != expectedPrefix)
        {
            warning = $"Expected line to start with '{expectedPrefix}'.";
            return false;
        }

        if (line.Length < recommendedLength)
        {
            warning = $"Expected at least {recommendedLength} characters, got {line.Length}.";
            return false;
        }

        return true;
    }

    static string Slice(string value, int startIndex, int length)
    {
        if (string.IsNullOrEmpty(value) || startIndex >= value.Length)
        {
            return string.Empty;
        }

        int safeLength = Math.Min(length, value.Length - startIndex);
        return value.Substring(startIndex, safeLength);
    }

    static void TryParseInt(string value, out int parsed, out bool success)
    {
        success = int.TryParse(value.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed);
    }

    static void TryParseClassification(string value, out string parsed, out bool success)
    {
        parsed = value.Trim();
        success = parsed.Length > 0;
    }

    static void TryParseDouble(string value, out double parsed, out bool success)
    {
        success = double.TryParse(value.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out parsed);
    }

    static void TryParseEccentricity(string value, out double parsed, out bool success)
    {
        string normalized = "0." + value.Trim();
        success = double.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out parsed);
    }

    static void TryParseEpoch(string value, out string parsed, out bool success)
    {
        parsed = string.Empty;
        success = false;

        string trimmed = value.Trim();
        if (trimmed.Length < 5)
        {
            return;
        }

        if (!int.TryParse(trimmed.Substring(0, 2), NumberStyles.Integer, CultureInfo.InvariantCulture, out int year))
        {
            return;
        }

        if (!double.TryParse(trimmed.Substring(2), NumberStyles.Float, CultureInfo.InvariantCulture, out double dayOfYear))
        {
            return;
        }

        int fullYear = year < 57 ? 2000 + year : 1900 + year;
        DateTime epochUtc = new DateTime(fullYear, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddDays(dayOfYear - 1d);
        parsed = epochUtc.ToString("o", CultureInfo.InvariantCulture);
        success = true;
    }
}
