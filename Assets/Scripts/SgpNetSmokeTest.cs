using System;
using SGPdotNET.CoordinateSystem;
using SGPdotNET.Observation;
using SGPdotNET.Util;
using UnityEngine;

public class SgpNetSmokeTest : MonoBehaviour
{
    [TextArea] public string tleName = "ISS (ZARYA)";
    [TextArea] public string tleLine1 = "1 25544U 98067A   19034.73310439  .00001974  00000-0  38215-4 0  9991";
    [TextArea] public string tleLine2 = "2 25544  51.6436 304.9146 0005074 348.4622  36.8575 15.53228055154526";

    void Start()
    {
        try
        {
            // Construct satellite from 3-line TLE (name + two lines)
            var sat = new Satellite(tleName, tleLine1, tleLine2);

            // A known reference location (Statue of Liberty example from repo)
            var location = new GeodeticCoordinate(
                Angle.FromDegrees(40.689236),
                Angle.FromDegrees(-74.044563),
                0
            );

            var groundStation = new GroundStation(location);

            // Propagate/observe at a known UTC time (or DateTime.UtcNow)
            var utc = new DateTime(2019, 3, 5, 3, 45, 12, DateTimeKind.Utc);
            var observation = groundStation.Observe(sat, utc);

            Debug.Log($"SGP.NET smoke test OK.\nUTC: {utc:o}\n{observation}");
        }
        catch (Exception ex)
        {
            Debug.LogError("SGP.NET smoke test FAILED:\n" + ex);
        }
    }
}
