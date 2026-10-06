# Second boss level (`SecondBossScene`)

A single-screen arena: the camera never moves and the screen borders are the walls. Outside the walls is a void with stars.

**Camera:** the same isometric look as the first level (pitch 30 degrees, orthographic) but fixed, with no yaw (the arena is aligned to the camera, so it fills the screen as a rectangle). `Cameras/FixedArenaCamera` on the Main Camera sizes the view from `Visible Width` (35.56 = orthographic size 10, the first level uses 12) and keeps the view exactly 16:9: when the window is not 16:9 the camera rect is letterboxed and a second camera paints the bars black. The HUD canvas follows the camera rect. The `Camera Pivot` rotation (30, 0, 0) holds the pitch. The old `CameraController` (follow the player) was removed from this scene.

**Fitting the arena to the frame:** with an orthographic camera at pitch P, the screen is `Visible Width` wide by `Visible Width * 9/16` tall; ground distances along z are stretched by `1/sin(P)`. The walls sit on the screen edges (left, right and bottom), and the north wall is pulled in by 2.5 screen units so a strip of void shows above it. If you change the pitch or zoom, the floor and walls have to be resized to match; the numbers used were `zSouth = -orthoSize / sin(P)` and `zNorth = (orthoSize - voidMargin - wallHeight * cos(P)) / sin(P)` (here -20 and 13.27).

**Pieces** (all under `Second Boss Level`):
- `Floor`: a cube with the `Arena/ArcaneFloor` shader (`Shaders/ArcaneFloor.shader`, material `Materials/Arena/ArcaneFloor`): dark base, drifting nebula, twinkling stars, faint tile grid and a glowing border. It also holds the `NavMeshSurface` (collects only itself); the baked data is `Scenes/SecondBossScene/NavMesh-Floor.asset`. Rebake with the surface's Bake button after moving the floor.
- A rune circle (rings, spinning dashed ring, hexagram) is built into the shader but switched off (`Rune Strength` 0 on the material) so it can be used later for skills (`Rune Center` and `Rune Radius` place it).
- `Walls`: four boxes (1 unit high, 1.3 thick, layer Ignore Raycast so mouse clicks pass through) with a glowing trim line along the inner top edge (`ArenaWall`, `ArenaTrim` materials). Clicking on a wall does nothing.
- `Void Stars`: a particle system far below and behind the arena; it is only seen through the strip above the north wall.
- The main light is realtime (the scene has no baked lighting) and the ambient is a flat dark violet. The camera background is a near-black violet.

**Scene setup:** `BossSelector` has one entry (Second Boss). The player starts at the south, the boss at the north. The boss's teleport distances are overridden on the scene instance for this arena (8 to 20 m from the player, at least 7 m of movement, 2 m from the walls). There is no boss 1 level here, no slimes and no `firstBoss` reference.

**Start screen:** card 2 loads this scene (boss index 0).

**Verified:** the scene loads from the start screen; clicks in the frame hit the floor; spell circles, explosion and 8 teleports all stay inside the arena; 0 console errors. Not verified: real mouse play, other window sizes (only 4:3 was available, which gave the letterbox), and a standalone build.
