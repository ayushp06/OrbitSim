using System.Globalization;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
#if ENABLE_VR || ENABLE_XR
using UnityEngine.XR;
#endif

public class SatelliteTleInfoPanel : MonoBehaviour
{
    public enum DisplayMode
    {
        DesktopOverlay,
        WorldSpaceHud,
        Auto
    }

    const string DefaultMessage = "Hover over a satellite to view TLE data.";

    [Header("Display")]
    public DisplayMode displayMode = DisplayMode.Auto;
    public Camera targetCamera;
    public bool showPanelWhenNotHovering = false;

    [Header("Desktop Layout")]
    public Vector2 panelSize = new Vector2(520f, 500f);
    public Vector2 screenMargin = new Vector2(24f, 24f);

    [Header("VR Layout")]
    public Vector3 worldSpaceLocalPosition = new Vector3(0.55f, 0.05f, 2.2f);
    public Vector2 worldSpacePanelSize = new Vector2(560f, 520f);
    public float worldSpaceScale = 0.0022f;

    [Header("Style")]
    public Color panelColor = new Color(0.02f, 0.03f, 0.05f, 0.82f);
    public Color textColor = new Color(0.92f, 0.96f, 1f, 1f);
    public int fontSize = 15;

    Canvas canvas;
    RectTransform panelRect;
    Text contentText;

