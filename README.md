# OrbitSim

OrbitSim is a Unity VR/desktop simulation for visualizing LEO satellites around a 3D Earth globe in space. It loads local TLE data, propagates satellite positions, renders orbit markers and optional orbit lines, and shows detailed TLE information when the user hovers over a satellite.

## Project Basics

- Unity version: `6000.3.7f1`
- Main scene: `Assets/Scenes/SampleScene.unity`
- Sample TLE data: `Assets/StreamingAssets/TLE/leo-sample.tle`
- Earth texture/material: `Assets/Textures/EarthTexture1.jpg`, `Assets/Textures/Earth_Mat.mat`
- SGP4 library: `Assets/Packages/SGP.NET.1.4.0/lib/netstandard2.0/SGP.NET.dll`

The project currently supports desktop testing with mouse hover and keyboard time controls. The scene also includes an `XR` hierarchy root and VR-aware UI behavior, but full OpenXR/XR Interaction Toolkit package setup is still a future integration step.

## Scene Structure

`SampleScene.unity` is organized under a clear `SceneRoot` hierarchy:

- `Earth`: Earth transform and globe mesh.
- `Satellites`: parent for spawned satellite marker objects.
- `OrbitVisuals`: parent for orbit line renderers.
- `XR`: player rig/camera organization for desktop and future VR setup.
- `UI`: runtime TLE information panel placeholder.
- `Managers`: simulation manager objects and startup helpers.

`SimulationManager` hosts the core runtime components:

- `TleLoader`
- `SatelliteManager`
- `SatelliteHoverController`

## Core Systems

### TLE Loading

`Assets/Scripts/SatelliteTleData.cs` stores raw and parsed TLE fields:

- satellite name
- line 1 and line 2
- NORAD catalog ID
- classification
- epoch
- inclination
- RAAN
- eccentricity
- argument of perigee
- mean anomaly
- mean motion

`Assets/Scripts/TleLoader.cs` loads standard 3-line TLE records from StreamingAssets:

```text
satellite name
line 1
line 2
```

Malformed entries are skipped or warned about so the simulation can keep running with valid records.

### Orbit Propagation

`Assets/Scripts/SatelliteManager.cs` uses SGP.NET when possible to predict satellite ECI positions from each TLE. If SGP4 setup or prediction fails for an entry, it falls back to a lightweight Keplerian visualization based on parsed TLE orbital elements.

The fallback is approximate. It ignores drag, J2/precession, decay, and other perturbation effects handled by full SGP4. It is intended for visual continuity, not authoritative orbital analysis.

### Satellite Rendering

`SatelliteManager` loads TLE entries, optionally filters them to likely LEO satellites, and spawns one lightweight marker per satellite under `Satellites`. Each marker receives `Assets/Scripts/SatelliteInfo.cs`, which stores the satellite's parsed TLE data for hover/UI use.

Optional orbit lines are generated under `OrbitVisuals`. To protect performance with large catalogs, orbit lines are capped by `maxOrbitLines` and marker updates are centralized in one manager instead of per-object update loops.

### Hover Interaction

`Assets/Scripts/SatelliteHoverController.cs` detects hovered satellites.

- Desktop mode uses mouse raycasts from the main camera.
- VR mode can use configured ray origin transforms when XR controller rays are available.
- Hovered markers are highlighted visually.
- Hover changes are published through a UnityEvent and a static C# event.

### TLE UI

`Assets/Scripts/SatelliteTleInfoPanel.cs` builds a readable runtime UI panel.

- Desktop mode uses a screen-space corner panel.
- VR mode can switch to a world-space HUD attached in front of the player camera.
- When nothing is hovered, it shows: `Hover over a satellite to view TLE data.`
- When a satellite is hovered, it displays the satellite name, NORAD ID, raw TLE lines, inclination, RAAN, eccentricity, argument of perigee, mean anomaly, mean motion, and epoch.

## Scale Model

The scene uses a readable visualization scale, not a physically exact distance scale.

- `SatelliteManager.earthRadiusWorldUnits` maps Earth's real radius, 6371 km, to the globe radius in Unity world units.
- `SatelliteManager.orbitAltitudeExaggeration` multiplies only the altitude above Earth, so LEO satellites are visible above the globe.
- Satellite directions come from SGP4 when available, with the Keplerian fallback used for orbit-line previews and propagation failures. This keeps satellites distributed around the globe instead of clustering them in an artificial display band.
- Set `orbitAltitudeExaggeration` to `1` for a physically proportional altitude scale.

