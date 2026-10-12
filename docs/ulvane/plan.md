# Ulvane (third boss): first build

Idea #1 in `docs/ideas.md`. Status: placeholder slice built and checked in Play mode through the CLI on 2026-10-10: the model, the forest level, one skill for Ulvane and one for each of the 7 animals. Everything is tuning-level placeholder; nothing here has been playtested by hand yet.

## Concept

A longbow hunter who keeps his distance and calls animals. Gorath is a brute that chases, Vexara is a stationary caster that teleports; Ulvane is a handler: the animals do most of the attacking and the fight is about reading which animal is coming and where. Every animal has exactly one skill.

## What exists

- **Model:** `Assets/Asset Packs/Ulvane/Model/Erika Archer.fbx` (the Mixamo Erika Archer with bow and arrow, moved from `docs/`). Set to Humanoid (own avatar), materials extracted to `Ulvane/Materials`, textures to `Ulvane/Textures`. The mesh names inside the FBX are shifted against their contents (the mesh called `Arrow_Mesh` is the clothes, `Eyes_Mesh` is the arrow); the materials follow the contents, so it renders right. The arrow is a separate skinned mesh on the `mixamorig:arrow` bone, so it can be hidden later.
- **Prefab:** `Prefabs/Characters/Bosses/Ulvane.prefab` (scale 1.3, NavMeshAgent speed 3.2, capsule collider on the root, layer Ignore Raycast, 300 health). Animator: `Animations/Ulvane/Ulvane Controller.controller`, built only from library clips, see `docs/ulvane/animations.md`.
- **Scripts** (`Scripts/Bosses/Ulvane/`, namespace `Bosses.Ulvane`):
  - `UlvaneBoss`: kiting movement, attack choice (never the same twice), the arrow shot, the beast sequence, a placeholder phase 2.
  - `UlvaneBeast` (abstract): the animal lifecycle (pop in beside Ulvane or at the edge, perform its skill, pop out), movement that stops at the NavMesh edge, telegraph helpers. One subclass per animal.
  - `UlvaneProjectile`: arrows and snowballs. Straight over the floor, stops at the NavMesh edge.
  - `UlvaneAttack`: the enum the harness uses.
  - Shared: `Shared/GroundBlast` (a circle that fills, then hurts inside it), `Shared/NavMeshUtility` (snap to the floor, clamp a line to the edge).
- **Beast prefabs:** `Prefabs/Characters/Bosses/Ulvane/<Animal> Beast.prefab`, variants of the `Animals_FREE` prefabs with the pack's movement scripts removed. Scales are guesses (tiger 1.2, dog 1.8, horse 1.3, deer 1.2, chicken 2.6, penguin 1.1, kitty 3.5). The animals only have idle, walk and run clips, so every skill is code driven.
- **Level:** `Scenes/UlvaneScene` (in Build Settings), see below.
- **Shared change:** `BossController` now flashes every material of the model (hit red, counter green), not just the first one, because Ulvane is built from 6 meshes. Gorath and Vexara have one material each, so they look the same.
- **Debug harness:** works for Ulvane. Keys 1-8 force an attack in `UlvaneAttack` order, F5 repeats the one chosen in `Ulvane Attack To Use` on the `DebugHarness` object.

## Skills

| Key | Attack | Who | What happens | Damage |
|---|---|---|---|---|
| 1 | ArrowShot | Ulvane | Draws the bow, an orange lane locks on the player (aim with the perceived position plus a little lead), arrow flies down the lane. Phase 2: 3 arrows in a fan. | 2 |
| 2 | TigerPounce | Tiger | Crouches while a circle fills where the player is heading, leaps and lands on it. | 2 |
| 3 | DogHunt | Dog | Warning ring, then chases the player for 4.5 s, turning slowly, biting on contact. | 1 per bite |
| 4 | HorseTrample | Horse | Appears at the edge, a wide lane fills towards the player, gallops down the whole lane. | 2 |
| 5 | DeerStomp | Deer | Runs up to the player, a ring fills around it, stomps; twice. | 1 |
| 6 | ChickenEggs | Chicken | Runs circles around the player laying 5 eggs, each a circle that explodes 1.3 s later. | 1 |
| 7 | PenguinSnowballs | Penguin | 5 lanes fill in a fan, a snowball flies down each. | 1 |
| 8 | KittyFlurry | Kitty | Three quick dashes at the player, each with a short lane. | 1 |

