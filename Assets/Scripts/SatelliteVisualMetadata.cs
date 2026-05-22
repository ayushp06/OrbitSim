using System;
using System.Collections.Generic;
using UnityEngine;

public static class SatelliteVisualMetadata
{
    public struct LegendEntry
    {
        public readonly string label;
        public readonly Color color;

        public LegendEntry(string label, Color color)
        {
            this.label = label;
            this.color = color;
        }
    }

    public static readonly LegendEntry[] LegendEntries =
    {
        new LegendEntry("North America", new Color(0.18f, 0.72f, 1f, 1f)),
        new LegendEntry("South America", new Color(0.1f, 0.85f, 0.42f, 1f)),
        new LegendEntry("Europe", new Color(0.95f, 0.72f, 0.18f, 1f)),
        new LegendEntry("Asia", new Color(1f, 0.36f, 0.28f, 1f)),
        new LegendEntry("Oceania", new Color(0.66f, 0.48f, 1f, 1f)),
        new LegendEntry("International", new Color(0.92f, 0.92f, 0.96f, 1f)),
        new LegendEntry("Unknown", new Color(0.55f, 0.6f, 0.65f, 1f)),
    };

    static readonly Dictionary<string, OwnerInfo> ownerInfoByCode = new Dictionary<string, OwnerInfo>(StringComparer.OrdinalIgnoreCase)
    {
        { "ARGN", new OwnerInfo("Argentina", "Argentina") },
        { "AUS", new OwnerInfo("Australia", "Australia") },
        { "BRAZ", new OwnerInfo("Brazil", "Brazil") },
        { "CA", new OwnerInfo("Canada", "Canada") },
        { "CIS", new OwnerInfo("Russia / CIS", "Commonwealth of Independent States") },
        { "ESA", new OwnerInfo("European Union", "European Space Agency") },
        { "FR", new OwnerInfo("France", "France") },
        { "GER", new OwnerInfo("Germany", "Germany") },
        { "IND", new OwnerInfo("India", "India") },
        { "INDO", new OwnerInfo("Indonesia", "Indonesia") },
        { "IRAN", new OwnerInfo("Iran", "Iran") },
        { "ISRA", new OwnerInfo("Israel", "Israel") },
        { "ISS", new OwnerInfo("International", "International Space Station partners") },
        { "IT", new OwnerInfo("Italy", "Italy") },
        { "JPN", new OwnerInfo("Japan", "Japan") },
        { "GLOB", new OwnerInfo("International", "Globalstar") },
        { "ORB", new OwnerInfo("United States", "ORBCOMM") },
        { "PRC", new OwnerInfo("China", "China") },
        { "SAUD", new OwnerInfo("Saudi Arabia", "Saudi Arabia") },
        { "SKOR", new OwnerInfo("South Korea", "South Korea") },
        { "SPN", new OwnerInfo("Spain", "Spain") },
        { "SWED", new OwnerInfo("Sweden", "Sweden") },
        { "THAI", new OwnerInfo("Thailand", "Thailand") },
        { "UK", new OwnerInfo("United Kingdom", "United Kingdom") },
        { "US", new OwnerInfo("United States", "United States") },
    };

