# Animation library (Mixamo)

Check this file first when a skill or character needs an animation. Everything here is a Mixamo clip already inside the Unity project, so no download is needed. This catalog was generated from the project's clip data (177 clips).

## How the clips work

- **Humanoid clips play on any character whose model FBX is set to Humanoid.** The ninja (`Kachujin G Rosales.fbx`), the Maw (first boss) and the wizard (`Nightshade J Friedrich.fbx`) all are, so a library clip can drive any of them. Animals, slimes and other non-humanoid models cannot use them. A new Mixamo character becomes usable by setting its model's Rig tab to Humanoid.
- A clip's name is its file name. Names in the library are unique across the project, so an Animator state can be told apart by name alone.
- **Looping:** the library sets idles, runs, walks, strafes and falls to loop and everything else to play once. The heuristic is in `Assets/Editor/MixamoAnimationImporter.cs`; fix a wrong guess by ticking or clearing *Loop Time* on the FBX's Animation tab.
- Clips are read-only FBX sub-assets: animation events cannot be added to them. To use a clip with events, copy it (`AssetDatabase.CopyAsset` on a `.anim` extract) or time the skill in code, as the Dodge and Whirlwind do.
- **Root motion:** the ninja's Animator has *Apply Root Motion* on, so a clip that carries movement (dodges, dives, some turns and falls) will physically move him, and the NavMeshAgent does not know about it. Download locomotion *In Place*, and check any clip with built-in movement in Play before building a skill on it (the S dodge and F whirlwind move the ninja in code instead).

## Adding new clips

1. Download from Mixamo: **FBX Binary, Without Skin, 30 fps**, *In Place* for walks and runs. Put the FBX files (unzipped, or tell Claude where the zips are) in a category folder under `Assets/Asset Packs/Mixamo/Animation Library/`.
2. `MixamoAnimationImporter` configures every new FBX there on first import: Humanoid, avatar copied from the ninja (`Kachujin G Rosales.fbx`), clip named after the file, loop guessed from the name.
3. Give the file a short Title Case name before or after dropping it (`Sword Attack 2`, `Bow Aim Walk Left`), unique in the project. Mixamo's original names (`standing melee kick`) are renamed on the way in.
4. Mixamo packs include a copy of the character FBX (about 18 MB each): do not import it, the ninja model already exists.

## Library: `Assets/Asset Packs/Mixamo/Animation Library/`

### Locomotion (25)

idles, walks, runs, strafes, turns, falls: base movement for any humanoid.

| Clip | Length | Plays | File |
|---|---|---|---|
| Fall Land To Idle | 0.70 s | once | `Locomotion/Fall Land To Idle.fbx` |
| Fall Land To Run | 0.93 s | once | `Locomotion/Fall Land To Run.fbx` |
| Fall Loop | 1.07 s | loop | `Locomotion/Fall Loop.fbx` |
| Idle 01 | 5.10 s | loop | `Locomotion/Idle 01.fbx` |
| Idle 02 Looking | 3.20 s | loop | `Locomotion/Idle 02 Looking.fbx` |
| Idle 03 Examine | 5.10 s | loop | `Locomotion/Idle 03 Examine.fbx` |
| Idle Unarmed 01 | 4.87 s | loop | `Locomotion/Idle Unarmed 01.fbx` |
| Run Back | 0.67 s | loop | `Locomotion/Run Back.fbx` |
| Run Forward | 0.87 s | loop | `Locomotion/Run Forward.fbx` |
| Run Forward Stop | 0.87 s | once | `Locomotion/Run Forward Stop.fbx` |
| Run Left | 0.67 s | loop | `Locomotion/Run Left.fbx` |
| Run Right | 0.77 s | loop | `Locomotion/Run Right.fbx` |
| Sword Idle | 2.50 s | loop | `Locomotion/Sword Idle.fbx` |
| Sword Run | 0.70 s | loop | `Locomotion/Sword Run.fbx` |
| Sword Run 2 | 0.53 s | loop | `Locomotion/Sword Run 2.fbx` |
| Sword Strafe | 0.67 s | loop | `Locomotion/Sword Strafe.fbx` |
| Sword Strafe 2 | 0.70 s | loop | `Locomotion/Sword Strafe 2.fbx` |
| Sword Turn | 0.93 s | once | `Locomotion/Sword Turn.fbx` |
| Sword Turn 2 | 0.93 s | once | `Locomotion/Sword Turn 2.fbx` |
| Turn 90 Left | 1.20 s | once | `Locomotion/Turn 90 Left.fbx` |
| Turn 90 Right | 1.10 s | once | `Locomotion/Turn 90 Right.fbx` |
| Walk Back | 1.47 s | loop | `Locomotion/Walk Back.fbx` |
| Walk Forward | 1.20 s | loop | `Locomotion/Walk Forward.fbx` |
| Walk Left | 1.20 s | loop | `Locomotion/Walk Left.fbx` |
| Walk Right | 1.20 s | loop | `Locomotion/Walk Right.fbx` |

