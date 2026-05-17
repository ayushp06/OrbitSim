using System;
using System.Collections.Generic;
using SGPdotNET.Observation;
using UnityEngine;

public class SatelliteOrbitController : MonoBehaviour
{
    const double EarthRadiusKilometers = 6371.0;

    [Header("Data")]
    public TleLoader tleLoader;
    public bool loadOnStart = true;

    [Header("Scene References")]
    public Transform earthCenter;
    public Transform satellitesParent;
    public GameObject satelliteMarkerPrefab;

    [Header("Simulation")]
    public DateTimeKind simulationTimeKind = DateTimeKind.Utc;
    public float timeScale = 120f;
    public float earthRadiusWorldUnits = 10f;
    public float markerScale = 0.35f;

    [Header("Propagation")]
    public bool preferSgp4Propagation = true;

    readonly List<RuntimeSatellite> runtimeSatellites = new List<RuntimeSatellite>();
    DateTime simulationTimeUtc;

    public DateTime SimulationTimeUtc => simulationTimeUtc;
    public IReadOnlyList<SatelliteTleData> LoadedSatellites => tleLoader != null ? tleLoader.Satellites : Array.Empty<SatelliteTleData>();

    void Awake()
    {
        simulationTimeUtc = DateTime.UtcNow;
    }

    void Start()
    {
        if (loadOnStart)
        {
            BuildSatelliteRuntime();
        }
    }

    void Update()
    {
        simulationTimeUtc = simulationTimeUtc.AddSeconds(Time.deltaTime * timeScale);
        UpdateSatellitePositions();
    }

    public void BuildSatelliteRuntime()
    {
        runtimeSatellites.Clear();

        if (tleLoader == null)
        {
            tleLoader = GetComponent<TleLoader>();
        }

        if (tleLoader == null)
        {
            Debug.LogWarning("SatelliteOrbitController has no TleLoader assigned.");
            return;
        }

        tleLoader.LoadConfiguredSource();

        foreach (SatelliteTleData tle in tleLoader.Satellites)
        {
            RuntimeSatellite runtimeSatellite = new RuntimeSatellite(tle, CreateMarker(tle));
            if (preferSgp4Propagation)
            {
                runtimeSatellite.TryCreateSgp4Satellite();
            }

            runtimeSatellites.Add(runtimeSatellite);
        }

        UpdateSatellitePositions();
    }

    void UpdateSatellitePositions()
    {
        Vector3 origin = earthCenter != null ? earthCenter.position : transform.position;
        float kilometersToWorldUnits = earthRadiusWorldUnits / (float)EarthRadiusKilometers;

        foreach (RuntimeSatellite satellite in runtimeSatellites)
        {
            if (satellite.Marker == null)
            {
                continue;
            }

            Vector3 worldOffset = satellite.GetWorldOffset(simulationTimeUtc, kilometersToWorldUnits, preferSgp4Propagation);
            satellite.Marker.transform.position = origin + worldOffset;
        }
    }

    GameObject CreateMarker(SatelliteTleData tle)
    {
        GameObject marker = satelliteMarkerPrefab != null
            ? Instantiate(satelliteMarkerPrefab)
            : GameObject.CreatePrimitive(PrimitiveType.Sphere);

        marker.name = string.IsNullOrWhiteSpace(tle.satelliteName) ? "Satellite" : tle.satelliteName;
        marker.transform.SetParent(satellitesParent != null ? satellitesParent : transform, false);
        marker.transform.localScale = Vector3.one * markerScale;
        return marker;
    }

    class RuntimeSatellite
    {
        readonly SatelliteTleData tle;
        Satellite sgp4Satellite;
        bool sgp4Available;

        public GameObject Marker { get; }

        public RuntimeSatellite(SatelliteTleData tle, GameObject marker)
        {
            this.tle = tle;
            Marker = marker;
        }

        public void TryCreateSgp4Satellite()
        {
            try
            {
                sgp4Satellite = new Satellite(tle.satelliteName, tle.line1, tle.line2);
                sgp4Available = true;
            }
            catch (Exception ex)
            {
                sgp4Available = false;
                Debug.LogWarning($"{tle.satelliteName}: failed to create SGP4 satellite. Falling back to approximate Kepler visualization. {ex.Message}");
            }
        }

        public Vector3 GetWorldOffset(DateTime timeUtc, float kilometersToWorldUnits, bool preferSgp4)
        {
            if (preferSgp4 && sgp4Available)
            {
                try
                {
                    SGPdotNET.Util.Vector3 eciKilometers = sgp4Satellite.Predict(timeUtc).Position;
                    return ToUnityVector(eciKilometers.X, eciKilometers.Y, eciKilometers.Z) * kilometersToWorldUnits;
                }
                catch (Exception ex)
                {
                    sgp4Available = false;
                    Debug.LogWarning($"{tle.satelliteName}: SGP4 prediction failed. Falling back to approximate Kepler visualization. {ex.Message}");
                }
            }

            return ApproximateKeplerPosition(timeUtc) * kilometersToWorldUnits;
        }

        // This fallback is intended only for visualization continuity. It ignores drag, perturbations,
        // deep-space effects, exact TLE epoch derivatives, and full SGP4 corrections.
        Vector3 ApproximateKeplerPosition(DateTime timeUtc)
        {
            double inclination = DegreesToRadians(tle.hasInclination ? tle.inclination : 0d);
            double raan = DegreesToRadians(tle.hasRaan ? tle.raan : 0d);
            double eccentricity = tle.hasEccentricity ? tle.eccentricity : 0d;
            double argumentOfPerigee = DegreesToRadians(tle.hasArgumentOfPerigee ? tle.argumentOfPerigee : 0d);
            double meanAnomalyAtEpoch = DegreesToRadians(tle.hasMeanAnomaly ? tle.meanAnomaly : 0d);
            double meanMotionRevsPerDay = tle.hasMeanMotion ? Math.Max(tle.meanMotion, 0.01d) : 15d;

            DateTime epoch = ParseEpochOrNow(tle.epoch, timeUtc);
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

        static DateTime ParseEpochOrNow(string epoch, DateTime fallback)
        {
            return DateTime.TryParse(epoch, null, System.Globalization.DateTimeStyles.AdjustToUniversal, out DateTime parsed)
                ? parsed.ToUniversalTime()
                : fallback;
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