    public static bool TryGetCatalogMetadata(int noradCatalogId, out string country, out string ownerOperator, out string mission)
    {
        country = string.Empty;
        ownerOperator = string.Empty;
        mission = string.Empty;

        switch (noradCatalogId)
        {
            case 900: return BuildMetadata("US", "CALSPHERE 1", out country, out ownerOperator, out mission);
            case 902: return BuildMetadata("US", "CALSPHERE 2", out country, out ownerOperator, out mission);
            case 1512: return BuildMetadata("US", "TEMPSAT 1", out country, out ownerOperator, out mission);
            case 1520: return BuildMetadata("US", "CALSPHERE 4A", out country, out ownerOperator, out mission);
            case 2826: return BuildMetadata("US", "OPS 5712 (P/L 160)", out country, out ownerOperator, out mission);
            case 2872: return BuildMetadata("US", "SURCAL 159", out country, out ownerOperator, out mission);
            case 2874: return BuildMetadata("US", "OPS 5712 (P/L 153)", out country, out ownerOperator, out mission);
            case 5398: return BuildMetadata("US", "RIGIDSPHERE 2 (LCS 4)", out country, out ownerOperator, out mission);
            case 7530: return BuildMetadata("US", "OSCAR 7 (AO-7)", out country, out ownerOperator, out mission);
            case 7646: return BuildMetadata("FR", "STARLETTE", out country, out ownerOperator, out mission);
            case 14781: return BuildMetadata("UK", "UOSAT 2 (UO-11)", out country, out ownerOperator, out mission);
            case 16908: return BuildMetadata("JPN", "AJISAI (EGS)", out country, out ownerOperator, out mission);
            case 20442: return BuildMetadata("ARGN", "LUSAT (LO-19)", out country, out ownerOperator, out mission);
            case 20580: return BuildMetadata("US", "HST", out country, out ownerOperator, out mission);
            case 22490: return BuildMetadata("BRAZ", "SCD 1", out country, out ownerOperator, out mission);
            case 22824: return BuildMetadata("FR", "STELLA", out country, out ownerOperator, out mission);
            case 22825: return BuildMetadata("US", "EYESAT A (AO-27)", out country, out ownerOperator, out mission);
            case 22826: return BuildMetadata("IT", "ITAMSAT (IO-26)", out country, out ownerOperator, out mission);
            case 23439: return BuildMetadata("CIS", "RADIO ROSTO (RS15)", out country, out ownerOperator, out mission);
            case 23893: return BuildMetadata("US", "USA 119", out country, out ownerOperator, out mission);
            case 24278: return BuildMetadata("JPN", "JAS-2 (FO-29)", out country, out ownerOperator, out mission);
            case 24920: return BuildMetadata("US", "FORTE", out country, out ownerOperator, out mission);
            case 25118: return BuildMetadata("ORB", "ORBCOMM FM06", out country, out ownerOperator, out mission);
            case 25159: return BuildMetadata("ORB", "ORBCOMM FM04", out country, out ownerOperator, out mission);
            case 25160: return BuildMetadata("US", "CELESTIS-02 & TAURUS R/B", out country, out ownerOperator, out mission);
            case 25397: return BuildMetadata("ISRA", "TECHSAT 1B (GO-32)", out country, out ownerOperator, out mission);
            case 25398: return BuildMetadata("AUS", "WESTPAC", out country, out ownerOperator, out mission);
            case 25415: return BuildMetadata("ORB", "ORBCOMM FM19", out country, out ownerOperator, out mission);
            case 25416: return BuildMetadata("ORB", "ORBCOMM FM20", out country, out ownerOperator, out mission);
            case 25481: return BuildMetadata("ORB", "ORBCOMM FM27", out country, out ownerOperator, out mission);
            case 25504: return BuildMetadata("BRAZ", "SCD 2", out country, out ownerOperator, out mission);
            case 25544: return BuildMetadata("ISS", "ISS (ZARYA)", out country, out ownerOperator, out mission);
            case 25560: return BuildMetadata("US", "SWAS", out country, out ownerOperator, out mission);
            case 25575: return BuildMetadata("ISS", "ISS (UNITY)", out country, out ownerOperator, out mission);
            case 25757: return BuildMetadata("GER", "DLR-TUBSAT", out country, out ownerOperator, out mission);
            case 25982: return BuildMetadata("ORB", "ORBCOMM FM32", out country, out ownerOperator, out mission);
            case 25994: return BuildMetadata("US", "TERRA", out country, out ownerOperator, out mission);
            case 26400: return BuildMetadata("ISS", "ISS (ZVEZDA)", out country, out ownerOperator, out mission);
            case 26700: return BuildMetadata("ISS", "ISS (DESTINY)", out country, out ownerOperator, out mission);
            case 26702: return BuildMetadata("SWED", "ODIN", out country, out ownerOperator, out mission);
            case 26931: return BuildMetadata("US", "PCSAT (NO-44)", out country, out ownerOperator, out mission);
            case 26958: return BuildMetadata("ESA", "PROBA-1", out country, out ownerOperator, out mission);
            case 26998: return BuildMetadata("US", "TIMED", out country, out ownerOperator, out mission);
            case 27004: return BuildMetadata("GER", "MAROC-TUBSAT", out country, out ownerOperator, out mission);
            case 27056: return BuildMetadata("CIS", "COSMOS 2385", out country, out ownerOperator, out mission);
            case 27057: return BuildMetadata("CIS", "COSMOS 2386", out country, out ownerOperator, out mission);
            case 27424: return BuildMetadata("US", "AQUA", out country, out ownerOperator, out mission);
            case 27464: return BuildMetadata("CIS", "COSMOS 2390", out country, out ownerOperator, out mission);
            case 27465: return BuildMetadata("CIS", "COSMOS 2391", out country, out ownerOperator, out mission);
            case 27606: return BuildMetadata("ARGN", "LATINSAT B", out country, out ownerOperator, out mission);
            case 27607: return BuildMetadata("SAUD", "SAUDISAT 1C (SO-50)", out country, out ownerOperator, out mission);
            case 27612: return BuildMetadata("ARGN", "LATINSAT A", out country, out ownerOperator, out mission);
            case 27640: return BuildMetadata("US", "CORIOLIS", out country, out ownerOperator, out mission);
            case 27651: return BuildMetadata("US", "SORCE", out country, out ownerOperator, out mission);
            case 27843: return BuildMetadata("CA", "MOST", out country, out ownerOperator, out mission);
            case 27844: return BuildMetadata("JPN", "CUTE-1 (CO-55)", out country, out ownerOperator, out mission);
            case 27848: return BuildMetadata("JPN", "CUBESAT XI-IV (CO-57)", out country, out ownerOperator, out mission);
            case 27858: return BuildMetadata("CA", "SCISAT 1", out country, out ownerOperator, out mission);
            case 27868: return BuildMetadata("CIS", "COSMOS 2400", out country, out ownerOperator, out mission);
            case 27869: return BuildMetadata("CIS", "COSMOS 2401", out country, out ownerOperator, out mission);
            case 27939: return BuildMetadata("CIS", "MOZHAETS 4 (RS22)", out country, out ownerOperator, out mission);
            case 27944: return BuildMetadata("CIS", "LARETS", out country, out ownerOperator, out mission);
            case 28054: return BuildMetadata("US", "DMSP 5D-3 F16 (USA 172)", out country, out ownerOperator, out mission);
            case 28058: return BuildMetadata("PRC", "CHUANGXIN 1 (CX-1)", out country, out ownerOperator, out mission);
            case 28220: return BuildMetadata("PRC", "SHIYAN-1 (SY-1)", out country, out ownerOperator, out mission);
            case 28366: return BuildMetadata("US", "APRIZESAT 2", out country, out ownerOperator, out mission);
            case 28369: return BuildMetadata("SAUD", "SAUDICOMSAT 1", out country, out ownerOperator, out mission);
            case 28370: return BuildMetadata("SAUD", "SAUDICOMSAT 2", out country, out ownerOperator, out mission);
            case 28371: return BuildMetadata("SAUD", "SAUDISAT 2", out country, out ownerOperator, out mission);
            case 28372: return BuildMetadata("US", "APRIZESAT 1", out country, out ownerOperator, out mission);
            case 28376: return BuildMetadata("US", "AURA", out country, out ownerOperator, out mission);
            case 28380: return BuildMetadata("CIS", "COSMOS 2407", out country, out ownerOperator, out mission);
            case 28413: return BuildMetadata("PRC", "SHIJIAN-6 01A (SJ-6 01A)", out country, out ownerOperator, out mission);
            case 28414: return BuildMetadata("PRC", "SHIJIAN-6 01B (SJ-6 01B)", out country, out ownerOperator, out mission);
            case 28419: return BuildMetadata("CIS", "COSMOS 2408", out country, out ownerOperator, out mission);
            case 28420: return BuildMetadata("CIS", "COSMOS 2409", out country, out ownerOperator, out mission);
            case 28470: return BuildMetadata("PRC", "JB-3 3 (ZY 2C)", out country, out ownerOperator, out mission);
            case 28485: return BuildMetadata("US", "SWIFT", out country, out ownerOperator, out mission);
            case 28493: return BuildMetadata("SPN", "NANOSAT-1", out country, out ownerOperator, out mission);
            case 28521: return BuildMetadata("CIS", "COSMOS 2414", out country, out ownerOperator, out mission);
            case 28649: return BuildMetadata("IND", "IRS-P5 (CARTOSAT-1)", out country, out ownerOperator, out mission);
            case 28737: return BuildMetadata("PRC", "SJ-7", out country, out ownerOperator, out mission);
            case 28810: return BuildMetadata("JPN", "REIMEI (INDEX)", out country, out ownerOperator, out mission);
            case 28890: return BuildMetadata("PRC", "BEIJING 1", out country, out ownerOperator, out mission);
            case 28893: return BuildMetadata("IRAN", "SINAH 1", out country, out ownerOperator, out mission);
            case 28895: return BuildMetadata("JPN", "CUBESAT XI-V", out country, out ownerOperator, out mission);
            case 28908: return BuildMetadata("CIS", "COSMOS 2416 (RODNIK-S 1)", out country, out ownerOperator, out mission);
            case 29108: return BuildMetadata("FR", "CALIPSO", out country, out ownerOperator, out mission);
            case 29228: return BuildMetadata("CIS", "RESURS-DK 1", out country, out ownerOperator, out mission);
            case 29268: return BuildMetadata("SKOR", "ARIRANG-2 (KOMPSAT-2)", out country, out ownerOperator, out mission);
            case 29479: return BuildMetadata("JPN", "HINODE (SOLAR-B)", out country, out ownerOperator, out mission);
            case 29505: return BuildMetadata("PRC", "SHIJIAN-6 02A (SJ-6 02A)", out country, out ownerOperator, out mission);
            case 29506: return BuildMetadata("PRC", "SHIJIAN-6 02B (SJ-6 02B)", out country, out ownerOperator, out mission);
            case 29522: return BuildMetadata("US", "DMSP 5D-3 F17 (USA 191)", out country, out ownerOperator, out mission);
            case 29709: return BuildMetadata("INDO", "LAPAN-TUBSAT", out country, out ownerOperator, out mission);
            case 31113: return BuildMetadata("PRC", "HAIYANG-1B", out country, out ownerOperator, out mission);
            case 31118: return BuildMetadata("SAUD", "SAUDISAT 3", out country, out ownerOperator, out mission);
            case 31119: return BuildMetadata("SAUD", "SAUDICOMSAT 7", out country, out ownerOperator, out mission);
            case 31121: return BuildMetadata("SAUD", "SAUDICOMSAT 6", out country, out ownerOperator, out mission);
            case 31124: return BuildMetadata("SAUD", "SAUDICOMSAT 5", out country, out ownerOperator, out mission);
            case 31125: return BuildMetadata("SAUD", "SAUDICOMSAT 3", out country, out ownerOperator, out mission);
            case 31127: return BuildMetadata("SAUD", "SAUDICOMSAT 4", out country, out ownerOperator, out mission);
            case 31490: return BuildMetadata("PRC", "YAOGAN-2", out country, out ownerOperator, out mission);
            case 31573: return BuildMetadata("GLOB", "GLOBALSTAR M069", out country, out ownerOperator, out mission);
            case 31574: return BuildMetadata("GLOB", "GLOBALSTAR M072", out country, out ownerOperator, out mission);
            case 31598: return BuildMetadata("IT", "COSMO-SKYMED 1", out country, out ownerOperator, out mission);
            case 31698: return BuildMetadata("GER", "TERRASAR-X", out country, out ownerOperator, out mission);
            case 31792: return BuildMetadata("CIS", "COSMOS 2428", out country, out ownerOperator, out mission);
            case 31797: return BuildMetadata("GER", "SAR-LUPE 2", out country, out ownerOperator, out mission);
            case 32060: return BuildMetadata("US", "WORLDVIEW-1 (WV-1)", out country, out ownerOperator, out mission);
            case 32265: return BuildMetadata("GLOB", "GLOBALSTAR M066", out country, out ownerOperator, out mission);
            case 32289: return BuildMetadata("PRC", "YAOGAN-3", out country, out ownerOperator, out mission);
            case 32376: return BuildMetadata("IT", "COSMO-SKYMED 2", out country, out ownerOperator, out mission);
            case 32382: return BuildMetadata("CA", "RADARSAT-2", out country, out ownerOperator, out mission);
            case 32783: return BuildMetadata("IND", "CARTOSAT-2A", out country, out ownerOperator, out mission);
            case 32785: return BuildMetadata("JPN", "CUTE-1.7+APD II (CO-65)", out country, out ownerOperator, out mission);
            case 32790: return BuildMetadata("CA", "CANX-2", out country, out ownerOperator, out mission);
            case 32791: return BuildMetadata("JPN", "SEEDS II (CO-66)", out country, out ownerOperator, out mission);
            case 32953: return BuildMetadata("CIS", "YUBILEINY (RS30)", out country, out ownerOperator, out mission);
            case 32954: return BuildMetadata("CIS", "COSMOS 2437", out country, out ownerOperator, out mission);
            case 32955: return BuildMetadata("CIS", "COSMOS 2438", out country, out ownerOperator, out mission);
            case 32956: return BuildMetadata("CIS", "COSMOS 2439", out country, out ownerOperator, out mission);
            case 32958: return BuildMetadata("PRC", "FENGYUN 3A", out country, out ownerOperator, out mission);
            case 33053: return BuildMetadata("US", "FGRST (GLAST)", out country, out ownerOperator, out mission);
            case 33320: return BuildMetadata("PRC", "HUANJING 1A (HJ-1A)", out country, out ownerOperator, out mission);
            case 33321: return BuildMetadata("PRC", "HUANJING 1B (HJ-1B)", out country, out ownerOperator, out mission);
            case 33331: return BuildMetadata("US", "GEOEYE 1", out country, out ownerOperator, out mission);
            case 33396: return BuildMetadata("THAI", "THEOS", out country, out ownerOperator, out mission);
            case 33408: return BuildMetadata("PRC", "SHIJIAN-6 03A (SJ-6 03A)", out country, out ownerOperator, out mission);
            case 33409: return BuildMetadata("PRC", "SHIJIAN-6 03B (SJ-6 03B)", out country, out ownerOperator, out mission);
            case 33412: return BuildMetadata("IT", "COSMO-SKYMED 3", out country, out ownerOperator, out mission);
            case 33433: return BuildMetadata("PRC", "SHIYAN-3 (SY-3)", out country, out ownerOperator, out mission);
            case 33434: return BuildMetadata("PRC", "CHUANGXIN 1-02 (CX-1-02)", out country, out ownerOperator, out mission);
            case 33446: return BuildMetadata("PRC", "YAOGAN-4", out country, out ownerOperator, out mission);
            case 33492: return BuildMetadata("JPN", "GOSAT (IBUKI)", out country, out ownerOperator, out mission);
            case 33498: return BuildMetadata("JPN", "STARS (KUKAI)", out country, out ownerOperator, out mission);
            case 33591: return BuildMetadata("US", "NOAA 19", out country, out ownerOperator, out mission);
            case 37849: return BuildMetadata("US", "SUOMI NPP", out country, out ownerOperator, out mission);
            case 39084: return BuildMetadata("US", "LANDSAT 8", out country, out ownerOperator, out mission);
            case 39634: return BuildMetadata("ESA", "SENTINEL-1A", out country, out ownerOperator, out mission);
            case 40069: return BuildMetadata("CIS", "METEOR-M 2", out country, out ownerOperator, out mission);
            case 40697: return BuildMetadata("ESA", "SENTINEL-2A", out country, out ownerOperator, out mission);
            case 41884: return BuildMetadata("US", "CYGFM05", out country, out ownerOperator, out mission);
            case 42063: return BuildMetadata("ESA", "SENTINEL-2B", out country, out ownerOperator, out mission);
            case 42969: return BuildMetadata("ESA", "SENTINEL-5P", out country, out ownerOperator, out mission);
            case 43476: return BuildMetadata("US", "GRACE-FO 1", out country, out ownerOperator, out mission);
            case 43613: return BuildMetadata("US", "ICESAT-2", out country, out ownerOperator, out mission);
            case 48274: return BuildMetadata("PRC", "CSS (TIANHE)", out country, out ownerOperator, out mission);
            case 49260: return BuildMetadata("US", "LANDSAT 9", out country, out ownerOperator, out mission);
            case 54730: return BuildMetadata("JPN", "H-2A DEB", out country, out ownerOperator, out mission);
            default: return false;
        }
    }