### Combat Melee (8)

sword, punch and kick attacks; draw and sheath.

| Clip | Length | Plays | File |
|---|---|---|---|
| Melee Kick | 1.43 s | once | `Combat Melee/Melee Kick.fbx` |
| Melee Punch | 1.00 s | once | `Combat Melee/Melee Punch.fbx` |
| Sword Attack 1 | 2.33 s | once | `Combat Melee/Sword Attack 1.fbx` |
| Sword Attack 2 | 1.30 s | once | `Combat Melee/Sword Attack 2.fbx` |
| Sword Attack 3 | 1.73 s | once | `Combat Melee/Sword Attack 3.fbx` |
| Sword Attack 4 | 1.00 s | once | `Combat Melee/Sword Attack 4.fbx` |
| Sword Draw | 0.50 s | once | `Combat Melee/Sword Draw.fbx` |
| Sword Sheath | 1.27 s | once | `Combat Melee/Sword Sheath.fbx` |

### Combat Ranged (9)

bow aim, draw, recoil, equip: a ready set for a bow skill or character.

| Clip | Length | Plays | File |
|---|---|---|---|
| Bow Aim Overdraw | 3.73 s | once | `Combat Ranged/Bow Aim Overdraw.fbx` |
| Bow Aim Recoil | 0.70 s | once | `Combat Ranged/Bow Aim Recoil.fbx` |
| Bow Aim Walk Back | 1.47 s | loop | `Combat Ranged/Bow Aim Walk Back.fbx` |
| Bow Aim Walk Forward | 1.20 s | loop | `Combat Ranged/Bow Aim Walk Forward.fbx` |
| Bow Aim Walk Left | 1.20 s | loop | `Combat Ranged/Bow Aim Walk Left.fbx` |
| Bow Aim Walk Right | 1.27 s | loop | `Combat Ranged/Bow Aim Walk Right.fbx` |
| Bow Disarm | 1.10 s | once | `Combat Ranged/Bow Disarm.fbx` |
| Bow Draw Arrow | 1.00 s | once | `Combat Ranged/Bow Draw Arrow.fbx` |
| Bow Equip | 0.90 s | once | `Combat Ranged/Bow Equip.fbx` |

### Defense and Reactions (9)

blocks, hit reactions, deaths.

| Clip | Length | Plays | File |
|---|---|---|---|
| Block | 1.93 s | once | `Defense and Reactions/Block.fbx` |
| Death Backward | 3.07 s | once | `Defense and Reactions/Death Backward.fbx` |
| Death Forward | 3.17 s | once | `Defense and Reactions/Death Forward.fbx` |
| Hit React Front | 1.27 s | once | `Defense and Reactions/Hit React Front.fbx` |
| Hit React Headshot | 0.80 s | once | `Defense and Reactions/Hit React Headshot.fbx` |
| Sword Block | 0.47 s | once | `Defense and Reactions/Sword Block.fbx` |
| Sword Block 2 | 0.50 s | once | `Defense and Reactions/Sword Block 2.fbx` |
| Sword Block Idle | 1.37 s | loop | `Defense and Reactions/Sword Block Idle.fbx` |
| Sword Death | 2.30 s | once | `Defense and Reactions/Sword Death.fbx` |

### Evade (5)

dodges in four directions and a dive.

| Clip | Length | Plays | File |
|---|---|---|---|
| Dive Forward | 1.63 s | once | `Evade/Dive Forward.fbx` |
| Dodge Backward | 1.63 s | once | `Evade/Dodge Backward.fbx` |
| Dodge Forward | 1.00 s | once | `Evade/Dodge Forward.fbx` |
| Dodge Left | 0.97 s | once | `Evade/Dodge Left.fbx` |
| Dodge Right | 0.97 s | once | `Evade/Dodge Right.fbx` |

