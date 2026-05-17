# OrbitSim

OrbitSim is a Unity VR/desktop simulation for visualizing LEO satellites around a 3D Earth globe in space. The simulation loads local Two-Line Element data, filters likely LEO satellites, propagates their positions around Earth, renders satellite markers and optional orbit lines, and shows TLE details when a user hovers over a satellite.

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
- `com.unity.modules.physics`: raycast hover detection and marker colliders.
- `com.unity.modules.vr` and `com.unity.modules.xr`: base Unity XR modules.

SGP4 propagation is supplied by the checked-in SGP.NET DLL:

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

`TleLoader` reads a local text file from `StreamingAssets`. The default configured path is:

```text
Assets/StreamingAssets/TLE/leo-sample.tle
```

The loader expects standard 3-line TLE records:

```text
SATELLITE NAME
1 NNNNNU ...
2 NNNNN ...
```

`SatelliteTleData` stores the raw lines and parses:

- satellite name
- NORAD catalog ID
- classification
- epoch
- inclination
- RAAN
- eccentricity
- argument of perigee
- mean anomaly
- mean motion

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
3. The scene loads the configured TLE file.
4. `SatelliteManager` filters likely LEO satellites when `showOnlyLeoSatellites` is enabled.
5. The current demo configuration spawns one bright point-mass satellite marker and disables orbit paths.
6. Hover over the satellite marker with the visible mouse crosshair to view its TLE data in the corner panel.

To show more satellites later, raise or clear `SatelliteManager.maxSatellitesForTesting`. Keep `orbitLinesEnabled` off for a point-mass-only view.

## Desktop Controls

Desktop navigation uses `Assets/Scripts/CameraControls.cs` with Unity's Input System:

- Move the mouse to move the visible crosshair around the screen.
- Point the crosshair at a satellite marker to hover it and show TLE data.
- Mouse: look around while cursor is locked.
- `W`, `A`, `S`, `D`: move camera.
- `Q`, `E`: move down/up.
- `Left Shift`: movement boost.
- `Escape`: toggle cursor lock.

Simulation time controls are handled by `SatelliteManager`:

- `Space`: pause or resume simulation time.
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
2. Each marker receives a `SatelliteInfo` component containing its parsed `SatelliteTleData`.
3. `SatelliteHoverController` raycasts against satellite markers.
4. The hovered marker is highlighted.
5. Hover changes are emitted through a UnityEvent and a static C# event.
6. `SatelliteTleInfoPanel` listens for hover changes.
7. When no satellite is hovered, the panel shows:

```text
Hover over a satellite to view TLE data.
```

8. When a satellite is hovered, the panel displays name, NORAD ID, TLE lines, inclination, RAAN, eccentricity, argument of perigee, mean anomaly, mean motion, and epoch.

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
- `maxSatellitesForTesting`: limits marker count for performance testing. The current demo uses `1`.
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
- The included TLE file is a small sample dataset, not a complete live catalog.
- There is no live network TLE download/update system.
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
