using UnityEngine;

public class SatelliteInfo : MonoBehaviour
{
    [SerializeField] SatelliteTleData tleData;

    public SatelliteTleData TleData => tleData;

    public void Initialize(SatelliteTleData data)
    {
        tleData = data;
        gameObject.name = string.IsNullOrWhiteSpace(data.satelliteName) ? "Satellite" : data.satelliteName;
    }
}
