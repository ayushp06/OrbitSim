using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
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
    public bool useScreenCenterForDesktopHover = true;
    public Camera mouseHoverCamera;

    [Tooltip("Assign VR controller ray origins here. If XR Interaction Toolkit is added later, use the controller ray/interactor attach transforms.")]
    public bool enableVrRayHover = true;
    public Transform[] vrRayOrigins;

    [Header("Raycast")]
    public LayerMask satelliteLayerMask = ~0;
    public float maxHoverDistance = 500f;
    public float hoverSphereCastRadius = 0f;
    [Tooltip("Desktop crosshair pick radius in screen pixels around the satellite marker center.")]
    public float desktopHoverScreenRadiusPixels = 12f;

    [Header("Click Tracking")]
    public bool enableClickToTrack = true;
    public Camera trackingCamera;
    public bool rotateCameraToTrackedSatellite = true;
    public float trackingTurnSpeedDegreesPerSecond = 720f;
    public bool disableFlyCameraInputWhileTracking = true;

    [Header("Desktop Crosshair")]
    public bool showDesktopCrosshair = true;
    public Color crosshairColor = new Color(0.95f, 1f, 1f, 0.9f);
    public float crosshairSize = 8f;
    public float crosshairThickness = 1f;

    [Header("Highlight")]
    public Color highlightColor = new Color(0.2f, 1f, 1f, 1f);
    public float highlightedScaleMultiplier = 1f;

    [Header("Events")]
    public SatelliteHoverChangedEvent onHoveredSatelliteChanged = new SatelliteHoverChangedEvent();

    public static event Action<SatelliteInfo> HoveredSatelliteChanged;

    SatelliteInfo hoveredSatellite;
    Transform highlightedTransform;
    Vector3 originalScale;
    MaterialPropertyBlock highlightPropertyBlock;
    RectTransform crosshairRect;
    SatelliteInfo trackedSatellite;
    FlyCameraNewInput flyCameraControls;

    public SatelliteInfo HoveredSatellite => hoveredSatellite;
    public SatelliteInfo TrackedSatellite => trackedSatellite;

    void Awake()
    {
        highlightPropertyBlock = new MaterialPropertyBlock();

        if (mouseHoverCamera == null)
        {
            mouseHoverCamera = Camera.main;
        }

        if (trackingCamera == null)
        {
            trackingCamera = mouseHoverCamera;
        }

        if (trackingCamera != null)
        {
            flyCameraControls = trackingCamera.GetComponent<FlyCameraNewInput>();
        }

        BuildDesktopCrosshair();
    }

    void Update()
    {
        UpdateDesktopCrosshair();
        HandleClickTrackingInput();

        SatelliteInfo nextHovered = trackedSatellite;

        if (nextHovered == null && enableDesktopMouseHover)
        {
            nextHovered = FindDesktopSatelliteTarget();
        }

        if (nextHovered == null && enableVrRayHover)
        {
            nextHovered = RaycastVrRays();
        }

        SetHoveredSatellite(nextHovered);
    }

    void LateUpdate()
    {
        UpdateTrackedCameraAim();
    }

    void HandleClickTrackingInput()
    {
        if (!enableClickToTrack || !WasPrimaryClickPressed())
        {
            return;
        }

        if (trackedSatellite != null)
        {
            ClearTrackedSatellite();
            return;
        }

        SatelliteInfo clickedSatellite = FindDesktopSatelliteTarget();
        if (clickedSatellite == null && enableVrRayHover)
        {
            clickedSatellite = RaycastVrRays();
        }

        if (clickedSatellite != null)
        {
            SetTrackedSatellite(clickedSatellite);
        }
    }

    void SetTrackedSatellite(SatelliteInfo satellite)
    {
        trackedSatellite = satellite;
        SetFlyCameraInputEnabled(false);
    }

    void ClearTrackedSatellite()
    {
        trackedSatellite = null;
        if (flyCameraControls != null)
        {
            flyCameraControls.SyncLookAnglesFromTransform();
        }

        SetFlyCameraInputEnabled(true);
    }

    void UpdateTrackedCameraAim()
    {
        if (!rotateCameraToTrackedSatellite || trackedSatellite == null || trackingCamera == null)
        {
            return;
        }

        Vector3 toSatellite = trackedSatellite.transform.position - trackingCamera.transform.position;
        if (toSatellite.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(toSatellite.normalized, Vector3.up);
        float turnSpeed = Mathf.Max(1f, trackingTurnSpeedDegreesPerSecond);
        trackingCamera.transform.rotation = Quaternion.RotateTowards(
            trackingCamera.transform.rotation,
            targetRotation,
            turnSpeed * Time.deltaTime);

        if (flyCameraControls != null)
        {
            flyCameraControls.SyncLookAnglesFromTransform();
        }
    }

    void SetFlyCameraInputEnabled(bool enabled)
    {
        if (!disableFlyCameraInputWhileTracking || flyCameraControls == null)
        {
            return;
        }

        flyCameraControls.enabled = enabled;
    }

    SatelliteInfo FindDesktopSatelliteTarget()
    {
        if (mouseHoverCamera == null)
        {
            return null;
        }

        Vector2 targetScreenPoint = useScreenCenterForDesktopHover
            ? new Vector2(mouseHoverCamera.pixelWidth * 0.5f, mouseHoverCamera.pixelHeight * 0.5f)
            : GetMousePositionOrCenter();

        SatelliteInfo[] satellites = FindObjectsByType<SatelliteInfo>(FindObjectsSortMode.None);
        SatelliteInfo nearestSatellite = null;
        float bestScreenDistanceSquared = Mathf.Max(0f, desktopHoverScreenRadiusPixels) * Mathf.Max(0f, desktopHoverScreenRadiusPixels);
        float bestDepth = float.MaxValue;

        for (int i = 0; i < satellites.Length; i++)
        {
            SatelliteInfo satellite = satellites[i];
            if (satellite == null)
            {
                continue;
            }

            Vector3 viewportPoint = mouseHoverCamera.WorldToViewportPoint(satellite.transform.position);
            if (viewportPoint.z <= 0f || viewportPoint.x < 0f || viewportPoint.x > 1f || viewportPoint.y < 0f || viewportPoint.y > 1f)
            {
                continue;
            }

            float depth = Vector3.Distance(mouseHoverCamera.transform.position, satellite.transform.position);
            if (depth > maxHoverDistance || IsSatelliteOccluded(satellite, depth))
            {
                continue;
            }

            Vector2 satelliteScreenPoint = new Vector2(viewportPoint.x * mouseHoverCamera.pixelWidth, viewportPoint.y * mouseHoverCamera.pixelHeight);
            float screenDistanceSquared = (satelliteScreenPoint - targetScreenPoint).sqrMagnitude;
            if (screenDistanceSquared <= bestScreenDistanceSquared && depth < bestDepth)
            {
                bestScreenDistanceSquared = screenDistanceSquared;
                bestDepth = depth;
                nearestSatellite = satellite;
            }
        }

        return nearestSatellite;
    }

    bool IsSatelliteOccluded(SatelliteInfo satellite, float satelliteDistance)
    {
        Vector3 origin = mouseHoverCamera.transform.position;
        Vector3 toSatellite = satellite.transform.position - origin;
        if (toSatellite.sqrMagnitude <= 0.0001f)
        {
            return false;
        }

        Ray ray = new Ray(origin, toSatellite.normalized);
        RaycastHit[] hits = Physics.RaycastAll(ray, satelliteDistance, satelliteLayerMask, QueryTriggerInteraction.Collide);
        Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        for (int i = 0; i < hits.Length; i++)
        {
            Collider hitCollider = hits[i].collider;
            if (hitCollider == null)
            {
                continue;
            }

            SatelliteInfo hitSatellite = hitCollider.GetComponentInParent<SatelliteInfo>();
            if (hitSatellite == satellite)
            {
                return false;
            }

            if (hitSatellite == null)
            {
                return true;
            }
        }

        return false;
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
        if (hoverSphereCastRadius > 0f)
        {
            RaycastHit[] sphereHits = Physics.SphereCastAll(ray, hoverSphereCastRadius, maxHoverDistance, satelliteLayerMask, QueryTriggerInteraction.Collide);
            SatelliteInfo sphereSatellite = FindNearestSatelliteHit(sphereHits);
            if (sphereSatellite != null)
            {
                return sphereSatellite;
            }
        }

        RaycastHit[] rayHits = Physics.RaycastAll(ray, maxHoverDistance, satelliteLayerMask, QueryTriggerInteraction.Collide);
        return FindNearestSatelliteHit(rayHits);
    }

    static SatelliteInfo FindNearestSatelliteHit(RaycastHit[] hits)
    {
        if (hits == null || hits.Length == 0)
        {
            return null;
        }

        Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        for (int i = 0; i < hits.Length; i++)
        {
            Collider hitCollider = hits[i].collider;
            if (hitCollider == null)
            {
                continue;
            }

            SatelliteInfo satellite = hitCollider.GetComponentInParent<SatelliteInfo>();
            if (satellite != null)
            {
                return satellite;
            }
        }

        return null;
    }

    void BuildDesktopCrosshair()
    {
        if (!showDesktopCrosshair)
        {
            return;
        }

        GameObject canvasObject = new GameObject("Desktop Hover Crosshair Canvas");
        canvasObject.transform.SetParent(transform, false);

        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;

        canvasObject.AddComponent<GraphicRaycaster>();

        GameObject crosshairObject = new GameObject("Mouse Crosshair");
        crosshairObject.transform.SetParent(canvasObject.transform, false);
        crosshairRect = crosshairObject.AddComponent<RectTransform>();
        crosshairRect.anchorMin = useScreenCenterForDesktopHover ? new Vector2(0.5f, 0.5f) : Vector2.zero;
        crosshairRect.anchorMax = useScreenCenterForDesktopHover ? new Vector2(0.5f, 0.5f) : Vector2.zero;
        crosshairRect.pivot = new Vector2(0.5f, 0.5f);
        crosshairRect.sizeDelta = new Vector2(crosshairSize, crosshairSize);

        CreateCrosshairLine(crosshairObject.transform, "Horizontal", new Vector2(crosshairSize, crosshairThickness));
        CreateCrosshairLine(crosshairObject.transform, "Vertical", new Vector2(crosshairThickness, crosshairSize));
    }

    void CreateCrosshairLine(Transform parent, string name, Vector2 size)
    {
        GameObject lineObject = new GameObject(name);
        lineObject.transform.SetParent(parent, false);

        RectTransform rect = lineObject.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = Vector2.zero;

        Image image = lineObject.AddComponent<Image>();
        image.color = crosshairColor;
        image.raycastTarget = false;
    }

    void UpdateDesktopCrosshair()
    {
        if (crosshairRect == null)
        {
            return;
        }

        bool visible = enableDesktopMouseHover;
        crosshairRect.gameObject.SetActive(visible);
        if (!visible)
        {
            return;
        }

        if (useScreenCenterForDesktopHover)
        {
            crosshairRect.anchoredPosition = Vector2.zero;
        }
        else
        {
            crosshairRect.anchoredPosition = GetMousePositionOrCenter();
        }
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

        if (highlightPropertyBlock == null)
        {
            highlightPropertyBlock = new MaterialPropertyBlock();
        }

        highlightPropertyBlock.Clear();
        highlightPropertyBlock.SetColor("_Color", highlightColor);
        highlightPropertyBlock.SetColor("_BaseColor", highlightColor);
        highlightPropertyBlock.SetColor("_EmissionColor", highlightColor * 1.5f);
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

    static Vector2 GetMousePositionOrCenter()
    {
        return TryGetMousePosition(out Vector2 mousePosition)
            ? mousePosition
            : new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
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

    static bool WasPrimaryClickPressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
#else
        return Input.GetMouseButtonDown(0);
#endif
    }
}
