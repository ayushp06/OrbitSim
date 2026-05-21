using UnityEngine;
using UnityEngine.Rendering;

[DefaultExecutionOrder(-100)]
public class SunOrbitController : MonoBehaviour
{
    const double EarthRadiusKilometers = 6371.0d;
    const double AstronomicalUnitKilometers = 149597870.7d;
    const double SunRadiusKilometers = 695700.0d;
    const double EarthSiderealDaySeconds = 86164.0905d;
    const double EarthSiderealYearSeconds = 31558149.7632d;

    [Header("References")]
    public SatelliteManager simulationClock;
    public Transform earthReference;
    public Transform earthVisual;
    public Transform observerRig;

    [Header("Earth Orbit")]
    public bool orbitEarthAroundSun = true;
    public float startingEarthOrbitAngleDegrees;
    public float earthOrbitInclinationDegrees;

    [Header("Earth Rotation")]
    public bool spinEarthOnAxis = true;
    public float earthAxialTiltDegrees = 23.4393f;
    public float startingEarthRotationDegrees;
    public bool smoothEarthRotation = true;
    [Min(0.1f)]
    public float earthRotationSmoothing = 18f;

    [Header("Lighting")]
    public Light sunLight;
    public float softenedIntensity = 1.45f;
    public float shadowStrength = 0.35f;
    public Color sunColor = new Color(1f, 0.95f, 0.82f, 1f);

    [Header("Solar Scale")]
    public bool useEarthScaleSunDistance = true;
    public float fallbackSunDistanceWorldUnits = 100f;
    public bool createSunVisual = true;
    public float sunVisualScaleMultiplier = 1f;
    public bool expandCameraFarClipPlanes = true;

    [Header("Observer")]
    public bool keepObserverNearEarth = true;
    public Vector3 observerOffsetFromEarth = new Vector3(0f, 20f, -55f);

    [Header("Ambient Fill")]
    public bool applyAmbientFill = true;
    public Color ambientSkyColor = new Color(0.025f, 0.03f, 0.045f, 1f);
    public Color ambientEquatorColor = new Color(0.012f, 0.014f, 0.02f, 1f);
    public Color ambientGroundColor = new Color(0.004f, 0.004f, 0.006f, 1f);
    [Range(0f, 1f)]
    public float reflectionIntensity = 0.2f;

    double simulatedSeconds;
    Transform sunVisual;
    Material sunVisualMaterial;
    Quaternion targetEarthVisualRotation = Quaternion.identity;
    bool hasEarthVisualRotationTarget;

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

        if (earthVisual == null && simulationClock != null)
        {
            earthVisual = simulationClock.earthVisual;
        }

