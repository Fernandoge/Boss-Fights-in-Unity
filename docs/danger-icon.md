# Danger icon

A yellow warning triangle with a red rim and an exclamation mark floats above a boss while it casts a skill that takes **2 or more hearts** (`BossController.HeavyHitDamage`). It flashes faster than the E counter prompt so the two are never confused, and it disappears when the attack ends, when the boss dies, or when a counter window opens (the E prompt takes its place).

## How it works

- `UI/TargetMarker` is the shared sign: it follows a collider's top, faces the camera, bobs, pulses and pops in and out. `UI/DangerIcon` and `UI/CounterPrompt` are small builders that put their shapes on one. Everything is made in code, so nothing is wired in a scene.
- `BossController.ShowDangerIcon(int skillDamage)` shows it only when the damage reaches `HeavyHitDamage`; the base `Update` hides it when `isPerformingAttack` ends. So the icon follows the live damage numbers: change a damage value and the icon follows.
- The exclamation mark is a bar and a dot drawn into a sprite, not a font letter, so it is centred by construction (a bold glyph sat a few pixels off centre).
- Gorath calls it from `StartAttack` through `GetAttackDamage`, which maps each attack to its damage field. Vexara calls it around the clock waves in `ClockIntermissionSequence`.

## What shows it today

Read from the live values in `TestScene` and the Vexara prefab: Gorath's **Cataclysm** (its rocks use `Cataclysm Rock Damage`, 2) and Vexara's **clock waves** (2). Rock Throw is 1 (`Rock Damage` override in `TestScene`) and Rock Shower is 1 (`Rock Shower Damage`), so neither shows it. Fast Run and Jump Attack are 2 on the Gorath prefab but overridden to 1 in `TestScene`, so they show nothing either.

## Adding a heavy skill to a new boss

Call `ShowDangerIcon(damage)` when the skill starts (the base class hides it when the attack ends). For a skill that runs inside a longer attack, call `HideDangerIcon()` when its dangerous part is over.
