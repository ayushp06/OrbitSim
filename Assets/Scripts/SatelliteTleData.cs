using System;
using System.Globalization;

[Serializable]
public class SatelliteTleData
{
    public string satelliteName;
    public string line1;
    public string line2;
    public string dataSource;

    public bool hasNoradCatalogId;
    public int noradCatalogId;

    public bool hasInternationalDesignator;
    public string internationalDesignator;

    public bool hasClassification;
    public string classification;

    public bool hasCountryOfOrigin;
    public string countryOfOrigin;

    public bool hasOwnerOperator;
    public string ownerOperator;

    public bool hasMission;
    public string mission;

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
        TryParseInternationalDesignator(Slice(data.line1, 9, 8), out data.internationalDesignator, out data.hasInternationalDesignator);
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

        data.ApplyKnownCatalogMetadata();
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

    static void TryParseString(string value, out string parsed, out bool success)
    {
        parsed = value.Trim();
        success = parsed.Length > 0;
    }

    static void TryParseInternationalDesignator(string value, out string parsed, out bool success)
    {
        parsed = value.Trim();
        success = parsed.Length > 0;

        if (!success || parsed.Length < 5)
        {
            return;
        }

        string launchYear = parsed.Substring(0, 2);
        string launchNumber = parsed.Substring(2, 3);
        string piece = parsed.Substring(5).Trim();

        if (!int.TryParse(launchYear, NumberStyles.Integer, CultureInfo.InvariantCulture, out int shortYear))
        {
            return;
        }

        int fullYear = shortYear < 57 ? 2000 + shortYear : 1900 + shortYear;
        parsed = $"{fullYear}-{launchNumber}{piece}";
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

    void ApplyKnownCatalogMetadata()
    {
        if (!hasNoradCatalogId)
        {
            return;
        }

        switch (noradCatalogId)
        {
            case 25544:
                SetMetadata("International", "International Space Station partners", "Crewed low Earth orbit space station");
                break;
            case 20580:
                SetMetadata("United States", "NASA / ESA", "Hubble Space Telescope");
                break;
            case 25994:
                SetMetadata("United States", "NASA / JAXA / ASTER partners", "Terra Earth-observing satellite");
                break;
            case 27424:
                SetMetadata("United States", "NASA", "Aqua Earth-observing satellite");
                break;
            case 28376:
                SetMetadata("United States", "NASA", "Aura atmospheric chemistry research satellite");
                break;
            case 29108:
                SetMetadata("United States / France", "NASA / CNES", "CALIPSO cloud and aerosol lidar mission");
                break;
            case 33591:
                SetMetadata("United States", "NOAA", "NOAA 19 polar-orbiting weather satellite");
                break;
            case 37849:
                SetMetadata("United States", "NOAA / NASA", "Suomi NPP weather and climate observation satellite");
                break;
            case 39084:
                SetMetadata("United States", "NASA / USGS", "Landsat 8 Earth-imaging satellite");
                break;
            case 40697:
                SetMetadata("European Union", "European Space Agency", "Sentinel-2A multispectral Earth-observation satellite");
                break;
        }
    }

    void SetMetadata(string country, string operatorName, string missionDescription)
    {
        countryOfOrigin = country;
        hasCountryOfOrigin = !string.IsNullOrWhiteSpace(countryOfOrigin);

        ownerOperator = operatorName;
        hasOwnerOperator = !string.IsNullOrWhiteSpace(ownerOperator);

        mission = missionDescription;
        hasMission = !string.IsNullOrWhiteSpace(mission);
    }
}
