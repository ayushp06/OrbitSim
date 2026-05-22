using System;
using System.Collections;
using System.Collections.Generic;
using SGPdotNET.Observation;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class SatelliteManager : MonoBehaviour
{
    const double EarthRadiusKilometers = 6371.0;

    [Header("Data")]
    public TleLoader tleLoader;

    [Header("Scene References")]
    public GameObject satelliteMarkerPrefab;
    public Transform earthReference;
    public Transform earthVisual;
    public Transform satellitesParent;
    public Transform orbitVisualsParent;

    [Header("Simulation")]
    public float earthRadiusWorldUnits = 10f;
    public float orbitAltitudeExaggeration = 8f;
    public float timeScale = 120f;
    public float simulationSpeedMultiplier = 1f;
    public bool simulationPaused;
    public int maxSatellitesForTesting = 1;
    public bool orbitLinesEnabled = false;

    [Header("Performance")]
    [Min(0f)]
    public float satellitePositionUpdateInterval = 0.05f;
    public int maxSatellitePositionUpdatesPerFrame = 0;

    [Header("LEO Filtering")]
    public bool showOnlyLeoSatellites = true;
    public double maxLeoAltitudeKilometers = 2000d;
    public double minLeoMeanMotionRevsPerDay = 11.25d;

    [Header("Desktop Time Controls")]
    public bool enableKeyboardTimeControls = true;
    public float speedStepMultiplier = 2f;

    [Header("Marker Rendering")]
    public float markerScale = 0.35f;
    public Material satelliteMarkerMaterial;
    public bool shareMarkerMaterial = true;

    [Header("Legend")]
    public bool showContinentLegend = true;
    public Vector2 legendScreenMargin = new Vector2(24f, 24f);
    public Color legendPanelColor = new Color(0.02f, 0.03f, 0.05f, 0.88f);
    public Color legendTextColor = Color.white;
    public int legendFontSize = 16;

    [Header("Orbit Rendering")]
    public int orbitLineSegments = 96;
    public int maxOrbitLines = 64;
    public float orbitLineWidth = 0.025f;
    public Color orbitLineColor = new Color(0.25f, 0.8f, 1f, 0.45f);
    public Material orbitLineMaterial;

    readonly List<RuntimeSatellite> satellites = new List<RuntimeSatellite>();
    DateTime simulationTimeUtc;
    Material sharedOrbitLineMaterial;
    Material sharedSatelliteMarkerMaterial;
    Transform cachedEarthReference;
    Transform cachedSatelliteParent;
    Transform cachedOrbitVisualsParent;
    GameObject continentLegendObject;
    float positionUpdateTimer;
    int nextPositionUpdateIndex;

    public DateTime SimulationTimeUtc => simulationTimeUtc;
    public IReadOnlyList<RuntimeSatellite> RuntimeSatellites => satellites;
    public float EffectiveTimeScale => simulationPaused ? 0f : timeScale * Mathf.Max(0f, simulationSpeedMultiplier);

    void Awake()
    {
        simulationTimeUtc = DateTime.UtcNow;
        CacheSceneReferences();
    }

    void Start()
    {
        ApplyEarthVisualScale();
        BuildContinentLegend();
        StartCoroutine(LoadAndSpawnSatellitesAsync());
    }

    void Update()
    {
        HandleKeyboardTimeControls();
        simulationTimeUtc = simulationTimeUtc.AddSeconds(Time.deltaTime * EffectiveTimeScale);
        TickSatellitePositionUpdates();
    }

    public void PlaySimulation()
    {
        simulationPaused = false;
    }

    public void PauseSimulation()
    {
        simulationPaused = true;
    }

    public void ToggleSimulationPaused()
    {
        simulationPaused = !simulationPaused;
    }

    public void SetSimulationSpeedMultiplier(float multiplier)
    {
        simulationSpeedMultiplier = Mathf.Max(0f, multiplier);
    }

    public void MultiplySimulationSpeed(float multiplier)
    {
        SetSimulationSpeedMultiplier(simulationSpeedMultiplier * Mathf.Max(0.0001f, multiplier));
    }

    public void LoadAndSpawnSatellites()
    {
        ClearRuntimeSatellites();
        CacheSceneReferences();
        ClearOrbitVisuals();

        if (tleLoader == null)
        {
            tleLoader = GetComponent<TleLoader>();
        }

        if (tleLoader == null)
        {
            Debug.LogWarning("SatelliteManager has no TleLoader assigned.");
            return;
        }

        tleLoader.LoadConfiguredSource();
        SpawnLoadedSatellites();
    }

    public IEnumerator LoadAndSpawnSatellitesAsync()
    {
        ClearRuntimeSatellites();
        CacheSceneReferences();
        ClearOrbitVisuals();

        if (tleLoader == null)
        {
            tleLoader = GetComponent<TleLoader>();
        }

        if (tleLoader == null)
        {
            Debug.LogWarning("SatelliteManager has no TleLoader assigned.");
            yield break;
        }

        yield return tleLoader.LoadConfiguredSourceAsync();
        SpawnLoadedSatellites();
    }

    void SpawnLoadedSatellites()
    {
        satellites.Capacity = Mathf.Max(satellites.Capacity, maxSatellitesForTesting > 0 ? maxSatellitesForTesting : tleLoader.Satellites.Count);

        int filteredOutCount = 0;
        int spawnedCount = 0;
        int maxCount = maxSatellitesForTesting > 0 ? maxSatellitesForTesting : int.MaxValue;

        for (int i = 0; i < tleLoader.Satellites.Count && spawnedCount < maxCount; i++)
        {
            SatelliteTleData tle = tleLoader.Satellites[i];
            if (showOnlyLeoSatellites && !RuntimeSatellite.IsLikelyLeo(tle, maxLeoAltitudeKilometers, minLeoMeanMotionRevsPerDay))
            {
                filteredOutCount++;
                continue;
            }

            var runtimeSatellite = new RuntimeSatellite(tle, CreateMarker(tle));
            runtimeSatellite.TryCreateSgp4Satellite();
            satellites.Add(runtimeSatellite);
            spawnedCount++;
        }

        if (showOnlyLeoSatellites)
        {
            Debug.Log($"SatelliteManager spawned {spawnedCount} likely LEO satellites and filtered out {filteredOutCount} non-LEO or unclassifiable entries.");
        }

        if (orbitLinesEnabled)
        {
            SetOrbitVisualsActive(true);
            BuildOrbitLines();
        }
        else
        {
            SetOrbitVisualsActive(false);
        }

        UpdateAllSatellitePositions();
    }

    void TickSatellitePositionUpdates()
    {
        if (satellites.Count == 0)
        {
            return;
        }

        positionUpdateTimer += Time.deltaTime;
        if (satellitePositionUpdateInterval > 0f && positionUpdateTimer < satellitePositionUpdateInterval)
        {
            return;
        }

        positionUpdateTimer = 0f;

        if (maxSatellitePositionUpdatesPerFrame > 0 && maxSatellitePositionUpdatesPerFrame < satellites.Count)
        {
            // Large catalogs can spread propagation across frames. This trades perfect simultaneity for steadier frame time.
            UpdateSatellitePositionsBatch(maxSatellitePositionUpdatesPerFrame);
            return;
        }

        UpdateAllSatellitePositions();
    }

    void UpdateAllSatellitePositions()
    {
        Vector3 origin = GetEarthOrigin();

        for (int i = 0; i < satellites.Count; i++)
        {
            UpdateSatellitePosition(satellites[i], origin);
        }
    }

    void UpdateSatellitePositionsBatch(int maxUpdates)
    {
        Vector3 origin = GetEarthOrigin();
        int updates = Mathf.Min(maxUpdates, satellites.Count);

        for (int i = 0; i < updates; i++)
        {
            if (nextPositionUpdateIndex >= satellites.Count)
            {
                nextPositionUpdateIndex = 0;
            }

            UpdateSatellitePosition(satellites[nextPositionUpdateIndex], origin);
            nextPositionUpdateIndex++;
        }
    }

    void UpdateSatellitePosition(RuntimeSatellite satellite, Vector3 origin)
    {
        if (satellite.MarkerTransform == null)
        {
            return;
        }

        satellite.MarkerTransform.position = origin + ScalePositionForVisualization(satellite.GetPositionKilometers(simulationTimeUtc));
    }

    void BuildOrbitLines()
    {
        int lineCount = Mathf.Min(satellites.Count, Mathf.Max(0, maxOrbitLines));
        for (int i = 0; i < lineCount; i++)
        {
            RuntimeSatellite satellite = satellites[i];
            LineRenderer line = CreateOrbitLine(satellite.TleData);
            satellite.OrbitLine = line;
            WriteApproximateOrbitLine(satellite, line);
        }

        if (satellites.Count > lineCount)
        {
            Debug.Log($"Orbit lines limited to {lineCount} of {satellites.Count} satellites for performance.");
        }
    }

    void ClearOrbitVisuals()
    {
        if (cachedOrbitVisualsParent == null)
        {
            return;
        }

        // The point-mass demo should not show paths. Clear any saved/generated orbit children before spawning.
        for (int i = cachedOrbitVisualsParent.childCount - 1; i >= 0; i--)
        {
            DestroyRuntimeObject(cachedOrbitVisualsParent.GetChild(i).gameObject);
        }
    }

    void SetOrbitVisualsActive(bool active)
    {
        if (cachedOrbitVisualsParent != null)
        {
            cachedOrbitVisualsParent.gameObject.SetActive(active);
        }
    }

    void WriteApproximateOrbitLine(RuntimeSatellite satellite, LineRenderer line)
    {
        int segments = Mathf.Max(12, orbitLineSegments);
        line.positionCount = segments + 1;

        Vector3 origin = GetEarthOrigin();
        DateTime epoch = satellite.GetEpochOrDefault(simulationTimeUtc);
        double periodMinutes = satellite.GetOrbitalPeriodMinutes();

        for (int i = 0; i <= segments; i++)
        {
            double t = (double)i / segments;
            DateTime sampleTime = epoch.AddMinutes(periodMinutes * t);
            line.SetPosition(i, origin + ScalePositionForVisualization(satellite.GetApproximatePositionKilometers(sampleTime)));
        }
    }

    void ApplyEarthVisualScale()
    {
        Transform visual = earthVisual;
        if (visual == null && earthReference != null && earthReference.childCount > 0)
        {
            visual = earthReference.GetChild(0);
        }

        if (visual != null)
        {
            visual.localScale = Vector3.one * earthRadiusWorldUnits * 2f;
        }
    }

    void CacheSceneReferences()
    {
        cachedEarthReference = earthReference != null ? earthReference : transform;
        cachedSatelliteParent = satellitesParent != null ? satellitesParent : transform;
        cachedOrbitVisualsParent = orbitVisualsParent != null ? orbitVisualsParent : transform;
    }

    void HandleKeyboardTimeControls()
    {
        if (!enableKeyboardTimeControls || Keyboard.current == null)
        {
            return;
        }

        if (Keyboard.current.pKey.wasPressedThisFrame)
        {
            ToggleSimulationPaused();
        }

        if (Keyboard.current.equalsKey.wasPressedThisFrame || Keyboard.current.numpadPlusKey.wasPressedThisFrame)
        {
            MultiplySimulationSpeed(speedStepMultiplier);
        }

        if (Keyboard.current.minusKey.wasPressedThisFrame || Keyboard.current.numpadMinusKey.wasPressedThisFrame)
        {
            MultiplySimulationSpeed(1f / Mathf.Max(0.0001f, speedStepMultiplier));
        }
    }

    GameObject CreateMarker(SatelliteTleData tle)
    {
        GameObject marker = satelliteMarkerPrefab != null
            ? Instantiate(satelliteMarkerPrefab)
            : GameObject.CreatePrimitive(PrimitiveType.Sphere);

        marker.transform.SetParent(cachedSatelliteParent, false);
        marker.transform.localScale = Vector3.one * markerScale;

        SatelliteInfo info = marker.GetComponent<SatelliteInfo>();
        if (info == null)
        {
            info = marker.AddComponent<SatelliteInfo>();
        }

        Color markerColor = SatelliteVisualMetadata.GetContinentColor(tle);
        info.Initialize(tle, markerColor);
        ApplyMarkerMaterialAndColor(marker, markerColor);
        return marker;
    }

    void ApplyMarkerMaterialAndColor(GameObject marker, Color color)
    {
        Renderer markerRenderer = marker.GetComponent<Renderer>();
        if (markerRenderer == null)
        {
            markerRenderer = marker.GetComponentInChildren<Renderer>();
        }

        if (markerRenderer == null)
        {
            return;
        }

        if (shareMarkerMaterial)
        {
            // Shared instancing-capable materials avoid per-marker material clones from primitive creation and custom prefabs.
            Material material = GetSatelliteMarkerMaterial();
            if (material != null)
            {
                markerRenderer.sharedMaterial = material;
            }
        }

        ApplyRendererColor(markerRenderer, color);
    }

    public static void ApplyRendererColor(Renderer targetRenderer, Color color)
    {
        if (targetRenderer == null)
        {
            return;
        }

        var block = new MaterialPropertyBlock();
        block.SetColor("_Color", color);
        block.SetColor("_BaseColor", color);
        block.SetColor("_EmissionColor", color * 1.4f);
        targetRenderer.SetPropertyBlock(block);
    }

    Material GetSatelliteMarkerMaterial()
    {
        Material material = satelliteMarkerMaterial;
        if (material == null)
        {
            if (sharedSatelliteMarkerMaterial == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null)
                {
                    shader = Shader.Find("Standard");
                }

                if (shader == null)
                {
                    shader = Shader.Find("Sprites/Default");
                }

                if (shader == null)
                {
                    Debug.LogWarning("SatelliteManager could not find a marker shader. Existing marker materials will be used.");
                    return material;
                }

                sharedSatelliteMarkerMaterial = new Material(shader);
                sharedSatelliteMarkerMaterial.name = "Shared Satellite Marker Material";
                sharedSatelliteMarkerMaterial.color = new Color(0.2f, 0.9f, 1f, 1f);
                sharedSatelliteMarkerMaterial.SetColor("_BaseColor", new Color(0.2f, 0.9f, 1f, 1f));
                sharedSatelliteMarkerMaterial.SetColor("_EmissionColor", new Color(0.1f, 0.8f, 1f, 1f));
                sharedSatelliteMarkerMaterial.EnableKeyword("_EMISSION");
            }

            material = sharedSatelliteMarkerMaterial;
        }

        material.enableInstancing = true;
        return material;
    }

    LineRenderer CreateOrbitLine(SatelliteTleData tle)
    {
        var orbitObject = new GameObject($"{tle.satelliteName} Orbit");
        orbitObject.transform.SetParent(cachedOrbitVisualsParent, false);

        LineRenderer line = orbitObject.AddComponent<LineRenderer>();
        line.useWorldSpace = true;
        line.loop = true;
        line.widthMultiplier = orbitLineWidth;
        line.numCornerVertices = 2;
        line.numCapVertices = 2;
        line.startColor = orbitLineColor;
        line.endColor = orbitLineColor;
        line.material = GetOrbitLineMaterial();
        return line;
    }

    Material GetOrbitLineMaterial()
    {
        if (orbitLineMaterial != null)
        {
            return orbitLineMaterial;
        }

        if (sharedOrbitLineMaterial == null)
        {
            Shader shader = Shader.Find("Sprites/Default");
            sharedOrbitLineMaterial = new Material(shader != null ? shader : Shader.Find("Legacy Shaders/Particles/Alpha Blended"));
        }

        return sharedOrbitLineMaterial;
    }

    void BuildContinentLegend()
    {
        if (!showContinentLegend || continentLegendObject != null)
        {
            return;
        }

        continentLegendObject = new GameObject("Satellite Continent Legend Canvas");
        continentLegendObject.transform.SetParent(transform, false);

        Canvas canvas = continentLegendObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 900;

        CanvasScaler scaler = continentLegendObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        continentLegendObject.AddComponent<GraphicRaycaster>();

        GameObject panelObject = new GameObject("Satellite Origin Key");
        panelObject.transform.SetParent(continentLegendObject.transform, false);

        RectTransform panelRect = panelObject.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0f, 1f);
        panelRect.anchorMax = new Vector2(0f, 1f);
        panelRect.pivot = new Vector2(0f, 1f);
        panelRect.anchoredPosition = new Vector2(legendScreenMargin.x, -legendScreenMargin.y);
        panelRect.sizeDelta = new Vector2(290f, 256f);

        Image panelImage = panelObject.AddComponent<Image>();
        panelImage.color = legendPanelColor;

        GameObject textureObject = new GameObject("Legend Texture");
        textureObject.transform.SetParent(panelObject.transform, false);

        RectTransform textureRect = textureObject.AddComponent<RectTransform>();
        textureRect.anchorMin = new Vector2(0f, 1f);
        textureRect.anchorMax = new Vector2(0f, 1f);
        textureRect.pivot = new Vector2(0f, 1f);
        textureRect.anchoredPosition = new Vector2(12f, -12f);
        textureRect.sizeDelta = new Vector2(266f, 232f);

        RawImage legendImage = textureObject.AddComponent<RawImage>();
        legendImage.texture = CreateLegendTexture();
        legendImage.raycastTarget = false;
    }

    Texture2D CreateLegendTexture()
    {
        const int width = 266;
        const int height = 232;
        var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };

        Color32[] pixels = new Color32[width * height];
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = Color.clear;
        }

        DrawBitmapText(pixels, width, height, 2, 2, "SATELLITE ORIGIN", Color.white, 2);

        SatelliteVisualMetadata.LegendEntry[] entries = SatelliteVisualMetadata.LegendEntries;
        for (int i = 0; i < entries.Length; i++)
        {
            int y = 36 + i * 28;
            DrawBitmapRect(pixels, width, height, 4, y + 1, 18, 18, entries[i].color);
            DrawBitmapText(pixels, width, height, 34, y + 3, entries[i].label.ToUpperInvariant(), Color.white, 2);
        }

        texture.SetPixels32(pixels);
        texture.Apply();
        return texture;
    }

    static void DrawBitmapRect(Color32[] pixels, int width, int height, int x, int y, int rectWidth, int rectHeight, Color32 color)
    {
        int maxX = Mathf.Min(width, x + rectWidth);
        int maxY = Mathf.Min(height, y + rectHeight);
        for (int py = Mathf.Max(0, y); py < maxY; py++)
        {
            for (int px = Mathf.Max(0, x); px < maxX; px++)
            {
                pixels[py * width + px] = color;
            }
        }
    }

    static void DrawBitmapText(Color32[] pixels, int width, int height, int x, int y, string text, Color32 color, int scale)
    {
        int cursor = x;
        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            if (c == ' ')
            {
                cursor += 4 * scale;
                continue;
            }

            string[] glyph = GetGlyph(c);
            for (int row = 0; row < glyph.Length; row++)
            {
                for (int col = 0; col < glyph[row].Length; col++)
                {
                    if (glyph[row][col] != '1')
                    {
                        continue;
                    }

                    DrawBitmapRect(pixels, width, height, cursor + col * scale, y + row * scale, scale, scale, color);
                }
            }

            cursor += 6 * scale;
        }
    }

    static string[] GetGlyph(char character)
    {
        switch (char.ToUpperInvariant(character))
        {
            case 'A': return new[] { "01110", "10001", "10001", "11111", "10001", "10001", "10001" };
            case 'B': return new[] { "11110", "10001", "10001", "11110", "10001", "10001", "11110" };
            case 'C': return new[] { "01111", "10000", "10000", "10000", "10000", "10000", "01111" };
            case 'D': return new[] { "11110", "10001", "10001", "10001", "10001", "10001", "11110" };
            case 'E': return new[] { "11111", "10000", "10000", "11110", "10000", "10000", "11111" };
            case 'F': return new[] { "11111", "10000", "10000", "11110", "10000", "10000", "10000" };
            case 'G': return new[] { "01111", "10000", "10000", "10111", "10001", "10001", "01111" };
            case 'H': return new[] { "10001", "10001", "10001", "11111", "10001", "10001", "10001" };
            case 'I': return new[] { "11111", "00100", "00100", "00100", "00100", "00100", "11111" };
            case 'K': return new[] { "10001", "10010", "10100", "11000", "10100", "10010", "10001" };
            case 'L': return new[] { "10000", "10000", "10000", "10000", "10000", "10000", "11111" };
            case 'M': return new[] { "10001", "11011", "10101", "10101", "10001", "10001", "10001" };
            case 'N': return new[] { "10001", "11001", "10101", "10011", "10001", "10001", "10001" };
            case 'O': return new[] { "01110", "10001", "10001", "10001", "10001", "10001", "01110" };
            case 'P': return new[] { "11110", "10001", "10001", "11110", "10000", "10000", "10000" };
            case 'R': return new[] { "11110", "10001", "10001", "11110", "10100", "10010", "10001" };
            case 'S': return new[] { "01111", "10000", "10000", "01110", "00001", "00001", "11110" };
            case 'T': return new[] { "11111", "00100", "00100", "00100", "00100", "00100", "00100" };
            case 'U': return new[] { "10001", "10001", "10001", "10001", "10001", "10001", "01110" };
            case 'W': return new[] { "10001", "10001", "10001", "10101", "10101", "10101", "01010" };
            case 'Y': return new[] { "10001", "10001", "01010", "00100", "00100", "00100", "00100" };
            default: return new[] { "00000", "00000", "00000", "00000", "00000", "00000", "00000" };
        }
    }

    void ClearRuntimeSatellites()
    {
        for (int i = satellites.Count - 1; i >= 0; i--)
        {
            DestroyRuntimeObject(satellites[i].Marker);
            if (satellites[i].OrbitLine != null)
            {
                DestroyRuntimeObject(satellites[i].OrbitLine.gameObject);
            }
        }

        satellites.Clear();
    }

    void DestroyRuntimeObject(GameObject target)
    {
        if (target == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(target);
        }
        else
        {
            DestroyImmediate(target);
        }
    }

    Vector3 GetEarthOrigin()
    {
        return cachedEarthReference != null ? cachedEarthReference.position : transform.position;
    }

    Vector3 ScalePositionForVisualization(Vector3 positionKilometers)
    {
        if (positionKilometers.sqrMagnitude <= 0.0001f)
        {
            return Vector3.up * earthRadiusWorldUnits;
        }

        // Scale model:
        // - Earth radius is mapped to earthRadiusWorldUnits.
        // - Satellite direction comes from SGP4 or the Keplerian fallback, preserving global distribution.
        // - Altitude above Earth is exaggerated independently so LEO remains visible in VR/desktop views.
        float physicalRadiusKilometers = positionKilometers.magnitude;
        float altitudeKilometers = Mathf.Max(0f, physicalRadiusKilometers - (float)EarthRadiusKilometers);
        float altitudeWorldUnits = altitudeKilometers * (earthRadiusWorldUnits / (float)EarthRadiusKilometers) * Mathf.Max(0f, orbitAltitudeExaggeration);
        return positionKilometers.normalized * (earthRadiusWorldUnits + altitudeWorldUnits);
    }

    public class RuntimeSatellite
    {
        readonly SatelliteTleData tleData;
        Satellite sgp4Satellite;
        bool sgp4Available;

        public SatelliteTleData TleData => tleData;
        public GameObject Marker { get; }
        public Transform MarkerTransform { get; }
        public LineRenderer OrbitLine { get; set; }

        public RuntimeSatellite(SatelliteTleData tleData, GameObject marker)
        {
            this.tleData = tleData;
            Marker = marker;
            MarkerTransform = marker != null ? marker.transform : null;
        }

        public void TryCreateSgp4Satellite()
        {
            try
            {
                sgp4Satellite = new Satellite(tleData.satelliteName, tleData.line1, tleData.line2);
                sgp4Available = true;
            }
            catch (Exception ex)
            {
                sgp4Available = false;
                Debug.LogWarning($"{tleData.satelliteName}: SGP4 setup failed. Using approximate Keplerian visualization. {ex.Message}");
            }
        }

        public Vector3 GetPositionKilometers(DateTime timeUtc)
        {
            if (sgp4Available)
            {
                try
                {
                    SGPdotNET.Util.Vector3 eciKilometers = sgp4Satellite.Predict(timeUtc).Position;
                    return ToUnityVector(eciKilometers.X, eciKilometers.Y, eciKilometers.Z);
                }
                catch (Exception ex)
                {
                    sgp4Available = false;
                    Debug.LogWarning($"{tleData.satelliteName}: SGP4 prediction failed. Using approximate Keplerian visualization. {ex.Message}");
                }
            }

            return GetApproximatePositionKilometers(timeUtc);
        }

        public Vector3 GetApproximatePositionKilometers(DateTime timeUtc)
        {
            return ApproximateKeplerPositionKilometers(timeUtc);
        }

        public DateTime GetEpochOrDefault(DateTime fallback)
        {
            return DateTime.TryParse(tleData.epoch, null, System.Globalization.DateTimeStyles.AdjustToUniversal, out DateTime parsed)
                ? parsed.ToUniversalTime()
                : fallback;
        }

        public double GetOrbitalPeriodMinutes()
        {
            double meanMotion = tleData.hasMeanMotion ? Math.Max(tleData.meanMotion, 0.01d) : 15d;
            return 1440d / meanMotion;
        }

        public static bool IsLikelyLeo(SatelliteTleData tleData, double maxLeoAltitudeKilometers, double minLeoMeanMotionRevsPerDay)
        {
            if (tleData == null || !tleData.hasMeanMotion)
            {
                return false;
            }

            double meanMotion = Math.Max(tleData.meanMotion, 0.01d);
            double periodMinutes = 1440d / meanMotion;
            double semiMajorAxisKilometers = SemiMajorAxisFromMeanMotion(meanMotion);
            double averageAltitudeKilometers = semiMajorAxisKilometers - EarthRadiusKilometers;

            return meanMotion >= minLeoMeanMotionRevsPerDay ||
                   periodMinutes <= 128d ||
                   averageAltitudeKilometers <= maxLeoAltitudeKilometers;
        }

        // Orbit lines use this approximate two-body path even when marker positions use SGP4.
        // It is a lightweight visualization: it ignores drag, J2/precession, decay, and other SGP4 corrections.
        Vector3 ApproximateKeplerPositionKilometers(DateTime timeUtc)
        {
            double inclination = DegreesToRadians(tleData.hasInclination ? tleData.inclination : 0d);
            double raan = DegreesToRadians(tleData.hasRaan ? tleData.raan : 0d);
            double eccentricity = tleData.hasEccentricity ? tleData.eccentricity : 0d;
            double argumentOfPerigee = DegreesToRadians(tleData.hasArgumentOfPerigee ? tleData.argumentOfPerigee : 0d);
            double meanAnomalyAtEpoch = DegreesToRadians(tleData.hasMeanAnomaly ? tleData.meanAnomaly : 0d);
            double meanMotionRevsPerDay = tleData.hasMeanMotion ? Math.Max(tleData.meanMotion, 0.01d) : 15d;

            DateTime epoch = GetEpochOrDefault(timeUtc);
            double minutesSinceEpoch = (timeUtc - epoch).TotalMinutes;
            double meanMotionRadiansPerMinute = meanMotionRevsPerDay * 2d * Math.PI / 1440d;
            double meanAnomaly = NormalizeRadians(meanAnomalyAtEpoch + meanMotionRadiansPerMinute * minutesSinceEpoch);
            double eccentricAnomaly = SolveEccentricAnomaly(meanAnomaly, eccentricity);

            double semiMajorAxis = SemiMajorAxisFromMeanMotion(meanMotionRevsPerDay);
            double xOrbital = semiMajorAxis * (Math.Cos(eccentricAnomaly) - eccentricity);
            double yOrbital = semiMajorAxis * Math.Sqrt(1d - eccentricity * eccentricity) * Math.Sin(eccentricAnomaly);

            double cosRaan = Math.Cos(raan);
            double sinRaan = Math.Sin(raan);
            double cosArgPerigee = Math.Cos(argumentOfPerigee);
            double sinArgPerigee = Math.Sin(argumentOfPerigee);
            double cosInclination = Math.Cos(inclination);
            double sinInclination = Math.Sin(inclination);

            double x = (cosRaan * cosArgPerigee - sinRaan * sinArgPerigee * cosInclination) * xOrbital +
                       (-cosRaan * sinArgPerigee - sinRaan * cosArgPerigee * cosInclination) * yOrbital;
            double y = (sinRaan * cosArgPerigee + cosRaan * sinArgPerigee * cosInclination) * xOrbital +
                       (-sinRaan * sinArgPerigee + cosRaan * cosArgPerigee * cosInclination) * yOrbital;
            double z = (sinArgPerigee * sinInclination) * xOrbital +
                       (cosArgPerigee * sinInclination) * yOrbital;

            return ToUnityVector(x, y, z);
        }

        static Vector3 ToUnityVector(double xKilometers, double yKilometers, double zKilometers)
        {
            return new Vector3((float)xKilometers, (float)zKilometers, (float)yKilometers);
        }

        static double SemiMajorAxisFromMeanMotion(double meanMotionRevsPerDay)
        {
            const double earthGravitationalParameter = 398600.4418;
            double meanMotionRadiansPerSecond = meanMotionRevsPerDay * 2d * Math.PI / 86400d;
            return Math.Pow(earthGravitationalParameter / (meanMotionRadiansPerSecond * meanMotionRadiansPerSecond), 1d / 3d);
        }

        static double SolveEccentricAnomaly(double meanAnomaly, double eccentricity)
        {
            double eccentricAnomaly = meanAnomaly;
            for (int i = 0; i < 8; i++)
            {
                double delta = (eccentricAnomaly - eccentricity * Math.Sin(eccentricAnomaly) - meanAnomaly) /
                               (1d - eccentricity * Math.Cos(eccentricAnomaly));
                eccentricAnomaly -= delta;
            }

            return eccentricAnomaly;
        }

        static double DegreesToRadians(double degrees)
        {
            return degrees * Math.PI / 180d;
        }

        static double NormalizeRadians(double radians)
        {
            double twoPi = 2d * Math.PI;
            radians %= twoPi;
            return radians < 0d ? radians + twoPi : radians;
        }
    }
}