    void Awake()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        BuildPanel();
        ShowDefaultMessage();
    }

    void OnEnable()
    {
        SatelliteHoverController.HoveredSatelliteChanged += HandleHoveredSatelliteChanged;
    }

    void OnDisable()
    {
        SatelliteHoverController.HoveredSatelliteChanged -= HandleHoveredSatelliteChanged;
    }

    void LateUpdate()
    {
        if (canvas != null && canvas.renderMode == RenderMode.WorldSpace && targetCamera != null)
        {
            transform.rotation = targetCamera.transform.rotation;
        }
    }

    void HandleHoveredSatelliteChanged(SatelliteInfo satelliteInfo)
    {
        if (satelliteInfo == null || satelliteInfo.TleData == null)
        {
            ShowDefaultMessage();
            return;
        }

        SetPanelVisible(true);
        contentText.text = BuildSatelliteText(satelliteInfo.TleData);
    }

    void ShowDefaultMessage()
    {
        contentText.text = DefaultMessage;
        SetPanelVisible(showPanelWhenNotHovering);
    }

    void SetPanelVisible(bool visible)
    {
        if (panelRect != null)
        {
            panelRect.gameObject.SetActive(visible);
        }
    }

    void BuildPanel()
    {
        canvas = gameObject.GetComponent<Canvas>();
        if (canvas == null)
        {
            canvas = gameObject.AddComponent<Canvas>();
        }

        CanvasScaler scaler = gameObject.GetComponent<CanvasScaler>();
        if (scaler == null)
        {
            scaler = gameObject.AddComponent<CanvasScaler>();
        }

        if (gameObject.GetComponent<GraphicRaycaster>() == null)
        {
            gameObject.AddComponent<GraphicRaycaster>();
        }

        bool useWorldSpace = ShouldUseWorldSpaceHud();
        ConfigureCanvas(useWorldSpace, scaler);

        GameObject panelObject = new GameObject("TLE Info Panel");
        panelObject.transform.SetParent(transform, false);
        panelRect = panelObject.AddComponent<RectTransform>();
        Image panelImage = panelObject.AddComponent<Image>();
        panelImage.color = panelColor;

        GameObject textObject = new GameObject("TLE Text");
        textObject.transform.SetParent(panelObject.transform, false);
        RectTransform textRect = textObject.AddComponent<RectTransform>();
        contentText = textObject.AddComponent<Text>();
        contentText.font = GetBuiltInFont();
        contentText.fontSize = fontSize;
        contentText.color = textColor;
        contentText.alignment = TextAnchor.UpperLeft;
        contentText.horizontalOverflow = HorizontalWrapMode.Wrap;
        contentText.verticalOverflow = VerticalWrapMode.Truncate;
        contentText.supportRichText = false;

        ConfigurePanelLayout(useWorldSpace, textRect);
    }

    void ConfigureCanvas(bool useWorldSpace, CanvasScaler scaler)
    {
        if (useWorldSpace)
        {
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = targetCamera;
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;

            Transform parent = targetCamera != null ? targetCamera.transform : transform.parent;
            transform.SetParent(parent, false);
            transform.localPosition = worldSpaceLocalPosition;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one * worldSpaceScale;
            return;
        }

        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
    }

    void ConfigurePanelLayout(bool useWorldSpace, RectTransform textRect)
    {
        Vector2 size = useWorldSpace ? worldSpacePanelSize : panelSize;
        panelRect.sizeDelta = size;

        if (useWorldSpace)
        {
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(1f, 0.5f);
            panelRect.anchoredPosition = Vector2.zero;
        }
        else
        {
            panelRect.anchorMin = new Vector2(1f, 1f);
            panelRect.anchorMax = new Vector2(1f, 1f);
            panelRect.pivot = new Vector2(1f, 1f);
            panelRect.anchoredPosition = new Vector2(-screenMargin.x, -screenMargin.y);
        }

        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(16f, 14f);
        textRect.offsetMax = new Vector2(-16f, -14f);
    }

    bool ShouldUseWorldSpaceHud()
    {
        if (displayMode == DisplayMode.WorldSpaceHud)
        {
            return true;
        }

        if (displayMode == DisplayMode.DesktopOverlay)
        {
            return false;
        }

#if ENABLE_VR || ENABLE_XR
        return XRSettings.enabled;
#else
        return false;
#endif
    }

    string BuildSatelliteText(SatelliteTleData data)
    {
        var builder = new StringBuilder(768);
        builder.AppendLine(data.satelliteName);
        builder.AppendLine();
        AppendValue(builder, "Country of Origin", data.hasCountryOfOrigin, data.countryOfOrigin);
        AppendValue(builder, "Owner / Operator", data.hasOwnerOperator, data.ownerOperator);
        AppendValue(builder, "Mission", data.hasMission, data.mission);
        AppendValue(builder, "NORAD ID", data.hasNoradCatalogId, data.noradCatalogId.ToString(CultureInfo.InvariantCulture));
        AppendValue(builder, "International Designator", data.hasInternationalDesignator, data.internationalDesignator);
        AppendValue(builder, "Classification", data.hasClassification, data.classification);
        AppendValue(builder, "Epoch", data.hasEpoch, data.epoch);
        AppendValue(builder, "Inclination", data.hasInclination, FormatDegrees(data.inclination));
        AppendValue(builder, "RAAN", data.hasRaan, FormatDegrees(data.raan));
        AppendValue(builder, "Eccentricity", data.hasEccentricity, data.eccentricity.ToString("0.0000000", CultureInfo.InvariantCulture));
        AppendValue(builder, "Arg Perigee", data.hasArgumentOfPerigee, FormatDegrees(data.argumentOfPerigee));
        AppendValue(builder, "Mean Anomaly", data.hasMeanAnomaly, FormatDegrees(data.meanAnomaly));
        AppendValue(builder, "Mean Motion", data.hasMeanMotion, data.meanMotion.ToString("0.00000000", CultureInfo.InvariantCulture) + " rev/day");
        if (!string.IsNullOrWhiteSpace(data.dataSource))
        {
            AppendValue(builder, "TLE Source", true, data.dataSource);
        }

        builder.AppendLine();
        builder.AppendLine("TLE Line 1:");
        builder.AppendLine(data.line1);
        builder.AppendLine("TLE Line 2:");
        builder.AppendLine(data.line2);
        return builder.ToString();
    }

    static void AppendValue(StringBuilder builder, string label, bool hasValue, string value)
    {
        builder.Append(label);
        builder.Append(": ");
        builder.AppendLine(hasValue ? value : "N/A");
    }

    static string FormatDegrees(double degrees)
    {
        return degrees.ToString("0.0000", CultureInfo.InvariantCulture) + " deg";
    }

    static Font GetBuiltInFont()
    {
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null)
        {
            font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        return font;
    }
}
