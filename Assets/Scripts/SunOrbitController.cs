using UnityEngine;
using UnityEngine.Rendering;

public class SunOrbitController : MonoBehaviour
{
    const double EarthRadiusKilometers = 6371.0d;
    const double AstronomicalUnitKilometers = 149597870.7d;
    const double SunRadiusKilometers = 695700.0d;

    [Header("References")]
    public SatelliteManager simulationClock;
    public Transform earthReference;

    [Header("Orbit")]
    public float simulatedSecondsPerSunOrbit = 86400f;
    public float orbitInclinationDegrees = 23.4f;
    public float startingAngleDegrees;

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

    [Header("Ambient Fill")]
    public bool applyAmbientFill = true;
    public Color ambientSkyColor = new Color(0.025f, 0.03f, 0.045f, 1f);
    public Color ambientEquatorColor = new Color(0.012f, 0.014f, 0.02f, 1f);
    public Color ambientGroundColor = new Color(0.004f, 0.004f, 0.006f, 1f);
    [Range(0f, 1f)]
    public float reflectionIntensity = 0.2f;

    float simulatedSunSeconds;
    Transform sunVisual;
    Material sunVisualMaterial;

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
        ApplyAmbientFill();
        EnsureSunVisual();
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

    void UpdateSunDirection()
    {
        float orbitDuration = Mathf.Max(1f, simulatedSecondsPerSunOrbit);
        float orbitAngle = startingAngleDegrees + (simulatedSunSeconds / orbitDuration) * 360f;
        Quaternion orbitRotation = Quaternion.Euler(orbitInclinationDegrees, orbitAngle, 0f);
        Vector3 sunDirectionFromEarth = orbitRotation * Vector3.forward;

        Vector3 origin = earthReference != null ? earthReference.position : Vector3.zero;
        float sunDistance = GetSunDistanceWorldUnits();
        transform.position = origin + sunDirectionFromEarth * sunDistance;
        transform.rotation = Quaternion.LookRotation(-sunDirectionFromEarth, Vector3.up);
        UpdateSunVisualScale();
        ExpandCameraFarClipPlanes(sunDistance);
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