### Gestures (15)

head nods, shrugs, emotes: for intros, taunts, dialogue.

| Clip | Length | Plays | File |
|---|---|---|---|
| Gesture Acknowledging | 1.57 s | once | `Gestures/Gesture Acknowledging.fbx` |
| Gesture Angry | 2.17 s | once | `Gestures/Gesture Angry.fbx` |
| Gesture Annoyed Head Shake | 2.23 s | once | `Gestures/Gesture Annoyed Head Shake.fbx` |
| Gesture Being Cocky | 2.87 s | once | `Gestures/Gesture Being Cocky.fbx` |
| Gesture Dismissing | 3.27 s | once | `Gestures/Gesture Dismissing.fbx` |
| Gesture Happy Hand | 2.73 s | once | `Gestures/Gesture Happy Hand.fbx` |
| Gesture Hard Head Nod | 1.63 s | once | `Gestures/Gesture Hard Head Nod.fbx` |
| Gesture Head Nod Yes | 2.50 s | once | `Gestures/Gesture Head Nod Yes.fbx` |
| Gesture Lengthy Head Nod | 1.73 s | once | `Gestures/Gesture Lengthy Head Nod.fbx` |
| Gesture Look Away | 1.87 s | once | `Gestures/Gesture Look Away.fbx` |
| Gesture Relieved Sigh | 3.00 s | once | `Gestures/Gesture Relieved Sigh.fbx` |
| Gesture Sarcastic Head Nod | 2.00 s | once | `Gestures/Gesture Sarcastic Head Nod.fbx` |
| Gesture Shaking Head No | 1.80 s | once | `Gestures/Gesture Shaking Head No.fbx` |
| Gesture Thoughtful Head Shake | 2.97 s | once | `Gestures/Gesture Thoughtful Head Shake.fbx` |
| Gesture Weight Shift | 9.43 s | once | `Gestures/Gesture Weight Shift.fbx` |

## Older clips: ninja character folder (26)

Used by the ninja's Animator through extracted `.anim` copies in `Assets/Animations/Ninja/`; the FBX sources are here. Clips named `mixamo.com` are identified by file name.

| File | Clip | Length | Plays | Rig |
|---|---|---|---|---|
| `Mixamo/Ninja Kachujin/Frisbee Throw.fbx` | mixamo.com | 0.80 s | once | Human |
| `Mixamo/Ninja Kachujin/Kachujin G Rosales.fbx` | mixamo.com | 0.03 s | once | Human |
| `Mixamo/Ninja Kachujin/Kachujin G Rosales@Clapping.fbx` | Clapping | 1.10 s | loop | Human |
| `Mixamo/Ninja Kachujin/Kachujin G Rosales@Death.fbx` | Ninja Death | 3.60 s | once | Human |
| `Mixamo/Ninja Kachujin/Kachujin G Rosales@Dodge.fbx` | Ninja Dodge | 2.15 s | once | Human |
| `Mixamo/Ninja Kachujin/Kachujin G Rosales@Female Standing Pose.fbx` | Female Standing Pose | 0.02 s | once | Human |
| `Mixamo/Ninja Kachujin/Kachujin G Rosales@Flip Kick.fbx` | Flip Kick | 2.80 s | once | Human |
| `Mixamo/Ninja Kachujin/Kachujin G Rosales@Melee Combo.fbx` | Ninja Melee Combo | 4.20 s | once | Human |
| `Mixamo/Ninja Kachujin/Kachujin G Rosales@Ninja Idle.fbx` | Ninja Idle | 3.33 s | loop | Human |
| `Mixamo/Ninja Kachujin/Kachujin G Rosales@Pain Gesture.fbx` | Pain Gesture | 1.77 s | once | Human |
| `Mixamo/Ninja Kachujin/Kachujin G Rosales@Run.fbx` | Run | 0.63 s | loop | Human |
| `Mixamo/Ninja Kachujin/Kachujin G Rosales@Wide Arm Spell Casting.fbx` | Wide Arm Spell Casting | 3.53 s | once | Human |
| `Mixamo/Ninja Kachujin/Martelo 2.fbx` | mixamo.com | 1.30 s | once | Human |
| `Mixamo/Ninja Kachujin/Standing 1H Magic Attack 01.fbx` | mixamo.com | 2.30 s | once | Human |
| `Mixamo/Ninja Kachujin/Standing 1H Magic Attack 02.fbx` | mixamo.com | 2.20 s | once | Human |
| `Mixamo/Ninja Kachujin/Standing 1H Magic Attack 03.fbx` | mixamo.com | 2.30 s | once | Human |
| `Mixamo/Ninja Kachujin/Standing 2H Cast Spell 01.fbx` | mixamo.com | 2.17 s | once | Human |
| `Mixamo/Ninja Kachujin/Standing 2H Magic Area Attack 01.fbx` | mixamo.com | 2.97 s | once | Human |
| `Mixamo/Ninja Kachujin/Standing 2H Magic Area Attack 02.fbx` | mixamo.com | 3.13 s | once | Human |
| `Mixamo/Ninja Kachujin/Standing 2H Magic Attack 01.fbx` | mixamo.com | 2.67 s | once | Human |
| `Mixamo/Ninja Kachujin/Standing 2H Magic Attack 02.fbx` | mixamo.com | 2.63 s | once | Human |
| `Mixamo/Ninja Kachujin/Standing 2H Magic Attack 03.fbx` | mixamo.com | 4.27 s | once | Human |
| `Mixamo/Ninja Kachujin/Standing 2H Magic Attack 04.fbx` | mixamo.com | 3.33 s | once | Human |
| `Mixamo/Ninja Kachujin/Standing 2H Magic Attack 05.fbx` | mixamo.com | 3.43 s | once | Human |
| `Mixamo/Ninja Kachujin/standing 1H cast spell 01.fbx` | mixamo.com | 2.00 s | once | Human |
| `Mixamo/Ninja Kachujin/standing idle.fbx` | mixamo.com | 1.80 s | once | Human |

