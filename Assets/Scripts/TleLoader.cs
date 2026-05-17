using System.Collections.Generic;
using System.IO;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class TleLoader : MonoBehaviour
{
    [Tooltip("Path under StreamingAssets used when no explicit TextAsset is assigned.")]
    public string streamingAssetsRelativePath = "TLE/leo-sample.tle";

    [Tooltip("When enabled, Play mode downloads the configured satellite TLE before falling back to StreamingAssets.")]
    public bool downloadTleAtRuntime = true;

    [Tooltip("CelesTrak GP query for the selected real satellite. Default is NOAA 19, NORAD 33591.")]
    public string tleUrl = "https://celestrak.org/NORAD/elements/gp.php?CATNR=33591&FORMAT=TLE";

    [Tooltip("Use the local StreamingAssets file if the runtime URL cannot be reached.")]
    public bool fallbackToStreamingAssets = true;

    [Tooltip("Optional editor/test override. If assigned, this text is loaded instead of StreamingAssets.")]
    public TextAsset tleTextOverride;

    [Tooltip("Load the configured TLE source in Awake.")]
    public bool loadOnAwake = true;

    [SerializeField] List<SatelliteTleData> satellites = new List<SatelliteTleData>();
    [SerializeField] string lastLoadedSource;

    public IReadOnlyList<SatelliteTleData> Satellites => satellites;
    public string LastLoadedSource => lastLoadedSource;

    void Awake()
    {
        if (loadOnAwake)
        {
            if (downloadTleAtRuntime && Application.isPlaying)
            {
                StartCoroutine(LoadConfiguredSourceAsync());
            }
            else
            {
                LoadConfiguredSource();
            }
        }
    }

    public void LoadConfiguredSource()
    {
        if (tleTextOverride != null)
        {
            LoadFromText(tleTextOverride.text, tleTextOverride.name);
            return;
        }

        if (downloadTleAtRuntime && Application.isPlaying)
        {
            Debug.LogWarning("Runtime TLE URL loading is asynchronous. Use LoadConfiguredSourceAsync when downloadTleAtRuntime is enabled.");
        }

        string fullPath = Path.Combine(Application.streamingAssetsPath, streamingAssetsRelativePath);
        LoadFromStreamingAssetsPath(fullPath);
    }

    public IEnumerator LoadConfiguredSourceAsync(System.Action<bool> onComplete = null)
    {
        if (tleTextOverride != null)
        {
            LoadFromText(tleTextOverride.text, tleTextOverride.name);
            onComplete?.Invoke(satellites.Count > 0);
            yield break;
        }

        if (downloadTleAtRuntime && !string.IsNullOrWhiteSpace(tleUrl))
        {
            using (UnityWebRequest request = UnityWebRequest.Get(tleUrl))
            {
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success && !string.IsNullOrWhiteSpace(request.downloadHandler.text))
                {
                    LoadFromText(request.downloadHandler.text, tleUrl);
                    onComplete?.Invoke(satellites.Count > 0);
                    yield break;
                }

                Debug.LogWarning($"TLE download failed from {tleUrl}: {request.error}");
            }
        }

        if (fallbackToStreamingAssets)
        {
            string fullPath = Path.Combine(Application.streamingAssetsPath, streamingAssetsRelativePath);
            LoadFromStreamingAssetsPath(fullPath);
            onComplete?.Invoke(satellites.Count > 0);
            yield break;
        }

        satellites.Clear();
        lastLoadedSource = string.Empty;
        onComplete?.Invoke(false);
    }

    public void LoadFromStreamingAssetsPath(string fullPath)
    {
        if (!File.Exists(fullPath))
        {
            Debug.LogWarning($"TLE loader could not find file: {fullPath}");
            satellites.Clear();
            lastLoadedSource = string.Empty;
            return;
        }

        LoadFromText(File.ReadAllText(fullPath), fullPath);
    }

    public List<SatelliteTleData> LoadFromText(string tleText, string sourceName = "TLE text")
    {
        satellites = ParseThreeLineTles(tleText, sourceName);
        for (int i = 0; i < satellites.Count; i++)
        {
            satellites[i].dataSource = sourceName;
        }

        lastLoadedSource = sourceName;
        Debug.Log($"Loaded {satellites.Count} TLE satellite entries from {sourceName}.");
        return satellites;
    }

    public static List<SatelliteTleData> ParseThreeLineTles(string tleText, string sourceName = "TLE text")
    {
        var parsedSatellites = new List<SatelliteTleData>();
        if (string.IsNullOrWhiteSpace(tleText))
        {
            Debug.LogWarning($"{sourceName}: TLE source is empty.");
            return parsedSatellites;
        }

        List<string> lines = NormalizeLines(tleText);
        for (int i = 0; i < lines.Count;)
        {
            int sourceLineNumber = i + 1;

            if (i + 2 >= lines.Count)
            {
                Debug.LogWarning($"{sourceName}:{sourceLineNumber}: Incomplete TLE entry. Expected name, line 1, and line 2.");
                break;
            }

            string satelliteName = lines[i];
            string line1 = lines[i + 1];
            string line2 = lines[i + 2];

            if (!line1.StartsWith("1 ") || !line2.StartsWith("2 "))
            {
                Debug.LogWarning($"{sourceName}:{sourceLineNumber}: Malformed 3-line TLE entry for '{satelliteName}'. Expected line 1 and line 2 after the satellite name.");
                i++;
                continue;
            }

            if (SatelliteTleData.TryParse(satelliteName, line1, line2, out SatelliteTleData satellite, out string warning))
            {
                parsedSatellites.Add(satellite);
                if (!string.IsNullOrEmpty(warning))
                {
                    Debug.LogWarning($"{sourceName}:{sourceLineNumber}: {warning}");
                }
            }
            else
            {
                Debug.LogWarning($"{sourceName}:{sourceLineNumber}: {warning}");
            }

            i += 3;
        }

        return parsedSatellites;
    }

    static List<string> NormalizeLines(string tleText)
    {
        var lines = new List<string>();
        using (var reader = new StringReader(tleText))
        {
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                string trimmed = line.Trim();
                if (trimmed.Length == 0 || trimmed.StartsWith("#"))
                {
                    continue;
                }

                lines.Add(trimmed);
            }
        }

        return lines;
    }
}
