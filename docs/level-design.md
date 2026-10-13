# Level design rules

How a boss level is built and what has to be true of it. Written down after the Ulvane forest (2026-10-12) so the next levels do not repeat the trial and error. Each level can add its own note under `docs/<boss>/level.md` (Vexara has one); this file is the shared part.

## Rules for every level

- **The arena stays open.** No obstacles on the walkable floor, only walls around it and flat decoration (dirt patches, ice, runes). The fight is about mechanics, not about walking around props.
- **The player must always be visible.** The camera follows the player at a fixed offset, so anything tall between the camera and the floor (tree canopies, roofs, pillars) hides them, and they need to see the character to dodge. Do not fade or hide the scenery to fix it (the tree fader was removed because it looked bad); end the walkable floor where the character stops being visible.
- **Walk through everything you can see, except on the scenery itself.** A visible patch of floor the player cannot walk on feels wrong. Block the NavMesh only where the scenery is (a tree trunk, a wall) or where the character would be hidden.
- **Animals and minions use the same NavMesh as the player**, so they cannot go deep into the forest either. Give them patrol zones (`UlvanePatrolZone`) that sit on the walkable floor.
- Boss skills read the player through `BossController.PerceivedPlayerPosition` (see the aiming rule in `CLAUDE.md`), and every skill telegraph must stay on the walkable floor (`NavMeshUtility.DistanceToEdge` and `Snap` clip lanes and points to it).

## NavMesh recipe (forest levels with a follow camera)

1. One `NavMeshSurface` on the level root (`Forest Level`), geometry from Physics Colliders, children only. Trees have a capsule collider and a `NavMeshModifier` (Not Walkable), so each trunk cuts its own hole.
2. Four `NavMeshModifierVolume` walls (Not Walkable) outside the visible ground close the level; their inner edges sit just inside the edge of the ground.
3. Run **Tools > Level > Bake Visible Floor** (`Assets/Editor/VisibleFloorBaker.cs`), or `unity command eval --code 'return EditorTools.VisibleFloorBaker.Run();'`. It deletes the old `Visibility Blockers`, bakes the NavMesh, casts rays from where the camera would be to the character's feet, chest and head on every 1 m cell (against the canopy meshes, with temporary mesh colliders), keeps only the visible cells that connect to the player's start, turns every other cell into `NavMeshModifierVolume` boxes under `Visibility Blockers`, bakes again and saves the NavMesh asset over the surface's old one. Then save the scene.
4. Run it again whenever trees move, trees are added or the camera offset or pitch changes. Do not edit the blockers by hand.
5. Check: warp the player (`NavMeshAgent.Warp`) to the limits of the floor in Play mode and capture the game view; the character must be visible at every limit. Zones, spawn points and the boss start must still be on the NavMesh (`NavMesh.SamplePosition`).

What the tool needs: a `Camera.main` with a `CameraController` whose `player` is the start position, trees with a `LODGroup` (the LOD0 mesh is used for the canopy), and the camera at its start offset in the saved scene.

Known limits: the test is per 1 m cell, so the very edge of the floor can show a character half under the foliage (a slightly stricter version would also require the neighbouring cells to be visible). Leaves are cards, so the mesh test is a little stricter than what is drawn. It does not look at animals.

## Trees

- No LOD switching or culling: the `LODGroup` thresholds are lowered so LOD0 always shows. A tree copied from the scene keeps this; a fresh prefab drop needs it again (set all three LOD transition heights below 0.01).
- No tree hiding. `ForestOcclusionFader` and `ForestTree` were deleted.

## Fixed-camera arenas (Vexara)

The fixed-camera arenas are the other kind: the whole floor is on screen, so none of the above is needed. See `docs/vexara/level.md`.