## Older clips: Maw (first boss) folder (24)

The first boss's clips. Most are Generic on the Maw rig, so they work on the Maw as is; Humanoid ones (and any clip after setting it to Humanoid) work on other characters, for example the ninja's Dodge clip is a copy of the Maw Backflip.

| File | Clip | Length | Plays | Rig |
|---|---|---|---|---|
| `Mixamo/Maw J Laygo/Maw J Laygo.fbx` | mixamo.com | 0.03 s | once | Human |
| `Mixamo/Maw J Laygo/Maw J Laygo@Agony.fbx` | Agony | 3.70 s | once | Generic |
| `Mixamo/Maw J Laygo/Maw J Laygo@Backflip.fbx` | Backflip | 2.15 s | once | Generic |
| `Mixamo/Maw J Laygo/Maw J Laygo@Drunk Idle Variation.fbx` | Drunk Idle Variation | 5.38 s | once | Generic |
| `Mixamo/Maw J Laygo/Maw J Laygo@Drunk Run Forward.fbx` | Drunk Run Forward | 1.83 s | once | Generic |
| `Mixamo/Maw J Laygo/Maw J Laygo@Fast Run.fbx` | Fast Run | 0.52 s | once | Generic |
| `Mixamo/Maw J Laygo/Maw J Laygo@Idle.fbx` | Idle | 10.00 s | once | Generic |
| `Mixamo/Maw J Laygo/Maw J Laygo@Magic Heal.fbx` | Magic Heal | 2.67 s | once | Generic |
| `Mixamo/Maw J Laygo/Maw J Laygo@Magic Spell Casting.fbx` | Magic Spell Casting | 4.27 s | once | Generic |
| `Mixamo/Maw J Laygo/Maw J Laygo@Mutant Jump Attack.fbx` | Mutant Jump Attack | 3.70 s | once | Generic |
| `Mixamo/Maw J Laygo/Maw J Laygo@Mutant Swiping.fbx` | Mutant Swiping | 2.67 s | once | Generic |
| `Mixamo/Maw J Laygo/Maw J Laygo@Spin In Place.fbx` | Spin In Place | 4.00 s | once | Generic |
| `Mixamo/Maw J Laygo/Maw J Laygo@Standing 1H Magic Attack 03.fbx` | Standing 1H Magic Attack 03 | 2.28 s | once | Generic |
| `Mixamo/Maw J Laygo/Maw J Laygo@Standing 2H Magic Area Attack 01.fbx` | Standing 2H Magic Area Attack 01 | 2.95 s | once | Generic |
| `Mixamo/Maw J Laygo/Maw J Laygo@Standing 2H Magic Area Attack 02.fbx` | Standing 2H Magic Area Attack 02 | 3.25 s | once | Generic |
| `Mixamo/Maw J Laygo/Maw J Laygo@Standing 2H Magic Attack 05.fbx` | Standing 2H Magic Attack 05 | 3.53 s | once | Generic |
| `Mixamo/Maw J Laygo/Maw J Laygo@Standing Melee Combo Attack Ver. 2.fbx` | Standing Melee Combo Attack Ver. 2 | 4.20 s | once | Generic |
| `Mixamo/Maw J Laygo/Maw J Laygo@Standing Yell.fbx` | Standing Yell | 4.07 s | once | Generic |
| `Mixamo/Maw J Laygo/Maw J Laygo@Stomp.fbx` | Stomp | 1.87 s | once | Generic |
| `Mixamo/Maw J Laygo/Maw J Laygo@Tut Hip Hop Dance.fbx` | Tut Hip Hop Dance | 12.08 s | once | Generic |
| `Mixamo/Maw J Laygo/Maw J Laygo@Walking.fbx` | Walking | 1.37 s | once | Human |
| `Mixamo/Maw J Laygo/Maw J Laygo@Warming Up.fbx` | Warming Up | 6.28 s | once | Generic |
| `Mixamo/Maw J Laygo/Mutant Flexing Muscles.fbx` | mixamo.com | 2.80 s | once | Generic |
| `Mixamo/Maw J Laygo/Zombie Attack.fbx` | mixamo.com | 3.23 s | once | Generic |

