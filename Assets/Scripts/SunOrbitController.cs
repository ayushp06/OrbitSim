using UnityEngine;

public class SunOrbitController : MonoBehaviour
{
    [Header("References")]
    public SatelliteManager simulationClock;
    public Transform earthReference;

    [Header("Orbit")]
    public float simulatedSecondsPerSunOrbit = 86400f;
    public float orbitInclinationDegrees = 23.4f;
    public float startingAngleDegrees;

    [Header("Lighting")]
    public Light sunLight;
    public float softenedIntensity = 1.25f;
    public float shadowStrength = 0.45f;
    public Color sunColor = new Color(1f, 0.95f, 0.82f, 1f);

    float simulatedSunSeconds;

    void Awake()
    {
        if (sunLight == null)
        {
            sunLight = GetComponent<Light>();
        }

        if (simulationClock == null)
        {
            simulationClock = FindFirstObjectByType<SatelliteManager>();
        }

        if (earthReference == null && simulationClock != null)
        {
            earthReference = simulationClock.earthReference;
        }

        ApplyLightSettings();
        UpdateSunDirection();
    }

    void Update()
    {
        float effectiveTimeScale = simulationClock != null ? simulationClock.EffectiveTimeScale : 1f;
        simulatedSunSeconds += Time.deltaTime * effectiveTimeScale;
        UpdateSunDirection();
    }

    void ApplyLightSettings()
    {
        if (sunLight == null)
        {
            return;
        }

        sunLight.color = sunColor;
        sunLight.intensity = softenedIntensity;
        sunLight.shadowStrength = shadowStrength;
    }

    void UpdateSunDirection()
    {
        float orbitDuration = Mathf.Max(1f, simulatedSecondsPerSunOrbit);
        float orbitAngle = startingAngleDegrees + (simulatedSunSeconds / orbitDuration) * 360f;
        Quaternion orbitRotation = Quaternion.Euler(orbitInclinationDegrees, orbitAngle, 0f);
        Vector3 sunDirectionFromEarth = orbitRotation * Vector3.forward;

        Vector3 origin = earthReference != null ? earthReference.position : Vector3.zero;
        transform.position = origin + sunDirectionFromEarth * 100f;
        transform.rotation = Quaternion.LookRotation(-sunDirectionFromEarth, Vector3.up);
    }
}
