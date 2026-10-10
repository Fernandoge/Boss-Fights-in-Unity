# Boss death and victory screen

What happens when a boss reaches 0 health, and how to give a new boss a death.

## Flow

1. `BossController.TakeDamage` calls `Defeat()` when health reaches 0. A boss that is immune (phase 2 intermission, teleporting) cannot die until the immunity ends.
2. `Defeat()` sets `IsDead`, stops the boss's coroutines, closes the counter window (so the E prompt goes), hides the current skill indicator, stops the NavMeshAgent, sets the Animator trigger `Die`, calls the virtual `OnDefeated()`, and shows the victory screen after `Victory Screen Delay` seconds (default 4 s, on `BossController`). If the player already died, only the game over screen shows.
3. `OnDefeated()` is where each boss removes what it left on the field: `GorathBoss` clears rocks, fissure lines, boulders, meteors, the boulder sequence, the Cataclysm orbs and the slimes; `VexaraBoss` clears circles, line and clock blasts, orbs, stars, lasers, zones, the colour board and popups, and switches off the Cast Hand arm layer.
4. After the boss is dead `PlayerController.DamagePlayer` ignores every hit, so anything that was missed cannot kill the player during the victory.
5. `GameMenuScreen.ShowVictory` shows "YOU WIN!" with two buttons: **Kill the boss again!** (restarts the same boss, like Restart on the game over screen) and **Boss Selection** (the start screen). `GameMenuScreen.IsOpen` stops the pause key from stacking a pause screen on top.

The "character screen" from the idea does not exist yet: the second button goes to `StartScene`, which only has boss cards. When #8 adds a character select, point `OpenBossSelection` (and rename the button) at it.

## Giving a new boss a death

- Add the `Die` trigger to its Animator controller, a `Death` state with a non-looping clip, and an Any State transition to it (condition `Die`, no exit time, 0.1 s, can not transition to self). The two bosses were wired this way.
- Override `OnDefeated()` to clear that boss's own hazards. Anything that lives in the scene and is not cleared here keeps running.
- Set `Victory Screen Delay` a little longer than the clip, or the screen freezes the last moments of the fall.
- Animation events of the attack that was playing can still fire during the 0.1 s cross fade; `ActivateCounterWindow` already ignores them when dead, so guard any new event that spawns something.

## Clips used

- Vexara (Humanoid): `Vexara/Animations/Standing React Death Backward.fbx`, set to Humanoid with the wizard's avatar copied.
- Gorath (Generic): `Animations/Maw J Laygo/Death.anim`, baked from the library's `Death Backward` clip with `Assets/Editor/HumanoidClipBaker.cs`. A Generic character cannot play a Humanoid clip, so the tool poses the boss prefab with the clip through the Maw's Humanoid avatar and writes the bone transforms into a path-based clip. Humanoid root motion is folded into the hips, so the body keeps its fall without moving the NavMeshAgent root. To bake another library clip onto Gorath (any clip a Generic boss needs):

```
EditorTools.HumanoidClipBaker.Bake(
    "Assets/Asset Packs/Mixamo/Animation Library/<category>/<clip>.fbx",
    "Assets/Prefabs/Characters/Bosses/Gorath.prefab",
    "Assets/Asset Packs/Mixamo/Maw J Laygo/Maw J Laygo.fbx",
    "Assets/Animations/Maw J Laygo/<name>.anim");
```

Run it through the Unity CLI `eval`. It overwrites an existing output clip in place, so states that use it keep their reference. Keys are reduced to a small tolerance (about 4 MB for a 3 s clip, like the other Maw clips); change `KeyTolerance` for a smaller or more exact clip.

## Lessons from the two death clips

- **Humanoid clip on a Humanoid boss (Vexara):** a death clip that falls to the floor needs **Root Transform Position (Y): Bake Into Pose** on in the clip's import settings. Her Animator has Apply Root Motion off, so without it the fall lives in the root's height, which is dropped, and she lies down about 2 m above the floor. Her clip now has it on (`lockRootHeightY`).
- **Baked clip on a Generic boss (Gorath):** the baker must not write two keys a hair apart at the end of a clip; the last spline segment is almost zero wide and its slopes throw the pose off (his left leg lifted at the very end). `HumanoidClipBaker` now writes one key per frame and a single end key, and reads the last frame just before the clip's end, because sampling a Humanoid clip at exactly its length wraps to a wrong pose.

## Debug

F7 in the debug harness sets the boss to 1 health, so the next hit kills it. `BossController.DebugSetHealthToOne` does the same from the CLI.

## Checked and not checked

Checked in Play mode through the CLI on both bosses: the fall and the final pose, the victory screen, both buttons, the hazards removed (22 rocks in flight, a diagonal line blast), the player unhurt afterwards, a player already dead (game over only), and 0 console errors. Not checked: pressing Escape on the victory screen (the CLI cannot press keys), a kill during every other skill (the clean-up covers the known hazards), and how the death feels (the delay and the clips are placeholders for taste).
