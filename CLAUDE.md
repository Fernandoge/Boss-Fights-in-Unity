# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

A Unity 2022.3.4f1 boss-fight game. The Unity project lives in `BossFights/` (open that folder in Unity Hub, not the repo root). All gameplay code is under `BossFights/Assets/Scripts/`. The main scene is `BossFights/Assets/Scenes/TestScene.unity`.

There is no test suite or lint step, and Claude cannot playtest or verify Animator/scene wiring — ask the user to playtest and report results. The C# solution is `BossFights/BossFights.sln` (Rider is the configured IDE).

**Compile check:** after editing C# files, verify compilation with Unity's batch mode (the editor must be closed, since a project can only be open once; the project path contains a space, so it must be quoted):

```powershell
$log = "<scratchpad>\unity-compile.log"
$args2 = '-batchmode -nographics -quit -projectPath "E:\Git Projects\Boss-Fights-In-Unity\BossFights" -logFile "' + $log + '"'
Start-Process "C:\Program Files\Unity\Hub\Editor\2022.3.4f1\Editor\Unity.exe" -ArgumentList $args2 -Wait -PassThru
```

Exit code 0 with no `error CS` lines in the log means it compiled. If Unity is running (`Get-Process Unity`) or `BossFights/Temp/UnityLockfile` exists, skip the check and ask the user to confirm compilation in the editor instead.

Branches: work happens on `dev`; `main` is the PR/merge target.

## Architecture

The game is player-vs-boss encounters driven by Unity's NavMesh, Animator state machines, and animation events.

- **`GameManager`** (`Manager/GameManager/`) — bare singleton (`GameManager.Instance`) holding references to the `player` and `firstBoss`. Bosses find the player through it in `Start()`.

- **Player**: `PlayerController` (abstract, `Characters/`) implements movement (right-click NavMesh move), basic attack (left-click), dash (space), health/damage-immunity, and cooldown handling. `NinjaController` (`Characters/Ninja/`) extends it with skills (Heal, Wall, Clones, Kick, Katon) driven by QTE-style cast inputs. Spawned helpers like `NinjaClone` manage their own lifetime.

- **Bosses**: `BossController` (abstract, `Bosses/`) implements the shared loop — chase/idle movement, attack timer (`timeBetweenAttacks` counts down in `Update`, triggers `PerformAttack()`), health + two-phase transition at half health (`EnterSecondPhase`), skill indicators, and the counter-window system (boss flashes green, collider tag becomes `"Counterable"`, player can `TriggerCounter()`). Concrete bosses (`FirstBoss` in `Bosses/First Boss/`) override `PerformAttack()`/`EnterSecondPhase()` and add their skill roster.

- **Interfaces** (`Interfaces/`): `IDamageableByPlayer` (`TakeDamage(int)`) and `ICounterable` (`TriggerCounter()`) are how the player's attacks talk to bosses/minions.

- **Animation is load-bearing**: most skill sequencing runs through Animator triggers + animation events calling public methods on the scripts (e.g. `BasicAttack`, `ActivateSkillIndicator`, `StopPerformingAttack`). When adding a skill, the C# side is only half the work — Animator parameters, states, and event hookups must be done in the editor by the user. Always call this out.

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
