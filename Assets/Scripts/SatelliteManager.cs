using System;
using System.Collections.Generic;
using SGPdotNET.Observation;
using UnityEngine;

public class SatelliteManager : MonoBehaviour
{
    const double EarthRadiusKilometers = 6371.0;

    [Header("Data")]
    public TleLoader tleLoader;

    [Header("Scene References")]
    public GameObject satelliteMarkerPrefab;
    public Transform earthReference;
    public Transform satellitesParent;
    public Transform orbitVisualsParent;

    [Header("Simulation")]
    public float orbitScale = 1f;
    public float earthRadiusWorldUnits = 10f;
    public float timeScale = 120f;
    public int maxSatellitesForTesting = 0;
    public bool orbitLinesEnabled = true;

    [Header("Marker Rendering")]
    public float markerScale = 0.35f;

    [Header("Orbit Rendering")]
    public int orbitLineSegments = 96;
    public int maxOrbitLines = 64;
    public float orbitLineWidth = 0.025f;
    public Color orbitLineColor = new Color(0.25f, 0.8f, 1f, 0.45f);
    public Material orbitLineMaterial;

    readonly List<RuntimeSatellite> satellites = new List<RuntimeSatellite>();
    DateTime simulationTimeUtc;
    Material sharedOrbitLineMaterial;

    public DateTime SimulationTimeUtc => simulationTimeUtc;
    public IReadOnlyList<RuntimeSatellite> RuntimeSatellites => satellites;

    void Awake()
    {
        simulationTimeUtc = DateTime.UtcNow;
    }

    void Start()
    {
        LoadAndSpawnSatellites();
    }

    void Update()
    {
        simulationTimeUtc = simulationTimeUtc.AddSeconds(Time.deltaTime * timeScale);
        UpdateSatellitePositions();
    }

    public void LoadAndSpawnSatellites()
    {
        ClearRuntimeSatellites();

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

        int spawnCount = tleLoader.Satellites.Count;
        if (maxSatellitesForTesting > 0)
        {
            spawnCount = Mathf.Min(spawnCount, maxSatellitesForTesting);
        }

        for (int i = 0; i < spawnCount; i++)
        {
            SatelliteTleData tle = tleLoader.Satellites[i];
            var runtimeSatellite = new RuntimeSatellite(tle, CreateMarker(tle));
            runtimeSatellite.TryCreateSgp4Satellite();
            satellites.Add(runtimeSatellite);
        }

        if (orbitLinesEnabled)
        {
            BuildOrbitLines();
        }

        UpdateSatellitePositions();
    }

    void UpdateSatellitePositions()
    {
        Vector3 origin = GetEarthOrigin();
        float kilometersToWorldUnits = GetKilometersToWorldUnits();

        for (int i = 0; i < satellites.Count; i++)
        {
            RuntimeSatellite satellite = satellites[i];
            if (satellite.Marker == null)
            {
                continue;
            }

            satellite.Marker.transform.position = origin + satellite.GetWorldOffset(simulationTimeUtc, kilometersToWorldUnits);
        }
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
        float kilometersToWorldUnits = GetKilometersToWorldUnits();
        DateTime epoch = satellite.GetEpochOrDefault(simulationTimeUtc);
        double periodMinutes = satellite.GetOrbitalPeriodMinutes();

        for (int i = 0; i <= segments; i++)
        {
            double t = (double)i / segments;
            DateTime sampleTime = epoch.AddMinutes(periodMinutes * t);
            line.SetPosition(i, origin + satellite.GetApproximateWorldOffset(sampleTime, kilometersToWorldUnits));
        }
    }

    GameObject CreateMarker(SatelliteTleData tle)
    {
        GameObject marker = satelliteMarkerPrefab != null
            ? Instantiate(satelliteMarkerPrefab)
            : GameObject.CreatePrimitive(PrimitiveType.Sphere);

        marker.transform.SetParent(satellitesParent != null ? satellitesParent : transform, false);
        marker.transform.localScale = Vector3.one * markerScale;

        SatelliteInfo info = marker.GetComponent<SatelliteInfo>();
        if (info == null)
        {
            info = marker.AddComponent<SatelliteInfo>();
        }

        info.Initialize(tle);
        return marker;
    }

    LineRenderer CreateOrbitLine(SatelliteTleData tle)
    {
        var orbitObject = new GameObject($"{tle.satelliteName} Orbit");
        orbitObject.transform.SetParent(orbitVisualsParent != null ? orbitVisualsParent : transform, false);

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
        return earthReference != null ? earthReference.position : transform.position;
    }

    float GetKilometersToWorldUnits()
    {
        return (earthRadiusWorldUnits / (float)EarthRadiusKilometers) * Mathf.Max(0.0001f, orbitScale);
    }

    public class RuntimeSatellite
    {
        readonly SatelliteTleData tleData;
        Satellite sgp4Satellite;
        bool sgp4Available;

        public SatelliteTleData TleData => tleData;
        public GameObject Marker { get; }
        public LineRenderer OrbitLine { get; set; }

        public RuntimeSatellite(SatelliteTleData tleData, GameObject marker)
        {
            this.tleData = tleData;
            Marker = marker;
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

        public Vector3 GetWorldOffset(DateTime timeUtc, float kilometersToWorldUnits)
        {
            if (sgp4Available)
            {
                try
                {
                    SGPdotNET.Util.Vector3 eciKilometers = sgp4Satellite.Predict(timeUtc).Position;
                    return ToUnityVector(eciKilometers.X, eciKilometers.Y, eciKilometers.Z) * kilometersToWorldUnits;
                }
                catch (Exception ex)
                {
                    sgp4Available = false;
                    Debug.LogWarning($"{tleData.satelliteName}: SGP4 prediction failed. Using approximate Keplerian visualization. {ex.Message}");
                }
            }

            return GetApproximateWorldOffset(timeUtc, kilometersToWorldUnits);
        }

        public Vector3 GetApproximateWorldOffset(DateTime timeUtc, float kilometersToWorldUnits)
        {
            return ApproximateKeplerPositionKilometers(timeUtc) * kilometersToWorldUnits;
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
