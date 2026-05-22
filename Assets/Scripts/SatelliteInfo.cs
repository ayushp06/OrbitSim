using UnityEngine;

public class SatelliteInfo : MonoBehaviour
{
    [SerializeField] SatelliteTleData tleData;
    [SerializeField] Color markerColor = Color.white;

    public SatelliteTleData TleData => tleData;
    public Color MarkerColor => markerColor;

    public void Initialize(SatelliteTleData data)
    {
        Initialize(data, SatelliteVisualMetadata.GetContinentColor(data));
    }

    public void Initialize(SatelliteTleData data, Color color)
    {
        tleData = data;
        markerColor = color;
        gameObject.name = string.IsNullOrWhiteSpace(data.satelliteName) ? "Satellite" : data.satelliteName;
    }
}
