# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

A Unity 6000.3.25f1 (Unity 6.3 LTS) boss-fight game. The Unity project lives in `BossFights/` (open that folder in Unity Hub, not the repo root). All gameplay code is under `BossFights/Assets/Scripts/`. The main scene is `BossFights/Assets/Scenes/TestScene.unity`.

There is no lint step and no committed test suite. The C# solution is `BossFights/BossFights.sln` (Rider is the configured IDE).

## Unity CLI (driving the open Editor)

The `unity` CLI (winget `Unity.CLI`, at `%LOCALAPPDATA%\Microsoft\WindowsApps`, may need adding to `PATH`) talks to the open Editor through the `com.unity.pipeline` package (server on port 7800, no sign-in needed). Run it from `BossFights/`. `unity status` shows the connected Editor; `unity list` lists the ~160 tools; call a tool as `unity command <tool> --param value`. In Git Bash set `MSYS_NO_PATHCONV=1` or hierarchy paths like `/Player` get rewritten. Flags use underscores (`--save_path`); a tool called without arguments names its first missing required parameter.

**After editing C#:** `unity recompile`, poll `unity command recompile_status` until `completed`, then `unity command console_status` — `compilationFailed: false` and 0 errors means it compiled; `unity command console` shows the entries. Tool failures are also logged to the console, so clear it (`clear_console`) before judging errors.

**Verifying behaviour (tested):**
- `get_scene_hierarchy`, `find_gameobjects`, `get_serialized_fields --target <path> --component <Type>` (a GameObject target needs `--component`), `get_animator_controller --controller <asset path>`.
- Playtest: `editor_play`, drive the player with `eval` (e.g. `NavMeshAgent.SetDestination`; `eval` takes statements and needs `return ...;`), `set_timescale --scale N` (reset to 1 afterwards), `capture_game_view --save_path Assets/<folder>/x.png --width 960 --height 540`, then Read the PNG to look at the game. Always `editor_stop` when done. Save paths must be inside the project — delete captures afterwards.
- `simulate_key` / `simulate_pointer` do NOT work: they need the Input System package and this project uses the old Input Manager.
- Animator editing works (`add_animator_parameter`, `add_animator_state --isDefault`, `add_animator_transition --fromState --toState --conditions`, `create_animation_clip`, `set_animation_curve --keys`), as do scene, prefab (`create_prefab --source`, `instantiate_prefab --prefab`, `apply_prefab_overrides --instance`) and asset tools (`rename_asset --new_name`, `copy_asset`/`move_asset --destination`, `find_assets`; use `--type RuntimeAnimatorController` because `AnimatorController` returns nothing). Animation events still need to be checked by looking at the result.
- Tools that change project settings, packages or delete assets refuse without `--confirm true`; use `--dry_run true` first to preview. Settings take `--settings '{...}'`.
- Tests: the Test Framework package is not installed. To use it, `package_add --identifier com.unity.test-framework --confirm true`, then `list_tests` / `run_tests --mode EditMode` (verified working).
- Do experiments in a throwaway folder (e.g. `Assets/_CliTest`) and delete it afterwards. Save the scene (`save_scene`) after scene edits and leave `TestScene` not dirty.

Fallback when the Editor is closed: batch mode (the path contains a space, so it must be quoted):

```powershell
$log = "<scratchpad>\unity-compile.log"
$args2 = '-batchmode -nographics -quit -projectPath "E:\Git Projects\Boss-Fights-In-Unity\BossFights" -logFile "' + $log + '"'
Start-Process "C:\Program Files\Unity\Hub\Editor\6000.3.25f1\Editor\Unity.exe" -ArgumentList $args2 -Wait -PassThru
```

Exit code 0 with no `error CS` lines means it compiled. Never run batch mode while the Editor has the project open (`Get-Process Unity`, `BossFights/Temp/UnityLockfile`).

Branches: work happens on `dev`; `main` is the PR/merge target. Commits use the repo-local identity `Fernandoge <fernando.polgati.g@gmail.com>` (never the global work identity). The `pre-ai-development` tag marks the code before AI-assisted work. The TerrainTexturesPackFree import is intentionally left untracked (files over 100 MB; GitHub rejects them).

## Direction (current thinking, revisit freely)

Goal: the user wants to add player skills, bosses and boss skills mostly by describing them, then playtest and tweak. The first boss fight already feels good, so keep its behaviour intact. Roughly: the user describes a feature in a few sentences, Claude proposes the design and the assets needed, implements it on a feature branch, checks it with the CLI loop (recompile, console, screenshots), and the user playtests and tunes.

