using System;
using UnityEngine;
using UnityEngine.Events;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[Serializable]
public class SatelliteHoverChangedEvent : UnityEvent<SatelliteInfo>
{
}

public class SatelliteHoverController : MonoBehaviour
{
    [Header("Input")]
    public bool enableDesktopMouseHover = true;
    public Camera mouseHoverCamera;

    [Tooltip("Assign VR controller ray origins here. If XR Interaction Toolkit is added later, use the controller ray/interactor attach transforms.")]
    public bool enableVrRayHover = true;
    public Transform[] vrRayOrigins;

    [Header("Raycast")]
    public LayerMask satelliteLayerMask = ~0;
    public float maxHoverDistance = 500f;

    [Header("Highlight")]
    public Color highlightColor = new Color(0.2f, 1f, 1f, 1f);
    public float highlightedScaleMultiplier = 1.6f;

    [Header("Events")]
    public SatelliteHoverChangedEvent onHoveredSatelliteChanged = new SatelliteHoverChangedEvent();

    public static event Action<SatelliteInfo> HoveredSatelliteChanged;

    SatelliteInfo hoveredSatellite;
    Transform highlightedTransform;
    Vector3 originalScale;
    readonly MaterialPropertyBlock highlightPropertyBlock = new MaterialPropertyBlock();

    public SatelliteInfo HoveredSatellite => hoveredSatellite;

    void Awake()
    {
        if (mouseHoverCamera == null)
        {
            mouseHoverCamera = Camera.main;
        }
    }

    void Update()
    {
        SatelliteInfo nextHovered = null;

        if (enableDesktopMouseHover)
        {
            nextHovered = RaycastDesktopMouse();
        }

        if (nextHovered == null && enableVrRayHover)
        {
            nextHovered = RaycastVrRays();
        }

        SetHoveredSatellite(nextHovered);
    }

    SatelliteInfo RaycastDesktopMouse()
    {
        if (mouseHoverCamera == null || !TryGetMousePosition(out Vector2 mousePosition))
        {
            return null;
        }

        Ray ray = mouseHoverCamera.ScreenPointToRay(mousePosition);
        return RaycastSatellite(ray);
    }

    SatelliteInfo RaycastVrRays()
    {
        if (vrRayOrigins == null)
        {
            return null;
        }

        for (int i = 0; i < vrRayOrigins.Length; i++)
        {
            Transform rayOrigin = vrRayOrigins[i];
            if (rayOrigin == null)
            {
                continue;
            }

            SatelliteInfo satellite = RaycastSatellite(new Ray(rayOrigin.position, rayOrigin.forward));
            if (satellite != null)
            {
                return satellite;
            }
        }

        return null;
    }

    SatelliteInfo RaycastSatellite(Ray ray)
    {
        if (!Physics.Raycast(ray, out RaycastHit hit, maxHoverDistance, satelliteLayerMask, QueryTriggerInteraction.Collide))
        {
            return null;
        }

        return hit.collider.GetComponentInParent<SatelliteInfo>();
    }

    void SetHoveredSatellite(SatelliteInfo nextHovered)
    {
        if (hoveredSatellite == nextHovered)
        {
            return;
        }

        ClearHighlight();
        hoveredSatellite = nextHovered;

        if (hoveredSatellite != null)
        {
            ApplyHighlight(hoveredSatellite);
        }

        onHoveredSatelliteChanged.Invoke(hoveredSatellite);
        HoveredSatelliteChanged?.Invoke(hoveredSatellite);
    }

    void ApplyHighlight(SatelliteInfo satellite)
    {
        highlightedTransform = satellite.transform;
        originalScale = highlightedTransform.localScale;
        highlightedTransform.localScale = originalScale * highlightedScaleMultiplier;

        Renderer renderer = satellite.GetComponentInChildren<Renderer>();
        if (renderer == null)
        {
            return;
        }

        highlightPropertyBlock.Clear();
        highlightPropertyBlock.SetColor("_Color", highlightColor);
        renderer.SetPropertyBlock(highlightPropertyBlock);
    }

    void ClearHighlight()
    {
        if (highlightedTransform != null)
        {
            highlightedTransform.localScale = originalScale;

            Renderer renderer = highlightedTransform.GetComponentInChildren<Renderer>();
            if (renderer != null)
            {
                renderer.SetPropertyBlock(null);
            }
        }

        highlightedTransform = null;
    }

    static bool TryGetMousePosition(out Vector2 mousePosition)
    {
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current == null)
        {
            mousePosition = default;
            return false;
        }

        mousePosition = Mouse.current.position.ReadValue();
        return true;
#else
        mousePosition = Input.mousePosition;
        return true;
#endif
    }
}
