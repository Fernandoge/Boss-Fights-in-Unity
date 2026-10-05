# Second boss: plan

Status: M1 (foundation) and M2 (basic boss, with the first skill) are built and verified; see `docs/second-boss/spell-circles.md`. TurtleShell was rejected, so the model comes from the user (Mixamo humanoid: `Nightshade J Friedrich` plus a magic/crouch/block animation set, dropped in `Assets/Asset Packs/Second Boss/`). `main` and `dev` are the safe rollback point.

## Goal

Add a second boss that fights in the same arena. Milestone 1 is only the basics: it chases the player and idles like the first boss, takes damage, shows its health and flashes. Skills, a second phase and a new map come afterwards, one at a time, with the same design, build, test, tune loop used for the first boss. A proper boss selector is not needed yet: a simple switch that activates one boss and deactivates the other is enough.

## What I reviewed (and what it means)

**1. Everything first-boss-specific is isolated.** All skill scripts live in `Bosses/First Boss/` and read damage values through `GameManager.Instance.firstBoss`. `GameManager` has a single typed slot, `public FirstBoss firstBoss`. A second boss must not reuse that slot: it gets its own slot and its own skill scripts, so the first boss is not touched.

**2. `BossController` is the shared base and is a good fit, but it has a contract.**
- It needs these Animator parameters with exactly these names: `Walking` (bool), `PerformingAction` (bool), `EnterSecondPhase` (trigger), `Countered` (trigger).
- Movement only runs while the Animator is in a state named exactly `Walking`. The default state must carry the tag `WalkingIdle`.
- Every attack ends through an animation event: the `Idle` clip calls `StopPerformingAttack` at 0.5 s. Without that event the boss stays "attacking" forever and never moves again.
- At 50% health the base class starts a phase transition: it sets `PerformingAction`, fires the `EnterSecondPhase` trigger and waits for an animation event to end it. A boss without a phase transition animation would freeze permanently. Milestone 1 therefore overrides `EnterSecondPhase` to do nothing.
- It reads `GetComponentInChildren<SkinnedMeshRenderer>().material` and sets `_Color` for the hit flash (needs a Standard-shader material), and `GetComponentInChildren<Collider>()` (the first collider found) as the counter collider.
- It writes the health number into a `TextMeshProUGUI`. The scene already has one (`Camera Pivot/Canvas/Health Background Boss/Health Bar`); two bosses can share it because only one is active at a time.

**3. The player already works with any boss.** Player projectiles damage whatever has an `IDamageableByPlayer` in its hierarchy, which `BossController` implements. The kick counter looks for the tag `Counterable` on the boss's counter collider. Nothing in the player code names a specific boss.

**4. The first boss's setup to copy:** root object on the `Ignore Raycast` layer (so mouse clicks pass through it), scale 2, a `NavMeshAgent` (speed 4, instant turning and acceleration, no avoidance), an `Animator`, a hips capsule collider, health 100, `stopBetweenPlayer` 4.5, `rotationSpeed` 3, an attack every 5 s. Its model is a Humanoid-rig Mixamo character with a Standard-shader material.

**5. The level has always-active parts that belong to the first boss.** Under `First Boss Level`: `Slimes` (wander the map on their own; an exploding slime deals 1000 damage), `BossObjects` (meteor and rock shower indicators) and `Intermission` belong to boss 1. `RocksWall` and `Floor` (the NavMesh) are shared. The selector must switch the boss-1 parts off together with boss 1, or slimes would roam during the second fight.

**6. Debug harness:** `BossDebugHarness` and its attack list are typed to `FirstBoss`. It needs to work on whichever boss is active.

**7. Gaps that exist for both bosses (not part of milestone 1):** no boss death or victory handling (health can go below 0), no audio anywhere in the project.

## Risks and how we avoid them

| Risk | How we avoid it |
|---|---|
| Breaking the first boss | Changes to shared code are additive only (a new `GameManager` slot, a new selector). `FirstBoss.cs` is not edited. After every milestone I re-run the first boss smoke test: force all 8 attacks, a 60 s natural run, the phase 2 transition, 0 console errors, scene not marked modified. |
| Second boss freezing at 50% health | Override `EnterSecondPhase` in milestone 1; build a real transition only when its animation exists. |
| Boss never attacks again after an attack | The new Animator controller follows the contract above, including the `StopPerformingAttack` event on its idle clip. |
| Slimes and level objects interfering | The selector activates and deactivates the boss and its level parts as a group. |
| Hit flash errors | Check the model's material has `_Color` before using it. |
| Mouse clicks blocked by the boss | Same `Ignore Raycast` layer setup as boss 1, verified with a ray test. |
| Too much at once | Strict milestones, each on a branch with small commits, each verified in the editor and with screenshots before the next. |

## Milestones

**M0 Decisions and assets (needs you).** See the checklist below.

**M1 Foundation, with no behaviour change to boss 1 (done).**
- `GameManager` gets a general `ActiveBoss` (a `secondBoss` slot is added in M2 when the class exists).
- A small `BossSelector` component activates one boss group and deactivates the other, chosen in the Inspector.
- The harness works on the active boss; forced attacks exist for boss 1 only until the second boss has skills (then a small interface lets each boss list its debug attacks).
- Smoke test boss 1: all 8 forced attacks play the right clip, a natural run uses every clip, the phase 2 transition works (walk 4 to 6), 0 console errors.

**M2 Second boss basics (done, with the Spell Circles skill and teleport).**
- Model prefab with agent, collider, layer, material, scale.
- Animator controller following the contract (idle, walk, and the idle event).
- `SecondBoss : BossController` with chase, health, flash and a no-op phase 2.
- Checks: it chases and stops at the right distance, turns to face the player, takes damage from projectiles, the health text updates, no console errors, boss 1 unaffected.

**M3 Skills, one at a time.** Each skill: you describe it, I propose the design and what it needs, I build it with the harness key, we test and tune. Reuse `SkillIndicator`, the VFX approach and the harness from boss 1. Then a phase 2 and its transition.

**M4 New map.** Out of scope for now.

## What I need from you

1. **The asset.** Done: a Mixamo humanoid (`Nightshade J Friedrich`) with magic attack, cast, block, crouch, walk/run, react and death animations. Because it is Humanoid, the existing Maw animations also retarget onto it.
2. **Boss concept:** name, theme, rough size, how it fights (melee, ranged, summoner, trapper), how it differs from boss 1.
3. **Skill ideas:** three to five in rough words, and what phase 2 should change.
4. **Animations:** for option A I can map the existing clips to skills; for B tell me what to download; any clip missing for a skill is the main thing that can block a skill.
5. **Particles/VFX:** I will reuse the existing packs and build simple ones; send specific packs if you want a certain look.
6. **Health and tuning starting values:** I will start from boss 1's values unless you say otherwise.

## Out of scope for now

A proper boss selector UI, the new map, boss death and victory handling, audio.