- **Split of work:** Claude handles code, Animator wiring, tuning fields and verification. The user sources art (Mixamo FBX downloads need their login), judges game feel, and playtests. Claude imports and configures dropped assets (humanoid rig, loops, prefab, colliders, NavMeshAgent, Animator states).
- **Assets:** Mixamo stays for humanoids (shared skeleton, retargeting). Non-humanoid bosses and minions come from packs with built-in animations. AI-generated 3D is experimental, for props or minions only.
- **Indicators and VFX are the costly part of each skill.** `Shared/SkillIndicator` is the reusable procedural indicator (line shape built, with a fill that sweeps over the telegraph time; add circle, cone and ring when a skill needs them) and is meant to replace the PNG placeholders. See `docs/first-boss/fissure-lines.md` for the pilot skill built on it. Build VFX from the existing packs (Hovl Studio, ParticlePack): copy and trim a pack prefab rather than building particles from scratch, because pack materials often use sprite sheets that need the Texture Sheet Animation module configured.
- **Debug harness (built):** `Debugging/BossDebugHarness` is on a `DebugHarness` object in `TestScene` (compiled only in the Editor and development builds). In Play mode: F1-F8 force a first-boss attack (FastRun, JumpAttack, RockThrow, FrontalAttack, Meteors, RockShower, FissureLines, Cataclysm), F9 toggles boss auto attacks, F10 player invulnerability, F11 phase 2 skill variants (the flag only, not the real phase 2 transition), F12 slow motion, Tab hides the overlay. Each also has a public method, so from the CLI use `eval`, e.g. `Object.FindFirstObjectByType<Debugging.BossDebugHarness>().ForceAttack(Bosses.First_Boss.FirstBossAttack.FissureLines);` (no reflection needed). `FirstBoss.DebugForceAttack` runs the same `StartAttack` as the random attacker. When adding a boss skill, add it to `FirstBossAttack` and `StartAttack` so the harness can force it. The CLI cannot press the F-keys (no input simulation), so call the methods instead. For fast effects use `set_timescale 0.05-0.2`, poll for the effect object, then `set_timescale 0` and capture, since each CLI call takes about a second of real time.
- **Known risks:** animation events on read-only Mixamo clips are untested through the CLI (fallback: time skills in code); `FirstBoss.cs` is ~870 lines, so before a third boss consider splitting skills into reusable components; Claude has no memory between sessions, so keep short design notes per boss and skill under `docs/`.
- **Pilot first:** the indicator system, the debug harness, one new player skill and one new boss skill, before committing to a bigger roadmap. Done so far: the indicator system, the debug harness and the Fissure Lines boss skill; a new player skill is still open.

## Architecture

The game is player-vs-boss encounters driven by Unity's NavMesh, Animator state machines, and animation events.

- **`GameManager`** (`Manager/GameManager/`) — bare singleton (`GameManager.Instance`) holding references to the `player` and `firstBoss`. Bosses find the player through it in `Start()`.

- **Player**: `PlayerController` (abstract, `Characters/`) implements movement (right-click NavMesh move), basic attack (left-click), dash (space), health/damage-immunity, and cooldown handling. `NinjaController` (`Characters/Ninja/`) extends it with skills (Heal, Wall, Clones, Kick, Katon) driven by QTE-style cast inputs. Spawned helpers like `NinjaClone` manage their own lifetime.

- **Bosses**: `BossController` (abstract, `Bosses/`) implements the shared loop — chase/idle movement, attack timer (`timeBetweenAttacks` counts down in `Update`, triggers `PerformAttack()`), health + two-phase transition at half health (`EnterSecondPhase`), skill indicators, and the counter-window system (boss flashes green, collider tag becomes `"Counterable"`, player can `TriggerCounter()`). Concrete bosses (`FirstBoss` in `Bosses/First Boss/`) override `PerformAttack()`/`EnterSecondPhase()` and add their skill roster.

- **Interfaces** (`Interfaces/`): `IDamageableByPlayer` (`TakeDamage(int)`) and `ICounterable` (`TriggerCounter()`) are how the player's attacks talk to bosses/minions.

- **Animation is load-bearing**: most skill sequencing runs through Animator triggers + animation events calling public methods on the scripts (e.g. `BasicAttack`, `ActivateSkillIndicator`, `StopPerformingAttack`). When adding a skill, the C# side is only half the work — Animator parameters, states and transitions can be added through the Unity CLI (see above), but animation-event hookups on clips and anything visual still need to be checked in a playtest (screenshot via the CLI, or ask the user). Always call out what was and wasn't verified.

- **Attack/skill pattern** (see `FirstBoss`): serialized tuning fields grouped by `[Header("Skill Name")]`, a static readonly Animator hash per skill, a trigger method, and animation-event callbacks. Skill telegraphs use `TriggerSkillWithIndicator()` from the base class.

## Coding Standards

(Migrated from the former `.github/copilot-instructions.md`.)

### Naming
- Private fields: `_underscorePrefix` (including `[SerializeField] private`)
- Protected fields: `camelCase`, no underscore
- Public fields, methods (all visibilities), properties: `PascalCase`
- Animator hashes: `static readonly int` with `PascalCase_With_Underscores`, e.g. `private static readonly int Jump_Attack = Animator.StringToHash("JumpAttack");` — hash string must match the Animator parameter exactly
- Namespaces mirror folder structure: `Bosses.First_Boss`, `Characters.Ninja`, `Manager.GameManager`

### Style
- **Always use explicit types — never `var`**
- Single-statement methods: expression-bodied (`=>`) instead of braces
- Single-statement `if`/`else` bodies: omit braces
- **No XML doc comments (`/// <summary>`)** — self-explanatory names, sparse inline comments only
- **No `Debug.Log`** unless the user explicitly asks
- Section separators: `/// *** Section Name *** ///`
- Group serialized fields with `[Header("...")]`

### Class member order
1. Serialized fields (grouped by `[Header]`) → public → protected → private fields → static readonly fields → properties → Unity lifecycle methods → public → protected → private methods → nested types

### Unity patterns
- Cache component references in `Awake()`/`Start()`
- Reuse allocation-heavy objects (`NavMeshPath`, etc.); prefer `sqrMagnitude` over `Distance` for comparisons
- Spawned objects manage their own lifetime with `Update()` timers, not spawner coroutines (spawners may call `StopAllCoroutines()`)
- Animation-event methods must be `public` with clear action names (e.g. `ActivateJumpingAttackParticles`)
