# Axeman (second player character)

A fast, aggressive melee fighter built on animation cancels, inspired by Riven (League of Legends) and combat games. Model: Mixamo `Brute` (`Assets/Asset Packs/Axeman/Model/Brute.fbx`, Humanoid, textures and materials extracted next to it). Animations: the Pro Melee Axe Pack, in the shared library as `Axe ...` / `Unarmed ...` clips (see `docs/animation-library.md`). Prefab: `Assets/Prefabs/Axeman/Axeman.prefab` (root scale 1.1, so he reads about as big as the ninja). Code: `Scripts/Characters/Axeman/` (`AxemanController`, `AxeMove`, `ThrownAxe`) and `Scripts/UI/SmashPrompt.cs`. Animator: `Assets/Animations/Axeman/Axeman Controller.controller`.

Everything below is a first pass; the numbers are meant to be tuned in the Inspector after playtesting.

## Controls

| Input | Move | What it does |
|---|---|---|
| Right click | move | NavMesh move, like the ninja. Clicking during a recovery cancels it. |
| Left click (hold to chain) | Basic attack | 3-hit chain: horizontal sweep (right axe), backhand sweep (**left axe**, a mirrored clip), overhead chop (heavier). **The character does not move.** |
| Q | Second Wind (heal) | Button smashing: press Q, then mash Q 10 times within 4 s. A small prompt above the skill bar shows the progress. He braces into the `Axe Taunt Battlecry` and loops only its gather (0.05 s to 0.45 s of the clip: the bend in and the rise, before the head goes back, so no roar is shown while mashing); every press pumps the loop (a faster loop, 1x at the start up to 2.2x, plus a short boost per press), and the tenth press plays the scream out (from 0.45 s, release at 1.55 s) and heals 1 heart when the scream releases (8 s cooldown, which also starts if it fails). Taking a hit, running out of time or dashing cancels it. |
| W | Rushing Cleave | Three casts within 3 s, each a long lunge (5.5 to 6 m, stopping in front of a boss) and a swing; the third is a wide 360 degree spin. The icon pulses while the chain is open; the cooldown starts when the chain ends. |
| F | Hammer Kick | Lunge + front kick, damages **and triggers the boss counter window** (F is the counter key of every character, `PlayerController.CounterKey`; the counter prompt shows it). Short invulnerability while lunging. |
| R | Execution Leap | One press, no roar and no rage any more: a big jump to the cursor (3 to 11.44 m, over the boss if needed, landing right in front of it) and a ground slam for 4 damage in a 4.5 m circle. The landing bursts a ring across the floor as wide as that circle (`Shared/GroundSplash`), so the area it hit is visible. Pack hit effects (Stones hit, Star hit) loop, so `PlayOnce` in `AxemanController` turns looping off on every spawned effect: the landing shows one burst and one ring. It rises with the distance (`Arc Height Factor` 0.6: a full-range leap peaks at about 3.4 m, which was measured). 12 s cooldown, needs the axe in hand. |
| E | Shield Dodge | A quick strong escape: one of the library's Evade clips (Dive Forward, Dodge Left, Dodge Right or Dodge Backward) chosen by where the cursor is relative to the way he faces (within 50 degrees: a forward dive roll, he turns to the cursor first; 50 to 130: sidestep right or left; beyond 130: back step; the sides and back keep his facing and slide toward the cursor). 5.2 m along the cursor direction using the clip's own motion, stops in front of an enemy, with a shield bubble around him for 0.5 s (he cannot be hurt while it lasts; the ninja's `DodgeBubble` prefab). Can be cancelled after about 0.75 clip seconds into an attack or another skill, like Riven's E. 7 s cooldown. Works without the axe. |
| D | Axe Throw | He holds **two axes** (one in each hand) and throws both like Beastmaster's Wild Axes: the axes fan out to two landing points either side of the point under the cursor (4 to 9 m away, 3.6 m apart at full range), come back converging on him (0.9 s at full range, less when closer, never under 0.5 s). Each axe hurts a target only once (4 damage), so something standing between the two landing points is hit by both. 8 s cooldown. **While the axes are away he cannot use anything that needs them** (basic attack, W, R, D again), until both are back; the heal, the kick, the shield charge and the dash still work. D is a temporary key. |
| Space | Dash (escape jump) | A real forward jump along the cursor direction: he crouches, takes off, flies with the legs tucked and lands in a crouch (`Unarmed Jump Forward`, which works on the Brute's avatar). About 6 m and 0.75 m of height (measured), about 0.66 s long. **No invulnerability**: it is worth using for the distance and for cancelling an animation, not for dodging through a hit. Cancels anything, even a wind-up; goes through enemies and lands in front of one that is in the way. It replaced a dive roll, which read as sliding forward rather than jumping. |

Health: 3 hearts and 2.2 s of hit immunity, same as the ninja. Running uses the generic `Run Forward` clip (a real sprint, like the ninja's) at agent speed 6. Damage is in the ninja's range: basic hits 2, 2 and 2, kick 2, thrown axe 2 per pass, spin 4, leap 4. Those are the base numbers: the hidden chain bonus (+10% per step, up to 5) and a crit still raise what a hit shows, so a chained basic can read 3.

## How the combat system works

Every attack and skill is an `AxeMove` (serialized on the controller, one field per move). Times are **seconds of the animation clip**, so changing `Speed` never breaks them:

- `ClipStart`: where in the clip the move begins. This skips the clip's slow wind-up; the cross fade (0.08 s) covers the jump from the idle pose.
- `HitTime`: the strike. Damage is applied here (code timed, no animation events: the clips are read-only FBX). Chosen where the axe head crosses the front of the body at full speed.
- `CancelTime`: from here the next input may cut the rest of the animation. Also unlocks move clicks.
- `EndTime`: the move ends by itself (cross fade back to Idle or Run).
- `Speed`: animation speed.
- `YawToHit`: how many degrees the clip turns the body between `ClipStart` and `HitTime` (measured from the clip).
- Strike shape: a sphere of `Radius` at `HitForwardOffset` along the aim direction, filtered by `Arc` (360 = all around). Each target is hit once per move.
- Movement: a code lunge (`LungeDistance`, `LungeEndTime`, `LungeToMouse`, `StopsAtEnemy`) for moves whose clip stands still, or `UsesClipMotion` + `ClipTravel` for the dash and the jump, which play the clip's own travel and height scaled to `LungeDistance` (so the feet match the movement). Calibrate `ClipTravel` by measuring the distance actually covered (the path is longer than the straight line, about 20% for these clips). A clip-motion move never ends inside a boss: the landing is pulled back until it is 0.9 m clear of any enemy collider.
- `TrailStartTime` / `TrailEndTime`: when the blade trail shows (a `TrailRenderer` on the axe head, so it follows the real swing).

**The clips turn the whole body** (the horizontal sweep turns about 175 degrees, the spins 190 to 300). The Animator's root rotation is applied in `OnAnimatorMove` (only while a move plays; locomotion and reactions stay in place), and at the start of each aimed move the difference between where the clip would strike and the cursor direction is spread over the wind-up (`YawToHit`), so the strike lands on the cursor. After a move ends the character turns the rest of the way to face the cursor. Strike checks use the aim direction captured at the start of the move, not `transform.forward`.

Animation cancel and feel:

1. **Cancel window:** after `CancelTime` any attack, skill, the dash, or a move click cuts the animation. The dash can cancel at any time.
2. **Input buffer:** a key pressed up to 0.35 s early is remembered and fires the moment the cancel window opens (`_inputBuffer`). Holding left click keeps asking for the next hit.
3. **Chain bonus (hidden):** a move started by cancelling the previous one (or within 0.4 s of it ending) adds +10% damage per step, up to 5 steps (`_chainDamageBonus`; set it to 0 to remove it). There is no counter on screen any more. Getting hit resets it.
4. **Skills prime the finisher:** after any skill, the next basic attack is the overhead chop (`_skillsPrimeFinisher`).
5. **Skills keep the Rushing Cleave alive:** E and R refresh its window, so W can be woven between them.
6. **Hit stop:** 0.05 s freeze of the animation on a landed hit; the chop, the spin and the jump also punch the camera zoom. A purple star spark appears on the target at the point of contact.

The animator has Idle, Run, Hurt, Death and one state per move clip; code cross-fades into a move state, so there are no transition graphs to maintain.

## Where to tune

All on the `AxemanController` component of the prefab (scene instances inherit). Per move: the `[Header]` groups (`Basic Attack`, `Skill Q`, ...). Global: `Axeman` and `Chain` headers. The heal's presses, time limit, amount and cooldown are under `Skill Q`. Dash cooldown is the base class's `Dash CD`. **Editing a default in the code does not change the prefab** (serialized values win): reset a move on the prefab (right-click the component, or copy the fresh values with a script) after changing a default.

Clip data (clip time in seconds; strike = axe head crosses the front of the body; measured with a preview render of the Brute and sampling the clip, see "Method"):

| Move | Clip | Start | Strike | Cancel | End | Body turn to strike |
|---|---|---|---|---|---|---|
| Basic 1, Rush 1 | Axe Slash Horizontal | 0.35 | 1.01 | 1.2 / 1.12 | 1.5 / 1.4 | -174 |
| Basic 2 | Axe Combo 3 Mirrored (its second swing, left axe) | 1.45 | 1.75 | 1.95 | 2.15 | -146 |
| Rush 2 | Axe Combo 3 (its second swing) | 1.45 | 1.75 | 1.9 | 2.1 | +146 |
| Basic 3 | Axe Slash Downward | 0.30 | 0.93 | 1.05 | 1.4 | -127 |
| Rush 3 | Axe Spin High | 0.55 | 1.05 | 1.3 | 1.95 | +213 |
| Axe Throw (release at the strike) | Axe Spin Low | 0.40 | 0.94 | 1.15 | 1.85 | +187 |
| Kick | Axe Kick 1 | 0.20 | 0.57 | 0.75 | 1.05 | -21 |
| Leap (R) | Axe Leap Slam | 0.40 | 1.68 | 1.85 | 2.4 | -7 (travels 4.8 m, rises 1.9 m; scaled to the distance, see `Arc Height Factor`) |
| Dash (Space) | Unarmed Jump Forward | 0.15 | none | 1.15 | 1.4 | none (clip path 3.2 m between Clip Start and Lunge End Time, scaled to 6 m; the clip rises 0.56 m, kept at `Arc Height Scale` 0.85 with `Arc Height Factor` 0, so the height does not grow with the distance) |
| Run | Run Forward (library clip, generic) | - | - | - | - | state speed 1.35, about 4.45 m/s of travel per second of clip |
| Shield Dodge (E) | `Dive Forward` / `Dodge Left` / `Dodge Right` / `Dodge Backward` (states `Shield Charge`, `Shield Dodge Left`, `Shield Dodge Right`, `Shield Dodge Back`, no transitions) | 0.1 (roll: 0.05) | none | 0.75 (roll: 1.0) | 1.0 (roll: 1.3) | clip motion scaled to 5.2 m (`Clip Travel` 2.94 back, 2.6 sides, 4.59 for the roll), speed 1.7 (roll: 2.2) |
| Heal | Axe Taunt Battlecry (wind-up, then a loop of 0.7 to 1.4 at speed 1 to 2.2 while mashing; the final scream from 0.8 at speed 1.1) | 0.8 | 1.55 | 2.2 | 2.7 | none |

Clips not used yet that could become skills: `Axe Combo 1` / `Combo 2` (long multi-hit flurries), `Axe Slash Backhand` (a 270 degree pirouette, low to overhead), `Axe Kick 2`, `Axe Block Idle` / `Axe Block React` (a guard or parry), `Axe Taunt Chest Thump`, `Axe Jump`, the `Axe Equip/Disarm` pair, `Dive Forward` (a dive roll), and all `Unarmed` clips.

## The thrown axe

`ThrownAxe` (prefab `Prefabs/Axeman/ThrownAxe.prefab`) is a standalone copy of the axe: the held axe's mesh was baked into `ThrownAxe.asset`, rotated so its handle lies along X and its blade is flat, so spinning it around Y looks like a boomerang. At the strike of the throw the held axe is hidden (`_heldAxe`) and the projectile is spawned at the hand. Its path is two quadratic curves, out to the far point bowing one way and back bowing the other, ending at the owner's current chest (so moving does not strand it); the way out is eased out and the way back eased in. It hurts each target once per pass (the set is cleared at the turn), plays the impact spark, and calls `OnAxeCaught` on arrival, which shows the held axe again and unlocks the axe attacks. If the owner dies the projectile disappears. Tune it with `Axe Throw Range`, `Curve` (how far it bows), `Duration` and `Damage` under `Skill F`. To rebuild the mesh after changing the model, bake the `BattleAxe_GEO` renderer in an idle pose and re-orient it (see the method below; the head cluster gives the handle direction, the head's flat plane the blade normal).

## Character selection

Order is character first, then boss:

- `StartScene`: `UI/CharacterSelectMenu` shows two character cards (render of the model in a frozen idle pose, same preview-rig technique as the boss cards; rig `CharacterPreviews` far below the scene, one camera and render texture per card, assets in `Assets/UI/CharacterSelect/`). Picking one remembers it and swaps to the boss cards, with a "Playing as ..." label and a Change Character button. Coming back from a fight (Boss Selection button) goes straight to the boss cards.
- `Characters/CharacterSelection` (static, like `BossSelection`) carries the choice into the fight scene; the game over Restart re-selects the same character.
- Each fight scene (`TestScene`, `VexaraScene`, `UlvaneScene`) has a `CharacterSelector` object. Entry 0 is the character the scene was built with (the ninja); the others are placed at its spawn point. It deactivates the unchosen character and its HUD (the ninja's `Spell Icon List`), points `GameManager.player` and the `CameraController` at the chosen one, and repoints Gorath's particle triggers. The chosen character is switched on in `Start`, not `Awake`: an agent activated before the NavMesh is registered fails to be created. Pressing Play in a fight scene uses the selector's `Starting Character Index` (0 = ninja).
- The Axeman brings its own HUD: a `Skill Bar` canvas with six `SpellIcon`s (placeholder art reused from the ninja) and the heal's `SmashPrompt` (scaled to 0.5, just above the bar). At startup it **detaches the canvas from the character** (a camera canvas sizes itself as if its parent had scale 1, so under the scaled character it drew 10% too large and fell off the screen) and takes the scene's camera. Everything on that canvas needs the same local Z (430) as the icons, or it sits in front of the camera's near plane and is invisible.
- To add a third character: put its prefab (inactive) in each fight scene, add an entry to every `CharacterSelector`, add a card and a preview model/camera/render texture in `StartScene` and an entry in `CharacterSelectMenu` (character index = selector index).

## Method (how the timings were found)

Reusable if you add moves from another Mixamo clip: instantiate the model in an Editor preview scene (`PreviewRenderUtility`), sample the clip with `AnimationClip.SampleAnimation` (this also moves the root, so the root yaw and position per time are the clip's root motion), render a contact sheet of frames to look at, and track the axe head (the vertices of the axe mesh far from its middle; the axe is bound to the right hand only, head at about (-0.89, 0.47, 0.03) in that bone's space) for its angle relative to the body, its height and its speed. The strike is the time its angle crosses 0 at high speed.

Two things that cost time:
- `Animator.CrossFadeInFixedTime`'s offset is in real seconds, so at animation speed 2 an offset of 0.35 started the clip at 0.70. Divide the offset by the speed.
- With `applyRootMotion` off, Humanoid clips lose their body turns completely: the arms swing and the body stays put.

## Verified and not verified

Verified in the Editor with scripted inputs (a temporary probe, removed afterwards) and screenshots, in `TestScene`:

- The strike lands within 2 to 20 degrees of the cursor direction for the sweeps; basic attacks move the character 0.00 m; the animator's clip time matches the move timer to 0.02 s.
- Added in the second pass: the axe throw (out, curve, two hits, catch, 1.6 s), the lock (basic and W ignored while the axe is away, kick and dash fire, basics work again after the catch), the jump covering 10.6 m and landing against a boss (third pass: 10.95 m with the 11.44 setting, peak height 3.4 m), the dash covering 8 m, W casts covering 5.5 to 6 m each, the new run, damage numbers of 2 to 4 (6 with rage on the jump), the smaller prompt.
- The first pass:  basic chain with buffered inputs and chain damage, W 1-2-3, E kick (front kick on Gorath's leg, counter code path not exercised against a real window), R then R (rage, jump with 1.9 m of height, slam), F spin, dash (5.0 m with a hop), the heal (10 presses restores a heart, the prompt shows progress and the timer, the failed cast starts the cooldown), the blade trail, hit stop, impact sparks, death and Restart keeping the character.
- Gorath's melee particle attack hurts the Axeman, same as the ninja. The ninja path loads unchanged (HUD, camera, 0 console errors).
- The character screen, the boss screen, and `VexaraScene` as the Axeman (earlier pass, before the animation rework).

Not verified: the real mouse and keyboard (all aiming went through `DebugAimPoint`, all keys through `DebugPress`), the feel (hit stop, speeds, cancel times, damage numbers, how long the heal takes), the E counter against a real counter window, `UlvaneScene` and `VexaraScene` after the animation rework, and foot sliding while running (agent 5 m/s against a run clip of about 2.3 m/s at 2.1x).

## Debug

`AxemanController.DebugPress("Basic" | "Q" | "W" | "E" | "R" | "F" | "Dash" | "Smash")` (the letters are the real keys: E is the axe throw, F the kick) and `DebugAimPoint` (editor and development builds) let the CLI drive the Axeman with `eval`; set `DebugAimPoint` before pressing, since the CLI has no mouse. When testing against Gorath, also stop the boss agent (`ResetPath`, `isStopped`), or its old path keeps shoving the player.

## Ideas

A guard/parry with `Axe Block Idle`, a recall input to call the axe back early, the axe sticking in the ground at the far point before returning, a combo-finisher branch with `Axe Combo 1`, a Riven-style empowered third cast, an execute bonus on low boss health for the leap, a style rank for long chains, real skill icons, a crouch with more presence for the heal (the clip is subtle), a chest-thump or roar effect for the battlecry.

Shield Dodge (E), replaced the run-clip charge on 2026-10-11: the 50 degree body pitch on the run clip pointed his head at the floor, so the Evade clips were used instead (the pitch machinery, `Lean Angle`, stays in `AxeMove` but nothing uses it now). Checked in Play mode through the CLI (DebugPress "E"): the right clip is picked for all four cursor directions (`ChooseShieldDash`), 5.0 to 5.2 m covered each way when nothing blocks it (it stops short in front of a boss: `StopsAtEnemy`), the facing is kept on the sides and back (`KeepsFacing`: no clip rotation applied, the agent does not turn him), the bubble lasts 0.5 s, and frames of the left, back and right dodges looked right. Not checked by hand: the feel, and how the sidesteps read from the real camera. `Clip Travel` of each move was calibrated by measuring: it is the clip's root travel (about 2.0 forward and back, 1.77 sideways) times about 1.47 for the Brute's rig scale, so a new clip can be estimated the same way and then measured in Play mode.

`Arc Height Scale` multiplies a clip's rise (below 1 flattens the jump) and `Arc Height Factor` decides how much the rise follows the scaling of the distance; the dash uses the first and the R leap the second. A field added to `AxeMove` is missing from what the prefab already stored, so write it on every move explicitly after adding one, or the moves silently get 0.

The dash's Clip Travel is not the clip's net displacement: the movement is applied as `|deltaPosition| * scale` along the aim direction every frame, and the jump clip twists the body up to 34 degrees in the air, so the path it walks is longer than the straight line between its first and last frame. Measuring the curve gave 2.29 m but the move really covered 3.31 m of clip path, so Clip Travel is tuned by measuring in Play mode (3.2 gives about 6 m, with 0.2 m of run-to-run variance from frame timing, so measure a few times). Measure a lunge away from the boss: its NavMeshAgent pushes the player while they overlap and a first measurement read 9.3 m with a metre of sideways drift because of it.

`Shared/GroundSplash` is the ring a heavy landing leaves on the floor: `GroundSplash.Show(centre, radius, colour, time)` builds it in code (a generated ring sprite laid flat, growing with an ease-out and fading), so nothing is wired in a scene and no custom shader can go missing from a build. A move asks for one with `Splash Time` above 0 and `Splash Colour`; it is drawn at the move's strike centre with the move's `Radius`, so the ring always shows the real area. Only the R leap uses it (0.45 s, warm orange).


Basic chain sped up (2026-10-11, later the same day): `Speed` of Basic 1, 2 and 3 is now 1.6 each (tried 1.8 first), and the three basic hits no longer spawn the purple impact particles (`AxeMove.NoImpactEffect`; the other moves and the thrown axe still do). Not run in the Editor (the Unity connection was down): needs a playtest.

Earlier that day, basic chain slowed by 35%: `Speed` of Basic 1, 2 and 3 is now 1.3, 0.975 and 1.3 (was 2, 1.5, 2), so the left-click chain is slower than the W Rushing Cleave, which keeps its speeds (2.3, 1.75, 2) and now feels like a faster reset of the basic attack. Clip times (hit, cancel, end) are in clip seconds and did not change; real time is clip time divided by speed.


Forward roll (2026-10-11): the forward dodge was `Dodge Forward`, which looked like a lame hop, so it is now `Dive Forward` (a dive roll, 1.63 s clip trimmed to 0.05 to 1.3, speed 2.2, about 0.57 s real, shield 0.55 s). Its root height drops to the floor, and the code only applies upward height changes (`ApplyClipMotion`), so with the default import the body floated above its shadow while rolling; the fix is `Bake Into Pose` for Root Transform Position (Y) on that clip (`lockRootHeightY` in the FBX import settings; `MixamoAnimationImporter` only touches new files, so it is kept). Checked in Play mode: 5.1 m covered, the body rolls along the ground, frames mid-roll looked right. Not checked: the feel at 2.2x speed, and the get-up at the end (the move ends at clip time 1.3, before the clip is fully standing: the cancel window opens at 1.0).

Two axes (2026-10-11): the left hand holds a mirrored copy of the held axe (`Held Axe Left` under the left hand bone, mesh `Prefabs/Axeman/HeldAxeLeft.asset`, the right axe's vertices reflected across the body's centre and moved into the left hand's bone space, triangles flipped). It follows whatever the left hand does in the clips, so in the one-handed swings it just swings along. `ThrownAxe.Launch` takes `isLeft`: the left axe bows to the left and spins the other way; `AxemanController.OnAxeCaught(isLeft)` shows each hand's axe again and unlocks the axe moves when `_axesAway` reaches 0. New throw tuning: range 9 (was 11), curve 2.4 (was 3.5), duration 0.9 (was 1.6). Verified in Play (CLI): both axes shown in the hands, both thrown to opposite sides and caught, Basic refused while away and accepted after, 0 console errors. Not checked: how the left axe looks in every clip (a close look at the idle pose was fine), the feel of the throw.

Wild Axes rework (2026-10-11): the throw first curved out to a fixed 9 m and came back on the opposite bow (an X); now it flies to the cursor (`_axeThrowMinRange` 4, `_axeThrowRange` 9, distance read when D is pressed) and returns along the same arc (`ThrownAxe.GetControl`). Checked in Play by sampling the axes' positions in his local frame: the axes peaked at about +-3 m to the sides, met at 7 m for a 7 m aim and came back on the mirrored arcs, the lock released once both were caught, 0 console errors. Not checked: close cursor (min range) and the feel.

Less wavy (2026-10-11): `_axeThrowCurve` 2.4 -> 1.5 and it now scales with the throw distance (`curve * distance / range`), so short throws are not hooks. Sampled in Play: a 7 m throw peaks about 1.8 m to each side.

Dota-style fan (2026-10-11): Beastmaster's Wild Axes (Liquipedia: the axes separate to 450 units apart at the target point and reconverge on the return; each axe hits an enemy once) replaced the arcs. Now each axe flies a nearly straight line (`_axeThrowCurve` 0.5 bow) from its hand to a landing point `_axeThrowSpread` (3.6 m, scaled by distance) apart around the cursor point and back; `ThrownAxe` no longer clears its hit list at the turn, so damage per axe per target is once, and `_axeThrowDamage` went 2 -> 4 to compensate (a target hit by both axes takes 8, as before). Checked in Play by dropping a marker per frame for each axe and viewing it from above: a V opening away from him, both legs on almost the same lines, 0 console errors.

Arcs instead of the V (2026-10-11, last pass): the ribbon on `ThrownAxe` was a child of the spinning mesh, so it drew a spiral (the "wavy" look); the `Trail` object now sits on the root, not spinning. Each leg is a cubic arc bulging outward (`ThrownAxe.Arc`; `_axeThrowCurve` 0.8 = about 145 degrees, 1 = a half circle, 0 = straight), landing points `_axeThrowSpread` 1.2 m apart. Checked in Play (markers per frame seen from above, and the ribbon in the game view): a ( ) pair of arcs meeting at the target, smooth ribbon, 0 console errors. This section supersedes the 'Dota-style fan' note above on the shape.

Hit per pass, longer range (2026-10-11): each axe hits a target once per pass again (`ThrownAxe` clears its hit list at the turn), 4 damage per hit, so a target in the middle takes up to 16 (2 axes x 2 passes). Range 9 -> 12.6 m (+40%), duration 0.9 -> 1.26 s so the axe speed is the same; `_axeThrowMinRange` stays 4. Checked in Play: a throw aimed at the boss (4.5 m away) took 20 health (four hits, one of them a critical for double), 0 console errors. This supersedes the 'once per axe' rule in the Dota-style fan note.

Thinner ( ) (2026-10-11): `_axeThrowCurve` 0.8 -> 0.5 (about 100 degrees per arc instead of 145) and `_axeThrowSpread` 1.2 -> 0.3 m, so the two axes meet almost touching at the target point. Checked in Play with per-frame markers (pairs of markers about 0.3 m apart at the far end, narrower arcs), 0 console errors.

Fixed distance (2026-10-11): the throw always travels `_axeThrowRange` (12.6 m) toward the cursor; the cursor only gives the direction, so a cursor next to him no longer shortens it (it made the 16 damage too easy). `_axeThrowMinRange` and the distance scaling of curve, spread and duration were removed. Checked in Play: cursor 1.5 m away, the axes still peaked at 12.7 and 13.0 m from him, 0 console errors. This supersedes the 4 to 9 m / cursor-distance notes above.

Left axe, Q animation and icons (2026-10-12): the second basic hit now swings the **left** axe. `Axe Combo 3.fbx` has a second clip entry, `Axe Combo 3 Mirrored` (the Humanoid Mirror option; the original clip and its internal ID are untouched, so Rush 2 and the `Axe Combo 3` state still work), with a matching state in the Axeman Controller. Measured in Play: the mirrored clip turns -146 degrees (the original +146) and the left hand is the one that sweeps. `AxeMove.UsesLeftAxe` picks which trail shows: `Blade Trail Left` is a copy of the right trail on the left hand, at the mirrored axe-head position (0.89, 0.47, 0.03). Q: the old crouch idle moved about 1 cm, so it looked frozen; each press now plays a chest thump beat (`_healBeatStarts` / `_healBeatEnds` / `_healBeatSpeed`), the beat ends back in the idle, and the final thump (`_healRise`) applies the heal at its hit time (checked: 2 -> 3 hearts). Icons: W, R, D, F and the Space dash have their own placeholder art in the style of the ninja's kick icon (dark vignette tile, cartoon Brute with a pale rim, speed lines). F (Hammer Kick) is the ninja's kick sprite itself recoloured into the Brute (skin, trousers, boots, braids, beard, an axe in the raised hand): `docs/axeman/make_kick_icon.py`. W, R, D and Space are drawn from joint poses by `docs/axeman/make_cartoon_icons.py` (shapes in `icon_shapes.py`, a tiny PNG reader in `pngio.py`; numpy only; they write `Assets/Placeholders/Spell Icons/Axeman/`, edit a pose and rerun, then reimport the PNG). Q and E keep the ninja-era icons on purpose. Not checked by eye: the left-axe swing and the thump up close (the capture camera is far), so they need a playtest; the icons are first-pass shapes.
