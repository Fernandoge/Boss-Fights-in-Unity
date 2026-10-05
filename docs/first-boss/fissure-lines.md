# First boss: Fissure Lines

Added as the pilot for the AI-assisted workflow (see CLAUDE.md "Direction").

**Behaviour:** the boss turns to face the player and stomps (copy of the Rock Shower stomp clip). Straight lines fan out along the ground, each starting `_fissureStartDistance` (3 m) in front of the boss, each shown by a `SkillIndicator` whose fill sweeps outward over the telegraph time. When the fill is complete the indicators stay fully filled for a short hold (`_fissureHoldAfterFillTime`, 0.3 s) so the player gets a clear beat, and then the lines erupt into spikes (the stomp impact) and hurt anyone standing inside a line once. Safe spots are the gaps between lines, so the player must move during the telegraph (the centre line starts on the player).

**Phase 2:** 5 lines instead of 3, a tighter angle between them and a shorter telegraph.

**Pieces**
- `Shared/SkillIndicator.cs` + `Shaders/SkillIndicator.shader`: reusable procedural line indicator (shapes other than a line are not built yet).
- `Bosses/First Boss/FissureLine.cs`: spikes and damage check for one line. Damage is a position check against the line (no trigger physics).
- `FirstBoss.cs`: `[Header("Fissure Lines")]` tuning fields, `StartFissureLines()`, and the animation-event methods `FissureLinesTelegraph` / `FissureLinesErupt`. In the random attack pool.
- Animator: trigger `FissureLines`, state `Fissure Lines` using `Fissure Stomp.anim` (a copy of `Stomp.anim` with only these two events at 0.08 s and 0.72 s). The animation speed is set so the time between the two events equals telegraph + hold (`FissureTelegraphAnimationTime` and `FissureImpactAnimationTime` in `FirstBoss.cs` must match the event times in the clip, 0.08 s and 0.72 s). The indicators use `ShowLine(..., hideWhenFilled: false)`, so they stay until the eruption removes them.
- Prefabs in `Prefabs/Characters/Bosses/`: `FissureIndicator`, `FissureLine`, `FissureDust`. Values live on the `First Boss` prefab. To see the skill on its own, force it with the debug harness (key 6, or `ForceAttack(FirstBossAttack.FissureLines)`) and turn boss auto attacks off with F1.

**Starting values (tune in playtest):** damage 1, start distance 3, length 24, width 2.0, 3 lines at 36 degrees, 1.0 s telegraph + 0.3 s hold; phase 2 adds 2 lines at 26 degrees, 0.8 s telegraph + 0.3 s hold. The telegraph and phase 2 telegraph are set on the boss in `TestScene`; the hold is on the prefab.

**Known limits:** lines still overlap slightly close to their start, so the safe gaps open up a few meters out. No second wave yet. Spikes are plain pyramids with the arena rock material. No sound.