    public static string GetContinent(SatelliteTleData data)
    {
        if (data == null || !data.hasCountryOfOrigin)
        {
            return "Unknown";
        }

        string[] countryCodes = GetCountryCodes(data);
        if (countryCodes.Length == 0)
        {
            return "Unknown";
        }

        if (countryCodes.Length > 1 || countryCodes[0] == "INTL")
        {
            return "International";
        }

        return GetContinentForCountryCode(countryCodes[0]);
    }

    public static Color GetContinentColor(SatelliteTleData data)
    {
        string continent = GetContinent(data);
        for (int i = 0; i < LegendEntries.Length; i++)
        {
            if (LegendEntries[i].label == continent)
            {
                return LegendEntries[i].color;
            }
        }

        return LegendEntries[LegendEntries.Length - 1].color;
    }

    public static string[] GetCountryCodes(SatelliteTleData data)
    {
        if (data == null || !data.hasCountryOfOrigin || string.IsNullOrWhiteSpace(data.countryOfOrigin))
        {
            return Array.Empty<string>();
        }

        string[] countries = data.countryOfOrigin.Split('/');
        var codes = new List<string>(countries.Length);
        for (int i = 0; i < countries.Length; i++)
        {
            string code = GetCountryCode(countries[i].Trim());
            if (!string.IsNullOrEmpty(code) && !codes.Contains(code))
            {
                codes.Add(code);
            }
        }

        return codes.ToArray();
    }

