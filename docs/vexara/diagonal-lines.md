# Vexara: Diagonal Lines

The boss's second skill. It casts with both hands, then a field of thin lines is telegraphed across the whole arena at once, and every line explodes at the same moment. Standing on a line when it explodes costs 1 heart; standing in a gap is safe.

**Pattern (new random one each cast):** 2 or 3 layers of parallel lines. Every layer has its own random angle, anywhere except near horizontal or vertical (at least 30 degrees from horizontal and 15 from vertical in world space; world X is screen-horizontal and the camera flattens Z, so horizontal needs the bigger margin). One layer in three crosses the previous one at exactly 90 degrees. Spacing is 2.4 to 3.4 m (later layers are 35% sparser each), width 0.55 to 0.95 m, and 10% of the lines (more in later layers) are dropped so the gaps are uneven. All layers overlap, which is where the chaos comes from. On average about 40% of the floor is safe (25% to 60%), measured over many random patterns.

**Timing:** (all 20% faster than the first version: wind-up 0.45, telegraph 1.4, teleport delay 0.3, end 0.2 s before) 0.375 s wind-up (two-handed cast), then the lines appear and fill over 1.17 s (red/orange telegraph, same indicator as the other skills). The boss teleports 0.25 s after casting, while the lines charge. The explosion is a bright white-violet flash of every line for about 0.4 s. The boss's next attack waits until 0.17 s after the explosion. The boss never casts the same skill twice in a row (like Gorath); with only two skills they strictly alternate.

**Pieces**
- `Bosses/Vexara/DiagonalLineBlast.cs` (prefab `DiagonalLineBlast`): builds the layers, clips each line to the arena, owns its timers and damage (a position check against the nearest line of each layer, with 0.25 m padding). `LineBlastSettings` (spacing, width, drop chance, angle limits, layer count, telegraph time, damage) is serialized on the boss as `Line Blast`.
- `VexaraBoss.cs`: `DiagonalLinesSequence`, `ChooseAttack` and `VexaraAttack`. The arena rectangle comes from the baked NavMesh bounds widened by the agent radius, so it follows the level if the floor changes.
- Prefabs `DiagonalLineIndicator` (telegraph, a copy of the line indicator) and `LineBlastFlash` (explosion flash, material `LineBlastFlash`).
- Animator: base-layer state `Cast Area` (`Standing 2H Magic Area Attack 01`, speed 1.4) reached by trigger `CastArea`.
- Harness with Vexara: key 3 forces the skill; F5 makes the boss use only one attack (`Vexara Attack To Use` on the `DebugHarness` object, default Diagonal Lines).

**Tuning:** everything is under `Line Blast` and `Diagonal Lines` on the Vexara prefab. For more safe space raise `Spacing Range` or `Drop Chance`; for a faster pace lower `Telegraph Time`.

**Known limits:** the explosion is a flat flash with no particles yet; the cast animation is a placeholder and its release is not synced to the line spawn; lines are not clipped by the wall rims (they stop at the floor edge).
