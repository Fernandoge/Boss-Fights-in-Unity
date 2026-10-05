# First boss: Boulder Roll

**Behaviour:** the boss stomps (copy of the Stomp clip, event at the impact). Big lanes of scrolling arrows appear one after another, 0.7 s apart; their order is the order the boulders spawn in. After the last arrow plus a short delay, the boulders spawn in that same order at the arena walls and roll slowly across the map. Five boulders roll in parallel lanes. A boulder is about 4x the old rock (6.4 m wide, lane as wide as the boulder). Boulders are on the Ignore Raycast layer, so the player's mouse rays (movement, aiming, dash) pass through them and hit the ground. It deals damage and breaks (rock chunks and dust) when it hits the player, any level wall or rock (NavMeshObstacle), or another boulder, and it also breaks when it leaves the far side of the arena.

**Lane planning (`BoulderLanePlanner`):** each cast picks horizontal or vertical lanes as the player sees them (the camera is at 45 degrees, so these are diagonal in world space) and spreads the boulders' lanes in parallel across the map. Lanes are placed randomly over the widest stretch of the map where a lane is at least 30 m long, with a guaranteed gap between lane edges (`_boulderMinLaneGap`, 4 m; it shrinks to a 2 m floor, then uses fewer lanes, if the map is too narrow). Each lane gets a random travel direction and the order the arrows appear in (and the boulders spawn in) is shuffled. Because the lanes are parallel and separated, arrows never overlap and boulders can never meet or break each other. The lanes do not aim at the player; with 5 of them the player has to focus on dodging. The walls are the real ones: the planner scans the NavMesh along each lane to find where walkable ground begins and ends, and the boulder spawns one radius inside the wall there and rolls to the far wall. Only ground-level walkable terrain counts. Tests (300 plans): always 5 lanes, 0 overlaps, edge gap at least 4.0 m (average 5.5 m), horizontal and vertical sets about equally likely, every spawn sphere touching a wall, a plan takes at most a few milliseconds.

**Phase 2:** the same 5 boulders (`_boulderPhase2ExtraCount` is 0; raise it for more).

**Pieces**
- `Shared/SkillIndicator.cs` + `Shaders/SkillIndicator.shader`: `ShowArrowLane` draws scrolling chevrons along a lane and stays until `Hide()`.
- `BoulderLanePlanner.cs`, `BoulderRollSequence.cs` (shows the arrows and launches the boulders on a timer, then removes itself), `BoulderProjectile.cs` (moves, rolls, damages, breaks).
- `FirstBoss.cs`: `[Header("Boulder Roll")]` fields, `StartBoulderRoll()`, animation event `BoulderRollStart`. Attack `BoulderRoll` in `FirstBossAttack` (index 7 in the random pool).
- Animator: trigger `BoulderRoll`, state `Boulder Roll` using `Boulder Stomp.anim` (copy of `Stomp.anim` with only the `BoulderRollStart` event at 0.72 s).
- Prefabs in `Prefabs/Characters/Bosses/`: `Boulder`, `BoulderBreak` (uses `FissureDust`); the arrows reuse the `FissureIndicator` prefab. Values live on the `First Boss` prefab.

**Starting values (tune in playtest):** damage 1, radius 3.2 (6.4 m wide), speed 9, arrows 0.7 s apart, launch delay 1.0 s after the last arrow, 0.9 s between launches, 5 boulders (+0 in phase 2), minimum lane gap 4 m.

**Testing:** on the `DebugHarness` object in `TestScene`, tick `Only Use One Attack` and pick `BoulderRoll` (or press F5 in Play mode) to make the boss use only this skill; keep it off in commits.

**Known limits:** boulders roll straight through the boss (it is not an obstacle), so a lane aimed at the player can pass over the boss. No sound. The break chunks are plain cubes and the boulder is a smooth sphere with the existing stone material. The boss can start its next attack while boulders are still rolling.
