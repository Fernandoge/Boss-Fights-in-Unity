# First boss: Fissure Lines

Added as the pilot for the AI-assisted workflow (see CLAUDE.md "Direction").

**Behaviour:** the boss turns to face the player and stomps (copy of the Rock Shower stomp clip). Straight lines fan out along the ground, each starting `_fissureStartDistance` (3 m) in front of the boss, each shown by a `SkillIndicator` whose fill sweeps outward over the telegraph time. On the stomp impact the lines erupt into spikes and hurt anyone standing inside a line once. Safe spots are the gaps between lines, so the player must move during the telegraph (the centre line starts on the player).

**Phase 2:** 5 lines instead of 3, a tighter angle between them and a shorter telegraph.

**Pieces**
- `Shared/SkillIndicator.cs` + `Shaders/SkillIndicator.shader`: reusable procedural line indicator (shapes other than a line are not built yet).
- `Bosses/First Boss/FissureLine.cs`: spikes and damage check for one line. Damage is a position check against the line (no trigger physics).
- `FirstBoss.cs`: `[Header("Fissure Lines")]` tuning fields, `StartFissureLines()`, and the animation-event methods `FissureLinesTelegraph` / `FissureLinesErupt`. Attack index 6 in the random pool.
- Animator: trigger `FissureLines`, state `Fissure Lines` using `Fissure Stomp.anim` (a copy of `Stomp.anim` with only these two events at 0.08 s and 0.72 s). The animation is slowed so the stomp lands when the telegraph ends (`FissureImpactAnimationTime` in `FirstBoss.cs` must match the `FissureLinesErupt` event time).
- Prefabs in `Prefabs/Characters/Bosses/`: `FissureIndicator`, `FissureLine`, `FissureDust`. Values live on the `First Boss` prefab. `_debugOnlyUseFissureLines` on that prefab makes the boss use only this skill (keep it off in commits).

**Starting values (tune in playtest):** damage 1, start distance 3, length 24, width 2.0, 3 lines at 36 degrees, 1.2 s telegraph; phase 2 adds 2 lines at 26 degrees, 1.0 s telegraph.

**Known limits:** lines still overlap slightly close to their start, so the safe gaps open up a few meters out. No second wave yet. Spikes are plain pyramids with the arena rock material. No sound.