## LEO Filtering

`SatelliteManager.showOnlyLeoSatellites` limits spawned markers to satellites that can be classified as likely LEO from parsed TLE fields. The classifier uses:

- mean motion
- orbital period
- approximate altitude derived from mean motion

Entries without enough parsed orbital data are excluded when this filter is enabled. Disable `showOnlyLeoSatellites` to show all valid TLE entries loaded by `TleLoader`.

## Time Controls

Desktop testing supports simple keyboard time controls through Unity's Input System:

- `Space`: pause or resume simulation time.
- `=` or numpad `+`: multiply simulation speed by `speedStepMultiplier`.
- `-` or numpad `-`: divide simulation speed by `speedStepMultiplier`.

The same controls are exposed as public methods on `SatelliteManager` for future VR UI buttons or controller bindings:

- `PlaySimulation()`
- `PauseSimulation()`
- `ToggleSimulationPaused()`
- `SetSimulationSpeedMultiplier(float multiplier)`
- `MultiplySimulationSpeed(float multiplier)`

## Inspector Settings

Important `SatelliteManager` fields:

- `satelliteMarkerPrefab`: optional custom marker prefab; falls back to a primitive sphere.
- `earthReference`: Earth center transform.
- `earthVisual`: visual globe transform to scale from `earthRadiusWorldUnits`.
- `earthRadiusWorldUnits`: Unity radius of Earth.
- `orbitAltitudeExaggeration`: visual multiplier for altitude above Earth.
- `timeScale`: base simulation seconds per real second.
- `simulationSpeedMultiplier`: runtime speed multiplier.
- `simulationPaused`: pauses simulation time when enabled.
- `satellitePositionUpdateInterval`: seconds between satellite position updates. Raising this lowers CPU cost for large catalogs at the cost of less frequent motion updates.
- `maxSatellitePositionUpdatesPerFrame`: optional cap for spreading propagation work across frames. Set to `0` to update all satellites together.
- `showOnlyLeoSatellites`: toggles LEO filtering.
- `maxSatellitesForTesting`: limits spawned satellites for testing when greater than zero.
- `orbitLinesEnabled`: enables/disables orbit line creation.
- `maxOrbitLines`: caps orbit line count for performance.
- `shareMarkerMaterial`: assigns a shared instancing-enabled material to markers to avoid per-marker material instances.

## Performance Notes

Satellite markers are spawned once and updated through `SatelliteManager` instead of through one update loop per marker. Runtime marker transforms are cached when markers are created, so the position hot path does not call `GetComponent`.

For large catalogs:

- Use `maxSatellitesForTesting` while tuning.
- Keep `orbitLinesEnabled` off or reduce `maxOrbitLines`.
- Increase `satellitePositionUpdateInterval` to reduce propagation frequency.
- Set `maxSatellitePositionUpdatesPerFrame` to spread satellite updates across multiple frames.
- Keep `shareMarkerMaterial` enabled so primitive markers can use a shared instancing-capable material.

## Running The Project

1. Open the repository in Unity `6000.3.7f1`.
2. Open `Assets/Scenes/SampleScene.unity`.
3. Enter Play Mode.
4. Use the desktop fly camera controls to navigate the globe.
5. Hover over a satellite marker to view TLE data in the corner panel.
6. Use the time controls to pause or change simulation speed.

## Validation Notes

The project has been validated with:

- `dotnet build EarthSatOrbitSim.sln`
- sample TLE data classification checks
- scene scans for missing script references

The current local `dotnet build` succeeds with 0 errors. Unity/.NET assembly conflict warnings for `System.Net.Http` and `System.Security.Cryptography.Algorithms` are present from package/editor references and do not currently block compilation.

## Current Limitations

- Full production XR setup is not complete. OpenXR, XR Plugin Management, and XR Interaction Toolkit still need to be installed/configured for target hardware.
- The Keplerian fallback and orbit-line previews are visual approximations.
- Large catalogs may need batching, object pooling, update throttling, and selective orbit rendering before VR-scale performance is acceptable.
- The current sample dataset is intentionally small and stored locally for testing.