## Second boss pack (wizard) (56)

Magic casts, crouches, jumps, blocks, hit reactions and deaths, used by the second boss. Rows marked **Generic** were imported as Generic, so before using one on another character set the FBX to Humanoid with the avatar copied from `Kachujin G Rosales.fbx` (do not change clips the wizard's Animator already uses). Clips named `mixamo.com` are identified by file name.

| File | Clip | Length | Plays | Rig |
|---|---|---|---|---|
| `Second Boss/Animations/Crouch Idle.fbx` | mixamo.com | 1.37 s | once | Generic |
| `Second Boss/Animations/Crouch To Standing Idle.fbx` | mixamo.com | 1.00 s | once | Generic |
| `Second Boss/Animations/Crouch Turn Left 90.fbx` | mixamo.com | 1.80 s | once | Generic |
| `Second Boss/Animations/Crouch Turn Right 90.fbx` | mixamo.com | 1.60 s | once | Generic |
| `Second Boss/Animations/Crouch Walk Back.fbx` | mixamo.com | 1.20 s | once | Generic |
| `Second Boss/Animations/Crouch Walk Forward.fbx` | mixamo.com | 1.13 s | once | Generic |
| `Second Boss/Animations/Crouch Walk Left.fbx` | mixamo.com | 1.13 s | once | Generic |
| `Second Boss/Animations/Crouch Walk Right.fbx` | mixamo.com | 1.27 s | once | Generic |
| `Second Boss/Animations/Standing 1H Magic Attack 01.fbx` | mixamo.com | 2.30 s | once | Human |
| `Second Boss/Animations/Standing 1H Magic Attack 02.fbx` | mixamo.com | 2.20 s | once | Human |
| `Second Boss/Animations/Standing 1H Magic Attack 03.fbx` | mixamo.com | 2.30 s | once | Human |
| `Second Boss/Animations/Standing 2H Cast Spell 01.fbx` | mixamo.com | 2.17 s | once | Human |
| `Second Boss/Animations/Standing 2H Magic Area Attack 01.fbx` | mixamo.com | 2.97 s | once | Human |
| `Second Boss/Animations/Standing 2H Magic Area Attack 02.fbx` | mixamo.com | 3.03 s | once | Human |
| `Second Boss/Animations/Standing 2H Magic Attack 01.fbx` | mixamo.com | 2.67 s | once | Generic |
| `Second Boss/Animations/Standing 2H Magic Attack 02.fbx` | mixamo.com | 2.63 s | once | Generic |
| `Second Boss/Animations/Standing 2H Magic Attack 03.fbx` | mixamo.com | 4.27 s | once | Generic |
| `Second Boss/Animations/Standing 2H Magic Attack 04.fbx` | mixamo.com | 3.33 s | once | Generic |
| `Second Boss/Animations/Standing 2H Magic Attack 05.fbx` | mixamo.com | 3.53 s | once | Human |
| `Second Boss/Animations/Standing Block End.fbx` | mixamo.com | 1.20 s | once | Generic |
| `Second Boss/Animations/Standing Block Idle.fbx` | mixamo.com | 2.77 s | loop | Human |
| `Second Boss/Animations/Standing Block React Large.fbx` | mixamo.com | 1.23 s | once | Generic |
| `Second Boss/Animations/Standing Block Start.fbx` | mixamo.com | 0.50 s | once | Human |
| `Second Boss/Animations/Standing Idle 03.fbx` | mixamo.com | 11.40 s | once | Generic |
| `Second Boss/Animations/Standing Idle 04.fbx` | mixamo.com | 7.47 s | once | Generic |
| `Second Boss/Animations/Standing Idle To Crouch.fbx` | mixamo.com | 0.97 s | once | Generic |
| `Second Boss/Animations/Standing Jump Running Landing.fbx` | mixamo.com | 1.37 s | once | Generic |
| `Second Boss/Animations/Standing Jump Running.fbx` | mixamo.com | 1.07 s | once | Generic |
| `Second Boss/Animations/Standing Jump.fbx` | mixamo.com | 2.33 s | once | Generic |
| `Second Boss/Animations/Standing Land To Standing Idle.fbx` | mixamo.com | 1.07 s | once | Generic |
| `Second Boss/Animations/Standing React Death Backward.fbx` | mixamo.com | 3.60 s | once | Generic |
| `Second Boss/Animations/Standing React Death Forward.fbx` | mixamo.com | 3.47 s | once | Generic |
| `Second Boss/Animations/Standing React Death Left.fbx` | mixamo.com | 3.43 s | once | Generic |
| `Second Boss/Animations/Standing React Death Right.fbx` | mixamo.com | 3.50 s | once | Generic |
| `Second Boss/Animations/Standing React Large From Back.fbx` | mixamo.com | 1.67 s | once | Generic |
| `Second Boss/Animations/Standing React Large From Front.fbx` | mixamo.com | 1.37 s | once | Generic |
| `Second Boss/Animations/Standing React Large From Left.fbx` | mixamo.com | 1.40 s | once | Generic |
| `Second Boss/Animations/Standing React Large From Right.fbx` | mixamo.com | 1.63 s | once | Generic |
| `Second Boss/Animations/Standing React Small From Back.fbx` | mixamo.com | 1.27 s | once | Generic |
| `Second Boss/Animations/Standing React Small From Front.fbx` | mixamo.com | 0.80 s | once | Generic |
| `Second Boss/Animations/Standing React Small From Left.fbx` | mixamo.com | 1.20 s | once | Generic |
| `Second Boss/Animations/Standing React Small From Right.fbx` | mixamo.com | 0.97 s | once | Generic |
| `Second Boss/Animations/Standing Run Back.fbx` | mixamo.com | 0.63 s | once | Generic |
| `Second Boss/Animations/Standing Run Forward.fbx` | mixamo.com | 0.73 s | once | Generic |
| `Second Boss/Animations/Standing Run Left.fbx` | mixamo.com | 0.77 s | once | Generic |
| `Second Boss/Animations/Standing Run Right.fbx` | mixamo.com | 0.77 s | once | Generic |
| `Second Boss/Animations/Standing Sprint Forward.fbx` | mixamo.com | 0.57 s | once | Generic |
| `Second Boss/Animations/Standing Turn Left 90.fbx` | mixamo.com | 1.60 s | once | Generic |
| `Second Boss/Animations/Standing Turn Right 90.fbx` | mixamo.com | 1.63 s | once | Generic |
| `Second Boss/Animations/Standing Walk Back.fbx` | mixamo.com | 1.20 s | once | Generic |
| `Second Boss/Animations/Standing Walk Forward.fbx` | mixamo.com | 1.13 s | once | Generic |
| `Second Boss/Animations/Standing Walk Left.fbx` | mixamo.com | 1.17 s | once | Generic |
| `Second Boss/Animations/Standing Walk Right.fbx` | mixamo.com | 1.20 s | once | Generic |
| `Second Boss/Animations/standing 1H cast spell 01.fbx` | mixamo.com | 1.97 s | once | Human |
| `Second Boss/Animations/standing idle 02.fbx` | mixamo.com | 5.20 s | once | Generic |
| `Second Boss/Animations/standing idle.fbx` | mixamo.com | 1.80 s | loop | Human |

