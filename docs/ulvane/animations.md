# Ulvane: animations

The Pro Longbow Pack zip (`BossFights/Pro Longbow Pack.zip`, 39 Mixamo clips plus a copy of the Kachujin model) was already imported: every clip in it exists in `Assets/Asset Packs/Mixamo/Animation Library/` under a short Title Case name (see `docs/animation-library.md`), so nothing new needed importing. The zip's `Kachujin G Rosales.fbx` was not imported. All clips are Humanoid, so they play on the Erika Archer (Humanoid, own avatar) through retargeting.

Ulvane's Animator has **Apply Root Motion off** (the NavMeshAgent and code move him), so clips with built-in movement (dodges, dives, falls) will not move him by themselves; move him in code during the clip.

Controller: `Assets/Animations/Ulvane/Ulvane Controller.controller`. Parameters: `Walking`, `MoveX`, `MoveZ` (floats for the walk blend), `Draw`, `Release`, `Command`, `Countered`, `EnterSecondPhase`, `Die` (triggers), plus `PerformingAction` which `BossController` expects.

## In use

| State | Clip | Why |
|---|---|---|
| Idle | Idle 01 | calm standing, bow in hand |
| Walking (2D blend `MoveX`/`MoveZ`) | Walk Forward, Walk Back, Walk Left, Walk Right | kiting: back away, close in, circle while facing the player |
| Draw Arrow | Bow Draw Arrow (1.0 s) | start of the arrow shot |
| Aim Overdraw | Bow Aim Overdraw (3.73 s, left early with `Release`) | holds the draw while the lane fills |
| Aim Recoil | Bow Aim Recoil (0.7 s) | the release |
| Command | Gesture Dismissing (3.27 s, left at 45%) | the "go" wave that calls an animal (placeholder pick) |
| Roar | Gesture Angry (2.17 s) | phase 2 transition (placeholder pick) |
| Hit React | Hit React Front (1.27 s) | `Countered` trigger; nothing triggers it yet |
| Death | Death Backward (3.07 s) | `Die`; the clip's *Root Transform Position (Y)* is set to *Bake Into Pose* (changed in the library for this) so the fall does not float |

## Good fits for what comes next

| Clip | Idea |
|---|---|
| Bow Aim Walk Forward / Back / Left / Right | a moving shot: strafe with the bow drawn (a phase 2 arrow skill) |
| Run Forward / Back / Left / Right, Run Forward Stop | repositioning sprints; Run Back to escape a player who got close; Run Forward Stop for the skid |
| Dodge Backward / Left / Right / Forward, Dive Forward | a hop away when the player closes in (move him in code); a rolling dodge also works as the "cornered" answer |
| Melee Kick, Melee Punch | point-blank shove when the player is on top of him |
| Bow Disarm, Bow Equip | bracket the animal command (put the bow away, whistle, take it back), or a phase where he fights bare-handed |
| Idle Unarmed 01 | idle while the bow is away |
| Block | a guarded stance, a candidate for a counterable window |
| Hit React Headshot | the heavy stagger when a counter lands |
| Idle 02 Looking, Idle 03 Examine | idle variety, a scouting intro |
| Turn 90 Left / Right | turning in place before a shot |
| Fall Loop, Fall Land To Idle, Fall Land To Run | being knocked up, or a leap |
| Death Forward | a second death variant |

## Animals

The `Animals_FREE` animals only have idle, walk and run clips, in a blend tree on `Vert` (0 = idle, 1 = moving) and `State` (0 = walk, 1 = run). `UlvaneBeast` drives them (`Idle()`, `Gallop()`); every skill is timed in code. They have no attack, hit or death animations, so a bite or a stomp is the run clip plus the effect on the floor.
