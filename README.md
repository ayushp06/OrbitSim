# OrbitSim

OrbitSim is a Unity VR/desktop simulation for visualizing LEO satellites around a 3D Earth globe in space. The default scene loads a bundled 100-satellite LEO sample catalog from StreamingAssets. The app propagates satellite positions around Earth, renders satellite markers, and shows origin and TLE details when a user hovers over a satellite.

## Required Unity Version

Open this project with Unity:

```text
6000.3.7f1
```

The version is recorded in `ProjectSettings/ProjectVersion.txt`.

## Required Packages

Unity Package Manager dependencies are defined in `Packages/manifest.json`. The important project packages are:

- `com.unity.inputsystem` `1.18.0`: desktop camera and keyboard controls.
- `com.unity.ugui` `2.0.0`: hover TLE information panel.
- `com.unity.visualscripting` `1.9.9`: present in the project, not central to the satellite simulation.
- `com.unity.collab-proxy` `2.11.3`: Unity Version Control integration package.
- `com.unity.timeline` `1.8.10`: installed by the Unity project template, not central to the satellite simulation.
- `com.unity.modules.physics`: raycast hover detection and marker colliders.
- `com.unity.modules.vr` and `com.unity.modules.xr`: base Unity XR modules.

NuGetForUnity is checked in under `Assets/NuGet/`, and the NuGet package list is stored in:

```text
Assets/packages.config
```

SGP4 propagation is supplied by the checked-in SGP.NET `1.4.0` DLL:

```text
Assets/Packages/SGP.NET.1.4.0/lib/netstandard2.0/SGP.NET.dll
```

OpenXR, XR Plugin Management, and XR Interaction Toolkit are not currently installed. The scene has an `XR` hierarchy and VR-aware code paths, but production VR controller support still needs those packages configured for the target headset.

## Opening The Unity Project

1. Open Unity Hub.
2. Select **Add project from disk**.
3. Choose the repository folder:

```text
D:\Unity\EarthSatOrbitSim
```

4. Open the project with Unity `6000.3.7f1`.
5. Open the main scene:

```text
Assets/Scenes/SampleScene.unity
```

## Scene Overview

`Assets/Scenes/SampleScene.unity` is organized under `SceneRoot`:

- `Earth`: Earth transform and textured globe mesh.
- `Satellites`: parent for spawned satellite markers.
- `OrbitVisuals`: parent for generated orbit line renderers.
- `XR`: desktop camera/player organization and future VR rig location.
- `UI`: runtime TLE information panel placeholder.
- `Managers`: simulation components and bootstrap objects.

The main runtime object is `Managers/SimulationManager`, which contains:

- `TleLoader`
- `SatelliteManager`
- `SatelliteHoverController`

## TLE Data Loading

TLE loading is handled by:

```text
Assets/Scripts/TleLoader.cs
Assets/Scripts/SatelliteTleData.cs
```

`TleLoader` can download a CelesTrak GP TLE in Play mode and can fall back to a local text file from `StreamingAssets`. The default runtime URL is:

```text
https://celestrak.org/NORAD/elements/gp.php?CATNR=33591&FORMAT=TLE
```

The default local catalog path is:

```text
Assets/StreamingAssets/TLE/leo-sample.tle
```

Both are configured on `Managers/SimulationManager`.

The loader expects standard 3-line TLE records:

```text
SATELLITE NAME
1 NNNNNU ...
2 NNNNN ...
```

`SatelliteTleData` stores the raw lines and parses:

- satellite name
- NORAD catalog ID
- international designator
- classification
- epoch
- inclination
- RAAN
- eccentricity
- argument of perigee
- mean anomaly
- mean motion

The hover panel also shows known catalog metadata for the bundled sample satellites, including country of origin, owner/operator, and mission.

Malformed records are skipped or reported with warnings so valid entries can continue loading.

## Adding New TLE Files

Place new TLE text files under:

```text
Assets/StreamingAssets/TLE/
```

Then select `Managers/SimulationManager` in `SampleScene.unity` and update:

```text
TleLoader.streamingAssetsRelativePath
```

For example:

```text
TLE/my-new-catalog.tle
```

Use `SatelliteManager.maxSatellitesForTesting` to limit the number of loaded markers while testing large catalogs.

## Running The Scene

1. Open `Assets/Scenes/SampleScene.unity`.
2. Press Play in the Unity Editor.
3. The scene loads the local `leo-sample.tle` catalog.
4. `SatelliteManager` filters likely LEO satellites when `showOnlyLeoSatellites` is enabled.
5. The current demo configuration spawns 100 bright point-mass satellite markers and disables orbit paths.
6. Satellite marker colors indicate origin continent, with a key in the top-left corner.
7. Hover over a satellite marker with the visible mouse crosshair to view country of origin, operator, mission, country flag markers, TLE source, parsed orbital fields, and raw TLE lines in the top-right panel.

To show more satellites later, raise or clear `SatelliteManager.maxSatellitesForTesting`. Keep `orbitLinesEnabled` off for a point-mass-only view.

## Optional SGP.NET Smoke Test

The repository includes a small SGP.NET verification component:

```text
Assets/Scripts/SgpNetSmokeTest.cs
```

Add this component to an enabled GameObject in a temporary test scene or attach it to an existing test object, then enter Play mode. It constructs a sample satellite from an ISS TLE and logs either:

```text
SGP.NET smoke test OK.
```

