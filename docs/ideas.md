# Ideas

A parking lot for ideas. Add them in a line or two, don't design yet. When one is picked up, move it to **In progress** and link its design note under `docs/`.

Format: `- #N Idea name [size]: what it is and why. (needs: assets, decisions or tech)`. Size is S (under an hour), M (a session), L (several sessions). The numbers are from the first brain dump (2026-10-08) and stay fixed so they can be referred to.

## Features

- #8 New melee character, combo based [L]: the ninja is built on QTE casting, this one is a combat fighter in the style of Riven (chained attacks, animation cancels, short dashes, a shield, an empowered finisher). Needs a character select screen (see #9) and sword clips from the animation library.
- #1 Beastmaster boss with low poly animal companions [L]: every animal has exactly one skill. Needs a low poly animal pack with animations and a model for the beastmaster. Per `CLAUDE.md`, split `FirstBoss.cs` skills into reusable components before a third boss.

## Design

- #6 A better way to damage the second boss [M-L]: she teleports so often that a melee character could never reach her. Options in the backlog artifact; recommended mix: teleport less often, a vulnerable or counterable window after each cast sequence (uses #2), and a marker where she will land.

## Refactor

- #7 Proper boss names and file names [M]: "First Boss" and "Second Boss" will stop being true. Rename folders, scripts, namespaces (`Bosses.First_Boss`), prefabs, scenes, the BossSelector entries, the start screen cards, the debug harness and the docs. Needs the final names from you. Move assets through the Unity CLI so GUIDs survive. Do it before #1.

## Suggested order

1. #7 renames: before more content piles up under the old names.
2. #6 second boss redesign (it can use the E prompt from #2).
3. #8 melee character (it needs a character select screen), then #1 beastmaster. The melee character is the test for #6.

## In progress

## Done or dropped

Tested in Play mode through the CLI on 2026-10-09. Still to playtest by hand: the feel of the Rock Shower speed, the first boss at 500 health, the dash against a wall, and a real `S` press during the intermission.

- #2 "E" prompt on counterable enemies and objects: `UI/CounterPrompt` is a key cap (white cap, green rim, bobbing and pulsing) built in code like `DamageNumber`, so nothing is wired in a scene. `BossController.ActivateCounterWindow` / `StopCounterWindow` and `IntermissionStone.MakeCounterable` / `RemoveCounterable` show and hide it, and it follows the collider's top. Checked in Play mode on both bosses and on the four intermission stones (all four show it, all vanish when one is kicked). The letter is a constant (`KeyLabel = "E"`), so a character with another counter key needs it changed. To playtest: the real counter window comes from the "Standing Melee Combo Attack" clip's animation event, which I opened through reflection instead.
- #9 Boss death and victory menu: `BossController` has a real death now (`Defeat`, `IsDead`, virtual `OnDefeated`), both bosses have a death animation (second boss: its own Standing React Death Backward set to Humanoid; first boss: the library's Death Backward baked onto the Maw with the new `Assets/Editor/HumanoidClipBaker.cs`), and `GameMenuScreen.ShowVictory` shows "YOU WIN!" with "Kill the boss again!" and "Boss Selection". The character screen does not exist yet, so the second button goes to `StartScene` until #8 adds one. F7 in the debug harness sets the boss to 1 health. Design note and how to give a new boss a death: `docs/boss-death.md`. Still to playtest by hand: the feel of the fall and the 4 s delay, and Escape on the victory screen.
- #3 Spell Circles explosion 15% slower: `_circleTelegraphTime` 0.5 to 0.575 (prefab and script default). Fire rate unchanged. Second boss.
- #4 Rock Shower 20% slower: `_rockShowerSpeed` 45 to 36 on the first boss prefab (speed x 0.8). Measured 36.0 on every rock.
- #5 Tankier first boss: the real value was the `TestScene` override (250, not the prefab's 10 or the script's 100), now 500. Phase 2 still starts at half health.
- #10 Dash landed inside the rocks: `SkillDash` in `PlayerController` now flattens the direction and stops at the first NavMesh edge (`NavMesh.Raycast`). A dash toward a wall 4.5 units away moved 4.53 units instead of 10.
- #11 Dodge during the first boss intermission hung the fight: the slime explosion (`DamagePlayer(1000)`) was blocked by the dodge or the hit cooldown, and the intermission only ends when the player dies. `DamagePlayer` has a new `isUnavoidable` flag that skips both; the explosion sets it. Debug invulnerability (F2) still applies. As a side effect the intermission explosion can no longer be dodged.
