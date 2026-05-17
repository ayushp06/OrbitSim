# EarthSatOrbitSim

Unity VR/desktop simulation for visualizing LEO satellites around a 3D Earth globe from local TLE data.

## Scale Model

The scene uses a readable visualization scale, not a physically exact distance scale.

- `SatelliteManager.earthRadiusWorldUnits` maps Earth's real radius, 6371 km, to the globe radius in Unity world units.
- `SatelliteManager.orbitAltitudeExaggeration` multiplies only the altitude above Earth, so LEO satellites are visible above the globe.
- Satellite directions come from SGP4 when available, with a Keplerian fallback for orbit-line previews and propagation failures. This keeps satellites distributed around the globe while making low-altitude orbits easier to inspect.
- Set `orbitAltitudeExaggeration` to `1` for a physically proportional altitude scale.

## LEO Filtering

`SatelliteManager.showOnlyLeoSatellites` limits spawned markers to satellites that can be classified as likely LEO from parsed TLE fields. The classifier uses mean motion, orbital period, and approximate altitude from mean motion. Entries without enough parsed orbital data are excluded when this filter is enabled.

Disable `showOnlyLeoSatellites` to show all valid TLE entries loaded by `TleLoader`.

## Time Controls

Desktop testing supports simple keyboard time controls through Unity's Input System:

- `Space`: pause or resume simulation time.
- `=` or numpad `+`: multiply simulation speed by `speedStepMultiplier`.
- `-` or numpad `-`: divide simulation speed by `speedStepMultiplier`.

The same controls are exposed as public methods on `SatelliteManager` for future VR UI buttons or controller bindings.