or an exception if the checked-in SGP.NET assembly cannot be loaded or used. This script is only a dependency smoke test; it is not required for the default scene.

## Desktop Controls

Desktop navigation uses `Assets/Scripts/CameraControls.cs` with Unity's Input System:

- The crosshair stays fixed at the center of the screen.
- Move the mouse to rotate the camera and point the centered crosshair at a satellite marker.
- Point the crosshair at a satellite marker to hover it and show TLE data.
- `W`, `A`, `S`, `D`: move camera.
- `Space`: move up.
- `Left Shift` or `Right Shift`: move down.
- `Left Ctrl`: movement boost.
- `Escape`: toggle cursor lock.

Simulation time controls are handled by `SatelliteManager`:

- `P`: pause or resume simulation time.
- `=` or numpad `+`: multiply simulation speed by `speedStepMultiplier`.
- `-` or numpad `-`: divide simulation speed by `speedStepMultiplier`.

## VR Controls

The project currently includes VR-aware structure but not a complete production XR setup.

Current behavior:

- `SatelliteTleInfoPanel` can switch to a world-space HUD when XR is active.
- `SatelliteHoverController` supports VR-style ray hover if ray origin transforms are assigned.
- No grab interaction is required for satellites.

Required future package/setup work for full VR controls:

- Install and configure XR Plugin Management.
- Install and configure OpenXR for the target headset.
- Install XR Interaction Toolkit if controller ray interactors are desired.
- Assign VR controller ray origin transforms to `SatelliteHoverController.vrRayOrigins`.
- Bind pause/play and speed controls to VR UI buttons or controller inputs.

## Hover-To-View-TLE

Hover behavior is implemented by:

```text
Assets/Scripts/SatelliteHoverController.cs
Assets/Scripts/SatelliteInfo.cs
Assets/Scripts/SatelliteTleInfoPanel.cs
```

Flow:

1. `SatelliteManager` spawns one marker per loaded satellite.
2. Each marker receives a `SatelliteInfo` component containing its parsed `SatelliteTleData` and continent color.
3. `SatelliteHoverController` raycasts against satellite markers.
4. The hovered marker is highlighted.
5. Hover changes are emitted through a UnityEvent and a static C# event.
6. `SatelliteTleInfoPanel` listens for hover changes.
7. When no satellite is hovered, the panel shows:

```text
Hover over a satellite to view TLE data.
```

8. When a satellite is hovered, the panel displays name, country of origin, owner/operator, mission, NORAD ID, international designator, classification, epoch, inclination, RAAN, eccentricity, argument of perigee, mean anomaly, mean motion, source, and raw TLE lines.

## Orbit Propagation And Scale

`SatelliteManager` uses SGP.NET when possible to predict satellite ECI positions from TLE data. If SGP4 setup or prediction fails for an entry, the manager falls back to a lightweight Keplerian visualization based on parsed TLE orbital elements.

The fallback is approximate and is intended for visual continuity, not authoritative orbital analysis. It ignores drag, J2/precession, decay, and other perturbation effects handled by full SGP4.

The scene uses a readable visualization scale:

- `earthRadiusWorldUnits` maps Earth's real radius, 6371 km, to the globe radius in Unity units.
- `orbitAltitudeExaggeration` multiplies only altitude above Earth so LEO satellites are visible.
- Satellite directions come from SGP4 or the Keplerian fallback, preserving distribution around the globe.
- Set `orbitAltitudeExaggeration` to `1` for physically proportional altitude scale.

## Performance Settings

Important `SatelliteManager` settings:

- `showOnlyLeoSatellites`: filters spawned markers to likely LEO satellites.
- `maxSatellitesForTesting`: limits marker count for performance testing. The current demo uses `100`.
- `orbitLinesEnabled`: enables or disables orbit lines. The current demo keeps this off for point-mass-only satellite visuals.
- `maxOrbitLines`: caps orbit line count.
- `satellitePositionUpdateInterval`: controls how often marker positions update.
- `maxSatellitePositionUpdatesPerFrame`: optionally spreads propagation work across frames.
- `shareMarkerMaterial`: uses a shared instancing-capable material for markers.

Markers are spawned once and updated centrally. Marker transforms are cached so the hot update path avoids unnecessary `GetComponent` calls.

## Known Limitations

- Full VR package setup is not complete. OpenXR, XR Plugin Management, and XR Interaction Toolkit still need to be added for production VR controller workflows.
- SGP.NET is available and used when possible, but orbit-line previews and fallback propagation are approximate Keplerian visualizations.
- The scale model intentionally exaggerates LEO altitude unless `orbitAltitudeExaggeration` is set to `1`.
- The included TLE file is a curated 100-satellite LEO sample, not a complete live catalog.
- Runtime TLE download depends on network access to CelesTrak; offline runs use the local fallback.
- Very large catalogs may still need pooling, GPU instanced mesh rendering, culling, and deeper profiling for VR frame rates.

## Future Improvements

- Add full OpenXR and XR Interaction Toolkit setup.
- Add VR controller UI for pause/play, speed, LEO filtering, and selected satellite details.
- Add live or user-imported TLE catalog management.
- Add object pooling for satellite markers and orbit lines.
- Replace marker GameObjects with GPU-instanced mesh rendering for very large catalogs.
- Add selective orbit rendering for hovered/selected satellites only.
- Add tests for TLE parsing, LEO classification, and coordinate conversion.
- Add build targets and profiling notes for the intended VR headset.