    static bool BuildMetadata(string ownerCode, string catalogName, out string country, out string ownerOperator, out string mission)
    {
        if (!ownerInfoByCode.TryGetValue(ownerCode, out OwnerInfo info))
        {
            country = ownerCode;
            ownerOperator = ownerCode;
        }
        else
        {
            country = info.country;
            ownerOperator = info.ownerOperator;
        }

        mission = catalogName;
        return true;
    }

    static string GetCountryCode(string country)
    {
        switch (country)
        {
            case "Argentina": return "AR";
            case "Australia": return "AU";
            case "Brazil": return "BR";
            case "Canada": return "CA";
            case "China": return "CN";
            case "European Union": return "EU";
            case "France": return "FR";
            case "Germany": return "DE";
            case "India": return "IN";
            case "Indonesia": return "ID";
            case "International": return "INTL";
            case "Iran": return "IR";
            case "Israel": return "IL";
            case "Italy": return "IT";
            case "Japan": return "JP";
            case "Russia":
            case "Russia / CIS": return "RU";
            case "Saudi Arabia": return "SA";
            case "South Korea": return "KR";
            case "Spain": return "ES";
            case "Sweden": return "SE";
            case "Thailand": return "TH";
            case "United Kingdom": return "GB";
            case "United States": return "US";
            default: return string.Empty;
        }
    }

    static string GetContinentForCountryCode(string countryCode)
    {
        switch (countryCode)
        {
            case "AR":
            case "BR":
                return "South America";
            case "AU":
                return "Oceania";
            case "CA":
            case "US":
                return "North America";
            case "DE":
            case "ES":
            case "EU":
            case "FR":
            case "GB":
            case "IT":
            case "SE":
                return "Europe";
            case "CN":
            case "IL":
            case "IN":
            case "IR":
            case "JP":
            case "KR":
            case "RU":
            case "SA":
            case "TH":
            case "ID":
                return "Asia";
            default:
                return "Unknown";
        }
    }

    struct OwnerInfo
    {
        public readonly string country;
        public readonly string ownerOperator;
        public OwnerInfo(string country, string ownerOperator)
        {
            this.country = country;
            this.ownerOperator = ownerOperator;
        }
    }
}
