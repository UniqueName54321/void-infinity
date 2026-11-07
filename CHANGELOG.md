# void!infinity Changelog

## v0.1.2.4

- Updated local star calculations to count every multitap note in strain, combo, and difficulty attributes. Standard now samples moving and variable-velocity sliders over time, including tracking effort for first/standalone native sliders. Bumped all four calculator versions so cached estimates are refreshed.
- Updated mod interactions for the new objects: Relax taps every standard multitap note; autoplay follows groups in all modes; Hidden/Fade In adjust child notes without prematurely hiding entire groups, and multitap counters do not reveal hidden notes. Mirror/Hard Rock propagate to group notes, and taiko Swap/Random/Simplified Rhythm handle the individual hits.
- Added **No Multis** to every mode's conversion mods. It replaces multitap groups with ordinary streams while preserving timing, hitsounds, columns, positions, and taiko hit types. This affects the playable map without rewriting the saved beatmap.
- Added native multitap support to **taiko**, **catch**, and **mania**, including editor configuration, saving/loading, separate tap judgements, autoplay, and undo/redo. Catch groups produce repeated fruits; mania groups stay in their authored column; taiko groups retain rim/centre and strong-hit information.

## v0.1.2.3

- Fixed older local `.vib` beatmaps failing to load and appearing to contain 0 hitobjects after the multitap update. Missing or null optional extension collections now retain empty defaults, preserving the map's existing hitobjects, slider extensions, and events. Existing files do not need to be rewritten or reimported.

## v0.1.2.2

- Made **Human Play** less mechanical: seeded cursor arcs, overshoot/corrections, idle hand drift, varied aim and tapping, imperfect slider tracking and spinner motion in standard; irregular timing across all modes, with additional mania finger/release variation, taiko hand/rhythm variation, and catch aim corrections. The mandatory autoplay badge remains visible.
- Added **multitap circles** in standard. Right-click a circle and choose **Configure multitap** to set the tap count, time signature, and taps per beat. Tap timing follows the map's BPM, including tempo changes; each tap is judged separately. Set the count to 1 to restore a normal circle. Saved in `.vib`/`.vibz`.
- Added **Convert stacked stream to multitap** for selected circles at the same position on a supported, uniform beat subdivision. Conversion preserves tap timing and individual hitsounds, supports undo/redo, and refuses selections that would lose timing, positions, or combo boundaries.

## v0.1.2.1

- Moved the mandatory Human Play autoplay marker to the bottom-left corner and condensed it into a compact horizontal badge. It retains 95% opacity and stays visible when the HUD is hidden.

## v0.1.2

- Added **Human Play** to every game mode's autoplay options. Seeded timing variation and mode-specific movement/input imperfections make autoplay more human-like, at the cost of some accuracy. A mandatory, unskinnable autoplay icon and **HUMAN PLAY** label remain in the centre of gameplay at 95% opacity, including when the HUD is hidden.
- Expanded osu!lazer library import with separate **Beatmaps**, **Skins**, and **Options** toggles. Import only the content you need; supported local preferences can be copied without importing beatmaps. Accounts, credentials, online settings, skin selection, and experimental switches are excluded from options import.
- Main-menu skin backgrounds no longer require an osu!supporter tag. Background selection works locally without supporter checks.
- Added **Experimental Features** to settings. Enabling **Enable Experimental Features** requires accepting a clear warning; individual feature switches only appear while it is enabled and need no additional confirmation. The first feature, **Test**, is a placeholder that does nothing.

## v0.1.1

- Centred the **E** in the Event tool's icon.
- Added much more autoplay customisation, with shared timing-offset and random-seed settings plus options specific to each game mode:
  - **Standard:** natural, linear, or cursor-dance movement; dance strength; tapping style; spinner speed and direction; reaction time; and delayed movement.
  - **Taiko:** alternating, preferred, or randomised hands; swell hit interval; and extra drumroll hits.
  - **Mania:** tap duration, seeded chord spread, and hold-release offset.
  - **Catch:** catcher aim offset or seeded aim variation, early movement, and always-dash behaviour.
- Added **Import osu!lazer library** to **Settings → Maintenance** and the first-run setup screen. Choose a lazer data folder to copy its beatmaps and assets into void!infinity, with progress and cancellation. Existing sets are deduplicated, deleted sets are skipped, and the source library is left unchanged. Close osu!lazer before importing.

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
