# void!infinity Changelog

## v0.1.0 — The (mostly) Slider Update

Introduces `.vib` for beatmaps and `.vibz` for beatmap archives to support new editor features incompatible with `.osu`.

### Sliders

- Slider control points can now have different velocities. For example, use `1 → 0.5` to slow down toward the end, or `0.5 → 1` to speed up. Right-click a control point and select **Change velocity**. Multiple velocity changes are handled correctly.
- Slider control points can now move during gameplay. Right-click a control point and select **Add movement**, move the point yourself, then press **Accept**. The movement menu is barebones for now.
- Movement is clamped to a radius by default to prevent too much chaos. A dotted circle shows the limit while moving the point. This restriction can be toggled in the editor's **Setup** tab.
- Converting a slider into a stream removes all of that slider's in-map movement. A confirmation popup warns you before doing so.

### Other editor features

- Added **Event** objects, which are invisible during gameplay. Change an Event's type in the **Inspector**.
- Events can change beatmap attributes on the fly, covering the settings in the **Setup** tab that make sense to change during gameplay. This is currently the only Event type.

## v0.0.0

- Initial version of void!infinity. No changes have been made beyond offline-first support and rebranding.
