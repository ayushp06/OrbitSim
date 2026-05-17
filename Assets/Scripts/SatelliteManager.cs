using System;
using System.Collections.Generic;
using SGPdotNET.Observation;
using UnityEngine;
using UnityEngine.InputSystem;

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
    public int maxSatellitesForTesting = 0;
    public bool orbitLinesEnabled = true;

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
        LoadAndSpawnSatellites();
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
            BuildOrbitLines();
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

        if (Keyboard.current.spaceKey.wasPressedThisFrame)
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

        info.Initialize(tle);
        ApplySharedMarkerMaterial(marker);
        return marker;
    }

    void ApplySharedMarkerMaterial(GameObject marker)
    {
        if (!shareMarkerMaterial)
        {
            return;
        }

        Renderer markerRenderer = marker.GetComponent<Renderer>();
        if (markerRenderer == null)
        {
            markerRenderer = marker.GetComponentInChildren<Renderer>();
        }

        if (markerRenderer == null)
        {
            return;
        }

        // Shared instancing-capable materials avoid per-marker material clones from primitive creation and custom prefabs.
        Material material = GetSatelliteMarkerMaterial();
        if (material != null)
        {
            markerRenderer.sharedMaterial = material;
        }
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
