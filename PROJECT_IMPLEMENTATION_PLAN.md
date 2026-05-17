# Project Implementation Plan

## Current Project State

- Unity version: `6000.3.7f1`
- Repository root: `D:\Unity\EarthSatOrbitSim`
- Git branch: `main`
- Main scene: `Assets/Scenes/SampleScene.unity`
- Current build settings scene list is empty in `ProjectSettings/EditorBuildSettings.asset`.
- Current XR setup is not production-ready: built-in XR/VR modules exist, but OpenXR, XR Plugin Management, and XR Interaction Toolkit are not installed or configured.

## Existing Assets

- `Assets/Textures/EarthTexture1.jpg`: Earth texture.
- `Assets/Textures/Earth_Mat.mat`: Earth material using the Earth texture.
- `Assets/Scenes/SampleScene.unity`: Contains a camera, directional light, Earth sphere, desktop fly camera setup, and a transform-only `SgpNetSmokeTestRunner`.
- `Assets/InputSystem_Actions.inputactions`: Generic Unity input action asset with keyboard/gamepad/touch/joystick/XR bindings.
- `Assets/Packages/SGP.NET.1.4.0/lib/netstandard2.0/SGP.NET.dll`: SGP.NET orbital propagation library installed through NuGetForUnity.

## Existing Scripts

- `Assets/Scripts/CameraControls.cs`
  - Desktop fly camera using Unity Input System.
  - Supports mouse look, WASD movement, Q/E vertical movement, shift boost, and cursor lock toggle.
  - Useful for editor/debug navigation, not final VR locomotion.

- `Assets/Scripts/SgpNetSmokeTest.cs`
  - Smoke test for SGP.NET.
  - Creates one ISS satellite from hardcoded TLE lines.
  - Observes it from a fixed ground station at a fixed UTC time.
  - Does not instantiate or position Unity satellite objects.

- `Assets/Scripts/TestScript.cs`
  - Minimal debug script that logs a message.

## Missing Capabilities

### Earth Globe In Space

- Create a stable Earth prefab using the existing material and texture.
- Define a world scale convention for Earth radius, LEO altitude, and orbit distances.
- Add space background, starfield/skybox, lighting, and camera clipping appropriate for VR.
- Add `SampleScene.unity` to build settings.

### LEO Satellite Visualization

- Create satellite marker prefab or batched marker renderer.
- Add satellite manager to spawn and update satellite visuals.
- Add orbit trail/line rendering, initially for selected or hovered satellites.
- Add hover/selection highlight state.

### TLE Data Loading

- Replace hardcoded smoke-test TLE with a reusable loader.
- Add local TLE catalog as a `TextAsset` or structured data asset.
- Parse satellite name, TLE line 1, and TLE line 2 into runtime satellite records.
- Preserve raw TLE text for UI display.

### Orbital Propagation And Positioning

- Wrap SGP.NET in a simulation service.
- Add simulation clock, time scale, pause/resume, and fixed update cadence.
- Convert SGP.NET output into Unity world coordinates around the Earth globe.
- Validate ISS/LEO altitude visually against the chosen scale.

### Hover Interaction

- Desktop mode: raycast from camera/mouse.
- VR mode: use XR ray interactor, gaze, or controller pointer.
- Add colliders or spatial picking for satellite markers.
- Emit hover enter/exit events to update the UI.

### Corner TLE UI

- Add a corner panel for desktop and an equivalent VR-readable world-space panel.
- Show satellite name, TLE line 1, TLE line 2, and optional live position fields.
- Update the panel only when the hover target changes.

### Performance For Many Satellites

- Avoid one heavy update loop per satellite.
- Use a central propagation scheduler.
- Update propagation at a fixed cadence rather than every rendered frame.
- Pool marker objects.
- Use GPU instancing or batched meshes for large satellite counts.
- Render orbit paths selectively or cache sampled paths.
- Add frustum/distance culling and LOD rules.

## Implementation Phases

### Phase 1: Project And XR Foundation

- Add `SampleScene.unity` to build settings.
- Install/configure XR Plugin Management and OpenXR.
- Install/configure XR Interaction Toolkit if controller hover/interaction is required.
- Keep the desktop fly camera as an editor/debug fallback.
- Confirm the project compiles after package changes.

### Phase 2: Scene Architecture

- Establish scene roots: `SimulationManager`, `EarthRoot`, `SatelliteRoot`, `OrbitRoot`, `UIRoot`, and `XRRoot`.
- Convert the Earth sphere/material setup into a reusable prefab.
- Add space background and VR-appropriate lighting.
- Define scene scale constants.

### Phase 3: TLE Data Model And Loading

- Add satellite data model for name, raw TLE lines, parsed SGP.NET satellite, and current state.
- Add a local sample TLE catalog.
- Implement catalog loading and validation.
- Report malformed TLE entries clearly.

### Phase 4: Propagation And Coordinate Mapping

- Implement a propagation service using SGP.NET.
- Implement simulation time controls.
- Convert propagated coordinates to Unity positions.
- Validate one known satellite first, then multiple satellites.

### Phase 5: Satellite And Orbit Rendering

- Create satellite marker visual.
- Spawn/update markers from loaded TLE data.
- Render selected or hovered satellite orbit path.
- Add visual hover/selection feedback.

### Phase 6: Hover And TLE UI

- Implement desktop hover raycast.
- Implement VR hover through XR interaction.
- Add corner TLE panel and bind it to hover state.
- Ensure UI is readable in both desktop preview and VR.

### Phase 7: Scaling And Performance

- Pool satellite markers.
- Add batched or instanced rendering path.
- Add propagation scheduling for large catalogs.
- Add selective orbit rendering and cached orbit samples.
- Profile frame time in the target VR runtime.

### Phase 8: Validation And Build

- Add play mode tests for TLE parsing and coordinate conversion.
- Verify no missing scene/script references.
- Verify OpenXR launch.
- Build the target platform and profile the simulation with many satellites.