All the animals aim with `PerceivedPlayerPosition` / `PerceivedPlayerVelocity` (through `UlvaneBoss.PlayerAimPoint`), never the live position. Skills of 2 hearts show the danger icon above Ulvane.

Kiting: he stays 8 to 13 units from the player (backs away when closer, walks closer when farther, circles in between at 60% speed, switches side every 2 to 4 s, slides sideways if cornered). His speed (3.2) is below the player's run speed, so a melee player can catch him. While a skill runs he stands still and faces the player.

## Level

`Scenes/UlvaneScene` is a copy of `TestScene` with the same follow-the-player orthographic camera (pitch 30, yaw 45, size 12), the player, the UI and the harness. Gorath and Vexara are removed; `BossSelector` has one entry (Ulvane).

- **Open arena:** a 51 x 51 flat floor with nothing on it, so movement is free and only the mechanics matter. Checked: 784 straight lines between points all over the arena, none touches an edge. Decoration inside is flat only (dirt patches, grass tufts, no colliders).
- **Forest wall:** the outer ring is dense trees (the Fantasy Forest pack's `tree_1`) and a few rocks. It is made not walkable with four `NavMeshModifierVolume` walls, so the wall holds even where the trees leave gaps. The rocks (RockFREE) are about 20 times too big at scale 1, so they are scaled to 3.5 to 6.5 units.
- **NavMesh:** a `NavMeshSurface` on `Forest Level` built from physics colliders; data in `Scenes/UlvaneScene/NavMesh-Forest.asset`. Rebuild it after moving trees or walls (`NavMeshSurface.BuildNavMesh()`, then save the data as an asset; the CLI's `bake_navmesh_surfaces` is not implemented for surfaces).
- **Trees hide when in the way:** `Cameras/ForestOcclusionFader` (on the camera) casts from the camera to the player and the boss, and trees (`Cameras/ForestTree`) in the way stop being drawn but keep their shadow. They pop instead of fading.
- Fog, a procedural sky and a warm sun set the mood. Ground is the pack's `grass01` texture on a Standard material.

## Checked in Play mode (CLI)

- Compiles, 0 console errors through all of this.
- Each of the 8 attacks forced with the harness, with screenshots: telegraph shows, the animal appears and does its thing, hits are applied (the player died to the natural rotation when left standing).
- A natural run of about 3 minutes of game time (3x speed) with auto attacks: attacks alternate, no errors.
- Phase 2 (F6-style hit): the roar plays, the flag is set, the arrow volley and the faster attack timer apply.
- Death: `Death Backward` plays, `OnDefeated` clears animals, blasts, projectiles and indicators; 0 errors.
- After the `BossController` material change, `TestScene` (Gorath) and `VexaraScene` were played and hit once: 1 renderer and 1 material each, so the flash is unchanged, 0 console errors. The full boss 1 regression (8 forced attacks, phase 2) was not re-run.

## Not checked, known gaps

- No hand playtest: feel, speeds, sizes, damage and difficulty are all guesses.
- The clips for the command gesture (`Gesture Dismissing`) and the roar (`Gesture Angry`) were picked by name, not by looking at them; swap them in the controller if they look wrong.
- The death pose and the victory screen delay were not looked at in a screenshot.
- Ulvane does nothing while an animal works (he just watches); the intended fight has him keep kiting and shooting meanwhile.
- No counter window or reachable-target design yet (see #2 in `ideas.md`); the E prompt and `Countered` state exist but nothing opens them.
- Phase 2 is a placeholder (roar, faster attacks, arrow volley).
- No start screen card for Ulvane (`StartScene`), no particles beyond the Smoke Puff, no sound.
- Animals are not damageable and have no hit reactions.
- Trees pop in and out instead of fading.

## Next steps

The to-do list lives in `docs/ideas.md` as #14 to #23 (playtest and tuning, damage model, Ulvane acting while the animals work, phase 2, start screen card, the Axeman in this scene, polish, commit and regression). Start with #14.
