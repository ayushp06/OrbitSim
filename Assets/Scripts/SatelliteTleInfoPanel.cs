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
    [Tooltip("Maximum panel size. The panel shrinks to fit the active satellite data.")]
    public Vector2 panelSize = new Vector2(520f, 500f);
    public Vector2 screenMargin = new Vector2(24f, 24f);

    [Header("VR Layout")]
    public Vector3 worldSpaceLocalPosition = new Vector3(0.55f, 0.05f, 2.2f);
    [Tooltip("Maximum world-space panel size. The panel shrinks to fit the active satellite data.")]
    public Vector2 worldSpacePanelSize = new Vector2(560f, 520f);
    public float worldSpaceScale = 0.0022f;

    [Header("Style")]
    public Color panelColor = new Color(0.02f, 0.03f, 0.05f, 0.82f);
    public Color textColor = new Color(0.92f, 0.96f, 1f, 1f);
    public int fontSize = 15;
    public Vector2 contentPadding = new Vector2(16f, 14f);
    public Vector2 minimumPanelSize = new Vector2(280f, 160f);
    public Vector2 flagMarkerSize = new Vector2(26f, 16f);
    public float flagMarkerSpacing = 5f;
    public float headerBodySpacing = 8f;

    Canvas canvas;
    RectTransform panelRect;
    RectTransform titleRect;
    RectTransform flagContainerRect;
    RectTransform detailsRect;
    Text titleText;
    Text contentText;
    bool currentUseWorldSpace;

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

        titleText.text = satelliteInfo.TleData.satelliteName;
        contentText.text = BuildSatelliteText(satelliteInfo.TleData);
        RebuildFlagMarkers(satelliteInfo.TleData);
        ResizePanelToContent();
        SetPanelVisible(true);
    }

    void ShowDefaultMessage()
    {
        titleText.text = string.Empty;
        contentText.text = DefaultMessage;
        ClearFlagMarkers();
        ResizePanelToContent();
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

        currentUseWorldSpace = ShouldUseWorldSpaceHud();
        ConfigureCanvas(currentUseWorldSpace, scaler);

        GameObject panelObject = new GameObject("TLE Info Panel");
        panelObject.transform.SetParent(transform, false);
        panelRect = panelObject.AddComponent<RectTransform>();
        Image panelImage = panelObject.AddComponent<Image>();
        panelImage.color = panelColor;

        GameObject titleObject = new GameObject("Satellite Title");
        titleObject.transform.SetParent(panelObject.transform, false);
        titleRect = titleObject.AddComponent<RectTransform>();
        titleText = titleObject.AddComponent<Text>();
        titleText.font = GetBuiltInFont();
        titleText.fontSize = fontSize + 2;
        titleText.fontStyle = FontStyle.Bold;
        titleText.color = textColor;
        titleText.alignment = TextAnchor.UpperLeft;
        titleText.horizontalOverflow = HorizontalWrapMode.Overflow;
        titleText.verticalOverflow = VerticalWrapMode.Truncate;
        titleText.supportRichText = false;

        GameObject flagContainerObject = new GameObject("Country Flag Markers");
        flagContainerObject.transform.SetParent(panelObject.transform, false);
        flagContainerRect = flagContainerObject.AddComponent<RectTransform>();

        GameObject textObject = new GameObject("TLE Text");
        textObject.transform.SetParent(panelObject.transform, false);
        detailsRect = textObject.AddComponent<RectTransform>();
        contentText = textObject.AddComponent<Text>();
        contentText.font = GetBuiltInFont();
        contentText.fontSize = fontSize;
        contentText.color = textColor;
        contentText.alignment = TextAnchor.UpperLeft;
        contentText.horizontalOverflow = HorizontalWrapMode.Wrap;
        contentText.verticalOverflow = VerticalWrapMode.Truncate;
        contentText.supportRichText = false;

        ConfigurePanelLayout(currentUseWorldSpace);
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

    void ConfigurePanelLayout(bool useWorldSpace)
    {
        panelRect.sizeDelta = useWorldSpace ? worldSpacePanelSize : panelSize;

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

        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(0f, 1f);
        titleRect.pivot = new Vector2(0f, 1f);

        flagContainerRect.anchorMin = new Vector2(0f, 1f);
        flagContainerRect.anchorMax = new Vector2(0f, 1f);
        flagContainerRect.pivot = new Vector2(0f, 1f);

        detailsRect.anchorMin = new Vector2(0f, 1f);
        detailsRect.anchorMax = new Vector2(0f, 1f);
        detailsRect.pivot = new Vector2(0f, 1f);
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
        var builder = new StringBuilder(512);
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

    void ResizePanelToContent()
    {
        if (panelRect == null || titleText == null || contentText == null)
        {
            return;
        }

        Vector2 maxSize = currentUseWorldSpace ? worldSpacePanelSize : panelSize;
        float maxContentWidth = Mathf.Max(120f, maxSize.x - contentPadding.x * 2f);
        float headerHeight = Mathf.Max(fontSize + 6f, flagMarkerSize.y);
        float titleWidth = string.IsNullOrWhiteSpace(titleText.text)
            ? 0f
            : GetPreferredSize(titleText, titleText.text, maxContentWidth, headerHeight).x;
        float flagWidth = GetFlagMarkersWidth();
        float detailsPreferredWidth = GetPreferredSize(contentText, contentText.text, maxContentWidth, 0f).x;
        float desiredContentWidth = Mathf.Max(titleWidth + flagWidth, detailsPreferredWidth);
        float contentWidth = Mathf.Clamp(desiredContentWidth, minimumPanelSize.x - contentPadding.x * 2f, maxContentWidth);
        float detailsHeight = GetPreferredSize(contentText, contentText.text, contentWidth, 0f).y;
        float titleAreaWidth = Mathf.Max(0f, contentWidth - flagWidth);

        bool hasHeader = !string.IsNullOrWhiteSpace(titleText.text);
        float desiredHeight = contentPadding.y * 2f + detailsHeight;
        if (hasHeader)
        {
            desiredHeight += headerHeight + headerBodySpacing;
        }

        Vector2 size = new Vector2(
            Mathf.Clamp(contentWidth + contentPadding.x * 2f, minimumPanelSize.x, maxSize.x),
            Mathf.Clamp(desiredHeight, minimumPanelSize.y, maxSize.y));

        panelRect.sizeDelta = size;

        float y = -contentPadding.y;
        titleRect.gameObject.SetActive(hasHeader);
        flagContainerRect.gameObject.SetActive(hasHeader && flagContainerRect.childCount > 0);
        if (hasHeader)
        {
            titleRect.anchoredPosition = new Vector2(contentPadding.x, y);
            titleRect.sizeDelta = new Vector2(titleAreaWidth, headerHeight);

            flagContainerRect.anchoredPosition = new Vector2(contentPadding.x + titleAreaWidth + flagMarkerSpacing, y - 1f);
            flagContainerRect.sizeDelta = new Vector2(flagWidth, headerHeight);
            y -= headerHeight + headerBodySpacing;
        }

        detailsRect.anchoredPosition = new Vector2(contentPadding.x, y);
        detailsRect.sizeDelta = new Vector2(contentWidth, Mathf.Max(0f, size.y + y - contentPadding.y));
    }

    static Vector2 GetPreferredSize(Text text, string value, float width, float height)
    {
        if (text == null)
        {
            return Vector2.zero;
        }

        TextGenerationSettings settings = text.GetGenerationSettings(new Vector2(width, height));
        float pixelsPerUnit = text.pixelsPerUnit;
        float preferredWidth = text.cachedTextGeneratorForLayout.GetPreferredWidth(value, settings) / pixelsPerUnit;
        float preferredHeight = text.cachedTextGeneratorForLayout.GetPreferredHeight(value, settings) / pixelsPerUnit;
        return new Vector2(preferredWidth, preferredHeight);
    }

    void RebuildFlagMarkers(SatelliteTleData data)
    {
        ClearFlagMarkers();

        string[] countryCodes = GetCountryCodes(data);
        for (int i = 0; i < countryCodes.Length; i++)
        {
            GameObject flagObject = new GameObject(countryCodes[i] + " Flag");
            flagObject.transform.SetParent(flagContainerRect, false);

            RectTransform flagRect = flagObject.AddComponent<RectTransform>();
            flagRect.anchorMin = new Vector2(0f, 1f);
            flagRect.anchorMax = new Vector2(0f, 1f);
            flagRect.pivot = new Vector2(0f, 1f);
            flagRect.sizeDelta = flagMarkerSize;
            flagRect.anchoredPosition = new Vector2(i * (flagMarkerSize.x + flagMarkerSpacing), 0f);

            RawImage flagImage = flagObject.AddComponent<RawImage>();
            flagImage.texture = CreateFlagTexture(countryCodes[i]);
            flagImage.raycastTarget = false;
        }
    }

    void ClearFlagMarkers()
    {
        if (flagContainerRect == null)
        {
            return;
        }

        for (int i = flagContainerRect.childCount - 1; i >= 0; i--)
        {
            Destroy(flagContainerRect.GetChild(i).gameObject);
        }
    }

    float GetFlagMarkersWidth()
    {
        int flagCount = flagContainerRect != null ? flagContainerRect.childCount : 0;
        if (flagCount == 0)
        {
            return 0f;
        }

        return flagCount * flagMarkerSize.x + (flagCount - 1) * flagMarkerSpacing + flagMarkerSpacing;
    }

    static string[] GetCountryCodes(SatelliteTleData data)
    {
        return SatelliteVisualMetadata.GetCountryCodes(data);
    }

    static Texture2D CreateFlagTexture(string countryCode)
    {
        const int width = 52;
        const int height = 32;
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

        switch (countryCode)
        {
            case "EU":
                DrawSolid(pixels, width, height, new Color32(0, 51, 153, 255));
                DrawCircleRing(pixels, width, height, new Vector2(width * 0.5f, height * 0.5f), 9f, new Color32(255, 204, 0, 255));
                break;
            case "FR":
                DrawVerticalBand(pixels, width, height, 0, width / 3, new Color32(0, 38, 84, 255));
                DrawVerticalBand(pixels, width, height, width / 3, width / 3, Color.white);
                DrawVerticalBand(pixels, width, height, width * 2 / 3, width - width * 2 / 3, new Color32(206, 17, 38, 255));
                break;
            case "INTL":
                DrawSolid(pixels, width, height, new Color32(8, 20, 34, 255));
                DrawDisc(pixels, width, height, new Vector2(width * 0.5f, height * 0.5f), 10f, new Color32(70, 180, 255, 255));
                DrawHorizontalBand(pixels, width, height, height / 2 - 1, 2, new Color32(8, 20, 34, 255));
                DrawVerticalBand(pixels, width, height, width / 2 - 1, 2, new Color32(8, 20, 34, 255));
                break;
            case "JP":
                DrawSolid(pixels, width, height, Color.white);
                DrawDisc(pixels, width, height, new Vector2(width * 0.5f, height * 0.5f), 8f, new Color32(188, 0, 45, 255));
                break;
            case "CN":
                DrawSolid(pixels, width, height, new Color32(222, 41, 16, 255));
                DrawDisc(pixels, width, height, new Vector2(width * 0.24f, height * 0.68f), 4f, new Color32(255, 222, 0, 255));
                break;
            case "AR":
                DrawHorizontalBand(pixels, width, height, 0, height / 3, new Color32(116, 172, 223, 255));
                DrawHorizontalBand(pixels, width, height, height / 3, height / 3, Color.white);
                DrawHorizontalBand(pixels, width, height, height * 2 / 3, height - height * 2 / 3, new Color32(116, 172, 223, 255));
                DrawDisc(pixels, width, height, new Vector2(width * 0.5f, height * 0.5f), 2f, new Color32(246, 180, 14, 255));
                break;
            case "AU":
                DrawSolid(pixels, width, height, new Color32(0, 32, 91, 255));
                DrawRect(pixels, width, height, 0, height / 2, width / 2, height / 2, new Color32(1, 33, 105, 255));
                DrawHorizontalBand(pixels, width, height, height * 3 / 4 - 1, 2, Color.white);
                DrawVerticalBand(pixels, width, height, width / 4 - 1, 2, Color.white);
                DrawDisc(pixels, width, height, new Vector2(width * 0.75f, height * 0.38f), 2.5f, Color.white);
                break;
            case "BR":
                DrawSolid(pixels, width, height, new Color32(0, 156, 59, 255));
                DrawDiamond(pixels, width, height, new Vector2(width * 0.5f, height * 0.5f), 18f, 11f, new Color32(255, 223, 0, 255));
                DrawDisc(pixels, width, height, new Vector2(width * 0.5f, height * 0.5f), 6f, new Color32(0, 39, 118, 255));
                break;
            case "CA":
                DrawVerticalBand(pixels, width, height, 0, width / 4, new Color32(255, 0, 0, 255));
                DrawVerticalBand(pixels, width, height, width / 4, width / 2, Color.white);
                DrawVerticalBand(pixels, width, height, width * 3 / 4, width / 4, new Color32(255, 0, 0, 255));
                DrawDisc(pixels, width, height, new Vector2(width * 0.5f, height * 0.5f), 4f, new Color32(255, 0, 0, 255));
                break;
            case "DE":
                DrawHorizontalBand(pixels, width, height, 0, height / 3, new Color32(0, 0, 0, 255));
                DrawHorizontalBand(pixels, width, height, height / 3, height / 3, new Color32(221, 0, 0, 255));
                DrawHorizontalBand(pixels, width, height, height * 2 / 3, height - height * 2 / 3, new Color32(255, 206, 0, 255));
                break;
            case "ES":
                DrawHorizontalBand(pixels, width, height, 0, height / 4, new Color32(170, 21, 27, 255));
                DrawHorizontalBand(pixels, width, height, height / 4, height / 2, new Color32(241, 191, 0, 255));
                DrawHorizontalBand(pixels, width, height, height * 3 / 4, height / 4, new Color32(170, 21, 27, 255));
                break;
            case "GB":
                DrawSolid(pixels, width, height, new Color32(1, 33, 105, 255));
                DrawHorizontalBand(pixels, width, height, height / 2 - 3, 6, Color.white);
                DrawVerticalBand(pixels, width, height, width / 2 - 3, 6, Color.white);
                DrawHorizontalBand(pixels, width, height, height / 2 - 1, 2, new Color32(200, 16, 46, 255));
                DrawVerticalBand(pixels, width, height, width / 2 - 1, 2, new Color32(200, 16, 46, 255));
                break;
            case "IL":
                DrawSolid(pixels, width, height, Color.white);
                DrawHorizontalBand(pixels, width, height, 4, 3, new Color32(0, 56, 184, 255));
                DrawHorizontalBand(pixels, width, height, height - 7, 3, new Color32(0, 56, 184, 255));
                DrawCircleRing(pixels, width, height, new Vector2(width * 0.5f, height * 0.5f), 5f, new Color32(0, 56, 184, 255));
                break;
            case "IN":
                DrawHorizontalBand(pixels, width, height, 0, height / 3, new Color32(255, 153, 51, 255));
                DrawHorizontalBand(pixels, width, height, height / 3, height / 3, Color.white);
                DrawHorizontalBand(pixels, width, height, height * 2 / 3, height - height * 2 / 3, new Color32(19, 136, 8, 255));
                DrawCircleRing(pixels, width, height, new Vector2(width * 0.5f, height * 0.5f), 4f, new Color32(0, 0, 128, 255));
                break;
            case "ID":
                DrawHorizontalBand(pixels, width, height, 0, height / 2, new Color32(206, 17, 38, 255));
                DrawHorizontalBand(pixels, width, height, height / 2, height - height / 2, Color.white);
                break;
            case "IR":
                DrawHorizontalBand(pixels, width, height, 0, height / 3, new Color32(35, 159, 64, 255));
                DrawHorizontalBand(pixels, width, height, height / 3, height / 3, Color.white);
                DrawHorizontalBand(pixels, width, height, height * 2 / 3, height - height * 2 / 3, new Color32(218, 0, 0, 255));
                break;
            case "IT":
                DrawVerticalBand(pixels, width, height, 0, width / 3, new Color32(0, 146, 70, 255));
                DrawVerticalBand(pixels, width, height, width / 3, width / 3, Color.white);
                DrawVerticalBand(pixels, width, height, width * 2 / 3, width - width * 2 / 3, new Color32(206, 43, 55, 255));
                break;
            case "KR":
                DrawSolid(pixels, width, height, Color.white);
                DrawDisc(pixels, width, height, new Vector2(width * 0.5f, height * 0.5f), 6f, new Color32(205, 46, 58, 255));
                DrawDisc(pixels, width, height, new Vector2(width * 0.5f, height * 0.5f - 3f), 3f, new Color32(0, 71, 160, 255));
                DrawRect(pixels, width, height, 9, 8, 9, 2, Color.black);
                DrawRect(pixels, width, height, width - 18, height - 10, 9, 2, Color.black);
                break;
            case "RU":
                DrawHorizontalBand(pixels, width, height, 0, height / 3, Color.white);
                DrawHorizontalBand(pixels, width, height, height / 3, height / 3, new Color32(0, 57, 166, 255));
                DrawHorizontalBand(pixels, width, height, height * 2 / 3, height - height * 2 / 3, new Color32(213, 43, 30, 255));
                break;
            case "SA":
                DrawSolid(pixels, width, height, new Color32(0, 108, 53, 255));
                DrawHorizontalBand(pixels, width, height, height / 2, 2, Color.white);
                break;
            case "SE":
                DrawSolid(pixels, width, height, new Color32(0, 106, 167, 255));
                DrawHorizontalBand(pixels, width, height, height / 2 - 3, 6, new Color32(254, 204, 0, 255));
                DrawVerticalBand(pixels, width, height, width / 3 - 3, 6, new Color32(254, 204, 0, 255));
                break;
            case "TH":
                DrawHorizontalBand(pixels, width, height, 0, height / 6, new Color32(165, 25, 49, 255));
                DrawHorizontalBand(pixels, width, height, height / 6, height / 6, Color.white);
                DrawHorizontalBand(pixels, width, height, height / 3, height / 3, new Color32(45, 42, 74, 255));
                DrawHorizontalBand(pixels, width, height, height * 2 / 3, height / 6, Color.white);
                DrawHorizontalBand(pixels, width, height, height * 5 / 6, height - height * 5 / 6, new Color32(165, 25, 49, 255));
                break;
            default:
                DrawUnitedStatesFlag(pixels, width, height);
                break;
        }

        texture.SetPixels32(pixels);
        texture.Apply();
        return texture;
    }

    static void DrawUnitedStatesFlag(Color32[] pixels, int width, int height)
    {
        Color32 red = new Color32(179, 25, 66, 255);
        Color32 blue = new Color32(10, 49, 97, 255);
        for (int stripe = 0; stripe < 13; stripe++)
        {
            int y = Mathf.RoundToInt(stripe * height / 13f);
            int nextY = Mathf.RoundToInt((stripe + 1) * height / 13f);
            DrawHorizontalBand(pixels, width, height, y, Mathf.Max(1, nextY - y), stripe % 2 == 0 ? red : Color.white);
        }

        DrawRect(pixels, width, height, 0, height - Mathf.RoundToInt(height * 7f / 13f), Mathf.RoundToInt(width * 0.42f), Mathf.RoundToInt(height * 7f / 13f), blue);
    }

    static void DrawSolid(Color32[] pixels, int width, int height, Color32 color)
    {
        DrawRect(pixels, width, height, 0, 0, width, height, color);
    }

    static void DrawVerticalBand(Color32[] pixels, int width, int height, int x, int bandWidth, Color32 color)
    {
        DrawRect(pixels, width, height, x, 0, bandWidth, height, color);
    }

    static void DrawHorizontalBand(Color32[] pixels, int width, int height, int y, int bandHeight, Color32 color)
    {
        DrawRect(pixels, width, height, 0, y, width, bandHeight, color);
    }

    static void DrawRect(Color32[] pixels, int width, int height, int x, int y, int rectWidth, int rectHeight, Color32 color)
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

    static void DrawDisc(Color32[] pixels, int width, int height, Vector2 center, float radius, Color32 color)
    {
        float radiusSquared = radius * radius;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Vector2 delta = new Vector2(x + 0.5f, y + 0.5f) - center;
                if (delta.sqrMagnitude <= radiusSquared)
                {
                    pixels[y * width + x] = color;
                }
            }
        }
    }

    static void DrawDiamond(Color32[] pixels, int width, int height, Vector2 center, float halfWidth, float halfHeight, Color32 color)
    {
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float normalized = Mathf.Abs((x + 0.5f - center.x) / halfWidth) + Mathf.Abs((y + 0.5f - center.y) / halfHeight);
                if (normalized <= 1f)
                {
                    pixels[y * width + x] = color;
                }
            }
        }
    }

    static void DrawCircleRing(Color32[] pixels, int width, int height, Vector2 center, float radius, Color32 color)
    {
        for (int i = 0; i < 12; i++)
        {
            float angle = i * Mathf.PI * 2f / 12f;
            Vector2 point = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
            DrawDisc(pixels, width, height, point, 1.2f, color);
        }
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
