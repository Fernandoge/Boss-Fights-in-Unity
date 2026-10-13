---
name: level-design
description: Use when creating or editing a boss level or arena in this Unity project (new scene, moving or adding trees or walls, NavMesh changes, camera offset changes, patrol zones). Gives the level rules and the visible-floor bake.
---

Read `docs/level-design.md` first: it holds the rules (open arena, the player must always be visible, walk through everything visible except the scenery) and the NavMesh recipe.

For a forest level with a follow camera, after any change to trees, walls or the camera offset: `unity command eval --code 'return EditorTools.VisibleFloorBaker.Run();'` (or the menu Tools > Level > Bake Visible Floor), then save the scene and check the player is visible at the limits of the floor (warp the player there in Play mode and capture the game view). Never fade or hide trees to make the player visible, and never edit the generated `Visibility Blockers` by hand.

New scenes must be listed in Build Settings, and a new fight scene needs the Axeman prefab and a `CharacterSelector` (see `CLAUDE.md`).
