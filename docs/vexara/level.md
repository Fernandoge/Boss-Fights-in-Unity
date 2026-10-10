# Vexara level (`VexaraScene`)

A single-screen arena: the camera never moves, and the arena sits centred in a 16:9 frame with a small dark margin around it where the HUD lives (hearts top-left, boss health top-right, skill icons bottom-centre).

**Camera:** the same isometric look as the first level (pitch 30 degrees, orthographic) but fixed, with no yaw (the arena is aligned to the camera, so it fills the screen as a rectangle). `Cameras/FixedArenaCamera` on the Main Camera sizes the view from `Visible Width` (35.56 = orthographic size 10, the first level uses 12) and keeps the view exactly 16:9: when the window is not 16:9 the camera rect is letterboxed and a second camera paints the bars black. The HUD canvas follows the camera rect. The `Camera Pivot` rotation (30, 0, 0) holds the pitch. The old `CameraController` (follow the player) was removed from this scene.

**Fitting the arena to the frame:** with an orthographic camera at pitch P, the screen is `Visible Width` wide by `Visible Width * 9/16` tall; ground distances along z are stretched by `1/sin(P)`. The arena fills the frame up to the HUD: margins of 1.2% of the screen width at the sides, 9.5% of the height at the top (hearts) and 10% at the bottom (skill icons, with a small gap above them). If you change the pitch or zoom, the floor and walls have to be resized to match; the numbers used were  (with the margins above: zSouth = (-orthoSize + bottomMargin * 2 * orthoSize) / sin(P), zNorth = (orthoSize - topMargin * 2 * orthoSize - wallHeight * cos(P)) / sin(P), here about -16 and 16.7).

**Pieces** (all under `Vexara Level`):
- `Floor`: a cube with the `Arena/ArcaneFloor` shader (`Shaders/ArcaneFloor.shader`, material `Materials/Arena/ArcaneFloor`): dark base, drifting nebula, twinkling stars, faint tile grid and a soft glowing border (`Edge Glow` 0.75 and `Edge Line` 0.35 on the material; raise them for a brighter border). It also holds the `NavMeshSurface` (collects only itself); the baked data is `Scenes/VexaraScene/NavMesh-Floor.asset`. Rebake with the surface's Bake button after moving the floor.
- A rune circle (rings, spinning dashed ring, hexagram) is built into the shader but switched off (`Rune Strength` 0 on the material) so it can be used later for skills (`Rune Center` and `Rune Radius` place it).
- `Walls`: four low rims (0.4 high, 0.6 thick, so the floor reaches almost to the frame and no dark wall face hides it; layer Ignore Raycast so mouse clicks pass through) with a purple trim line right at the floor edge on each side (the north and south lines are full width and overhang the side lines, which end flush against them, so top and bottom look the same) (emission kept low so it does not clip to white) (`ArenaWall`, `ArenaTrim` materials). Clicking on a wall does nothing.
- The main light is realtime (the scene has no baked lighting) and the ambient is a flat dark violet. The camera background is a near-black violet. The boss prefab carries a soft violet point light (`Boss Glow`) so the dark wizard stays readable, and the main light and ambient are brighter than the first version.

**Scene setup:** `BossSelector` has one entry (Vexara). The player starts at the south, the boss at the north. The boss's teleport distances are overridden on the scene instance for this arena (8 to 20 m from the player, at least 7 m of movement, 2 m from the walls). There is no boss 1 level here, no slimes and no `gorath` reference.

**HUD (all levels):** the spell icon list is centred at the bottom of the canvas at 75% scale in `TestScene` and `VexaraScene` (child `Spell Icon List` of the canvas; if a new level copies the canvas, it gets the same layout). There are no stars in the void any more.

**Start screen:** card 2 loads this scene (boss index 0).

**Verified:** the scene loads from the start screen; clicks in the frame hit the floor; spell circles, explosion and 8 teleports all stay inside the arena; 0 console errors. Not verified: real mouse play, other window sizes (only 4:3 was available, which gave the letterbox), and a standalone build.
