# First boss: Boulder Roll

**Behaviour:** the boss stomps (copy of the Stomp clip, event at the impact). Big lanes of scrolling arrows appear one after another, 0.7 s apart; their order is the order the boulders spawn in. After the last arrow plus a short delay, the boulders spawn in that same order at the arena walls and roll slowly across the map. A boulder is about 4x the old rock (6.4 m wide, lane as wide as the boulder). Boulders are on the Ignore Raycast layer, so the player's mouse rays (movement, aiming, dash) pass through them and hit the ground. It deals damage and breaks (rock chunks and dust) when it hits the player, any level wall or rock (NavMeshObstacle), or another boulder, and it also breaks when it leaves the far side of the arena.

**Lane planning (`BoulderLanePlanner`):** lanes run along the screen axes only (horizontal or vertical as the player sees them; the camera is at 45 degrees, so these are diagonal in world space). Each boulder picks one of the four directions and a lane aimed near the player (`_boulderAimSpread`, widened automatically if the player is cornered). The walls are the real ones: the planner scans the NavMesh along the lane to find where walkable ground begins and ends, and the boulder spawns one radius inside the wall at that point and rolls to the far wall. Lanes must cross at least 25 m of walkable ground and ignore raised patches on top of the walls. The planner simulates the boulders' paths and timing and only accepts a layout where no two boulders ever get closer than touching, so they never break each other. If that ever fails it falls back to parallel lanes from one wall, which cannot cross. Tests with the player anywhere on the map gave 0 collisions, 0 fallbacks and always the full number of boulders (3, or 5 in phase 2); every planned spawn sphere touched a wall and every lane ended at a wall; a plan takes at most a few milliseconds.

**Phase 2:** 2 extra boulders (5 instead of 3).

**Pieces**
- `Shared/SkillIndicator.cs` + `Shaders/SkillIndicator.shader`: `ShowArrowLane` draws scrolling chevrons along a lane and stays until `Hide()`.
- `BoulderLanePlanner.cs`, `BoulderRollSequence.cs` (shows the arrows and launches the boulders on a timer, then removes itself), `BoulderProjectile.cs` (moves, rolls, damages, breaks).
- `FirstBoss.cs`: `[Header("Boulder Roll")]` fields, `StartBoulderRoll()`, animation event `BoulderRollStart`. Attack `BoulderRoll` in `FirstBossAttack` (index 7 in the random pool).
- Animator: trigger `BoulderRoll`, state `Boulder Roll` using `Boulder Stomp.anim` (copy of `Stomp.anim` with only the `BoulderRollStart` event at 0.72 s).
- Prefabs in `Prefabs/Characters/Bosses/`: `Boulder`, `BoulderBreak` (uses `FissureDust`); the arrows reuse the `FissureIndicator` prefab. Values live on the `First Boss` prefab.

**Starting values (tune in playtest):** damage 1, radius 3.2 (6.4 m wide), speed 9, arrows 0.7 s apart, launch delay 1.0 s after the last arrow, 0.9 s between launches, aim spread 6 m, 3 boulders (+2 in phase 2).

**Testing:** on the `DebugHarness` object in `TestScene`, tick `Only Use One Attack` and pick `BoulderRoll` (or press F5 in Play mode) to make the boss use only this skill; keep it off in commits.

**Known limits:** boulders roll straight through the boss (it is not an obstacle), so a lane aimed at the player can pass over the boss. No sound. The break chunks are plain cubes and the boulder is a smooth sphere with the existing stone material. The boss can start its next attack while boulders are still rolling.