        ApplyLightSettings();
        ApplyAmbientFill();
        EnsureSunVisual();
        UpdateSolarSystem();
    }

    void Update()
    {
        float effectiveTimeScale = simulationClock != null ? simulationClock.EffectiveTimeScale : 1f;
        simulatedSeconds += Time.deltaTime * effectiveTimeScale;
        UpdateSolarSystem();
    }

    void LateUpdate()
    {
        ApplyEarthVisualRotation();
    }

    void ApplyLightSettings()
    {
        if (sunLight == null)
        {
            return;
        }

        sunLight.type = LightType.Directional;
        sunLight.color = sunColor;
        sunLight.intensity = softenedIntensity;
        sunLight.shadowStrength = shadowStrength;
    }

    void ApplyAmbientFill()
    {
        if (!applyAmbientFill)
        {
            return;
        }

        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = ambientSkyColor;
        RenderSettings.ambientEquatorColor = ambientEquatorColor;
        RenderSettings.ambientGroundColor = ambientGroundColor;
        RenderSettings.reflectionIntensity = reflectionIntensity;
    }

    void UpdateSolarSystem()
    {
        transform.position = Vector3.zero;
        UpdateSunVisualScale();

        Vector3 earthPosition = GetEarthPosition();
        if (earthReference != null)
        {
            earthReference.position = earthPosition;
        }

        UpdateEarthSpinTarget();
        UpdateSunLight(earthPosition);
        UpdateObserver(earthPosition);
        ExpandCameraFarClipPlanes(GetSunDistanceWorldUnits());
    }

    Vector3 GetEarthPosition()
    {
        if (!orbitEarthAroundSun)
        {
            return earthReference != null ? earthReference.position : Vector3.zero;
        }

        float orbitAngle = startingEarthOrbitAngleDegrees + (float)(simulatedSeconds / EarthSiderealYearSeconds * 360d);
        Quaternion orbitRotation = Quaternion.Euler(earthOrbitInclinationDegrees, orbitAngle, 0f);
        return orbitRotation * (Vector3.forward * GetSunDistanceWorldUnits());
    }

    void UpdateEarthSpinTarget()
    {
        if (!spinEarthOnAxis || earthVisual == null)
        {
            return;
        }

        float rotationAngle = Mathf.Repeat(startingEarthRotationDegrees + (float)(simulatedSeconds / EarthSiderealDaySeconds * 360d), 360f);
        Quaternion axialTilt = Quaternion.Euler(0f, 0f, -earthAxialTiltDegrees);
        Quaternion dailySpin = Quaternion.Euler(0f, rotationAngle, 0f);
        targetEarthVisualRotation = axialTilt * dailySpin;
        hasEarthVisualRotationTarget = true;

        if (!smoothEarthRotation)
        {
            earthVisual.localRotation = targetEarthVisualRotation;
        }
    }

    void ApplyEarthVisualRotation()
    {
        if (!smoothEarthRotation || !hasEarthVisualRotationTarget || earthVisual == null)
        {
            return;
        }

        float blend = 1f - Mathf.Exp(-Mathf.Max(0.1f, earthRotationSmoothing) * Time.deltaTime);
        earthVisual.localRotation = Quaternion.Slerp(earthVisual.localRotation, targetEarthVisualRotation, blend);
    }

    void UpdateSunLight(Vector3 earthPosition)
    {
        Vector3 directionToEarth = earthPosition.sqrMagnitude > 0.0001f ? earthPosition.normalized : Vector3.forward;
        transform.rotation = Quaternion.LookRotation(directionToEarth, Vector3.up);
    }

    void UpdateObserver(Vector3 earthPosition)
    {
        if (!keepObserverNearEarth || observerRig == null)
        {
            return;
        }

        observerRig.position = earthPosition + observerOffsetFromEarth;
    }

    float GetSunDistanceWorldUnits()
    {
        if (!useEarthScaleSunDistance)
        {
            return Mathf.Max(1f, fallbackSunDistanceWorldUnits);
        }

        float earthRadiusWorldUnits = simulationClock != null ? simulationClock.earthRadiusWorldUnits : 10f;
        return (float)(AstronomicalUnitKilometers / EarthRadiusKilometers) * earthRadiusWorldUnits;
    }

    void EnsureSunVisual()
    {
        if (!createSunVisual || sunVisual != null)
        {
            return;
        }

        GameObject visualObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        visualObject.name = "Scaled Sun Visual";
        visualObject.transform.SetParent(transform, false);
        sunVisual = visualObject.transform;

        Collider visualCollider = visualObject.GetComponent<Collider>();
        if (visualCollider != null)
        {
            Destroy(visualCollider);
        }

        Renderer renderer = visualObject.GetComponent<Renderer>();
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
        {
            shader = Shader.Find("Unlit/Color");
        }

        if (shader != null && renderer != null)
        {
            sunVisualMaterial = new Material(shader)
            {
                name = "Runtime Sun Visual Material",
                color = sunColor
            };
            SetMaterialColorIfPresent(sunVisualMaterial, "_Color", sunColor);
            SetMaterialColorIfPresent(sunVisualMaterial, "_BaseColor", sunColor);
            SetMaterialColorIfPresent(sunVisualMaterial, "_EmissionColor", sunColor * 2f);
            renderer.sharedMaterial = sunVisualMaterial;
        }
    }

    static void SetMaterialColorIfPresent(Material material, string propertyName, Color color)
    {
        if (material != null && material.HasProperty(propertyName))
        {
            material.SetColor(propertyName, color);
        }
    }

    void UpdateSunVisualScale()
    {
        if (sunVisual == null)
        {
            return;
        }

        float earthRadiusWorldUnits = simulationClock != null ? simulationClock.earthRadiusWorldUnits : 10f;
        float sunDiameterWorldUnits = (float)(SunRadiusKilometers / EarthRadiusKilometers) * earthRadiusWorldUnits * 2f;
        sunVisual.localPosition = Vector3.zero;
        sunVisual.localRotation = Quaternion.identity;
        sunVisual.localScale = Vector3.one * sunDiameterWorldUnits * Mathf.Max(0.001f, sunVisualScaleMultiplier);
    }

    void ExpandCameraFarClipPlanes(float sunDistance)
    {
        if (!expandCameraFarClipPlanes)
        {
            return;
        }

        float earthRadiusWorldUnits = simulationClock != null ? simulationClock.earthRadiusWorldUnits : 10f;
        float sunRadiusWorldUnits = (float)(SunRadiusKilometers / EarthRadiusKilometers) * earthRadiusWorldUnits;
        float requiredFarClip = sunDistance + sunRadiusWorldUnits * 2f;
        Camera[] cameras = Camera.allCameras;
        for (int i = 0; i < cameras.Length; i++)
        {
            if (cameras[i] != null && cameras[i].farClipPlane < requiredFarClip)
            {
                cameras[i].farClipPlane = requiredFarClip;
            }
        }
    }
}
