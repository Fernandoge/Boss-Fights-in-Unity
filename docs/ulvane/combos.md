# Ulvane combined animal commands (idea #2)

Two animals work together and one skill feeds the other. Combos are the only Ulvane skills that hit for 2 (the danger icon shows above him); every single skill (arrow, tiger, horse, kitty and the rest) now hits for 1.

## How a combo runs

`UlvaneBoss` drives both beasts step by step: `BeastX.BeginCombo()` takes the beast off its patrol and stops its own skill, the boss calls the beast's public combo methods, and `EndCombo()` gives it back (cooldown restarts as after a normal skill). `Enrage()` or the mourning request also release a held beast, and `OnComboReleased()` cleans up what it built. Forced from the harness with key 9 (`UlvaneAttack.SnowTornado`); nothing picks it on its own yet.

## Snow Tornado (dog + penguin)

Phase 2 only. In phase 2 Ulvane's attack picker chooses it once the cooldown is over (first 3 s after the phase starts, then every 8 to 12 s, `Snow Tornado Combo` fields on the boss) and both animals are free; otherwise he shoots. The harness key 9 forces it in any phase.

1. Ulvane only screams (the roar clip) and goes back to his own attacks. The combo runs beside him as its own coroutine (`SnowTornadoCombo`), and the other animals keep using their own skills. A danger icon shows above the dog and the penguin, not above Ulvane.
2. The dog runs to 5 units left of the penguin (as it looks at the player; the other side if that is outside the arena), spins up a whirlwind and stays there spinning. The whirlwind circle hurts for 2 every 0.7 s.
3. The penguin lobs a harmless snowball at the dog every 0.6 s. Each one that lands joins the balls circling inside the whirlwind.
4. With 5 stacked, and the dog still spinning, the snowballs go one after the other like Gorath's rock shower: every 0.25 s a lane starts on where the player is (perceived position plus a short lead), fills for 0.45 s, and the snowball flies down it at 48 (about 220% of the penguin's 21.84) for 2 damage. After the last one the whirlwind winds down, the dog is dizzy 0.9 s, and both go back to patrolling.

Code: `DogBeast` (`StormStart`, `AddSnowball`, `ThrowStack`), `PenguinBeast.ComboFeed`, `UlvaneBeast.BeginCombo` / `ShowDanger`, `UlvaneProjectile.LaunchAtTarget` / `Hold`, `UlvaneBoss.SnowTornadoSequence` and `SnowTornadoCombo`. Tuning is under `Snow Tornado Combo` on the dog, the penguin and the boss.

Checked in Play mode through the CLI: the combo forced and also picked naturally after the phase 2 transition, the dog running over, the whirlwind with the snowballs circling inside, the stack counting to 5, the lane aimed at the player while the whirlwind spins, the danger icons on both animals only, 0 console errors, scene not dirty. Not checked: the feel and difficulty of speed 48 and the 0.25 s rhythm, whether the 5 hits can be dodged, the combo near the arena edge, the combo being cut by phase 3, and the first throw frame by eye.

**Dog run fix (2026-10-13):** the dog ran to the penguin in his idle pose (sliding). The combo starts the run, then the dog's patrol step notices the command and calls `Idle()` once, which cancelled the `Gallop()` that `RunTo` had set before its loop. `RunTo` now sets `Gallop()` every frame. Checked in Play mode: during the run `Vert` and `State` stay at 1 and the run cycle advances. Not checked by eye at normal speed.

## Sky Fall (deer + tiger + chicken, 2026-10-14)

Phase 2 only, picked at random with the other combos (one combo at a time, shared cooldown `Combos` on the boss, never the same one twice in a row). Harness key 0. Hits for 2. The player cannot counter any of it (the deer's F window is not opened in the combo). Danger icons on all three animals.

1. Ulvane screams. The chicken runs to the tiger, the deer lines up 9 units away (towards the arena centre) facing the pair.
2. The deer lowers its head, lunges and scoops (the same head and sand wave as its own skill, no cone shown); at the scoop the tiger and the chicken are flung up out of the screen (45 units, 0.7 s, tumbling) and hidden. The deer is free again.
3. Eggs rain on the player for about 3.5 s (`ChickenBeast.ComboEggRain`): 16 eggs, one every 0.22 s, each a circle of 1.4 that fills for 0.9 s while the egg falls from 25 units; every third one lands almost where the player is heading, the rest scatter 6 units around that.
4. The chicken glides back down (2.2 s, flapping side to side), landing where it started; it hurts nobody. As it starts down, the tiger's circle appears (`TigerBeast.ComboSlam`): radius 14 (it covers most of the screen, so the player cannot just leave it), centred 4.5 units from the player, filling over 2 s, with a 60 degree wedge behind the tiger left empty (no colour: the player has to see that nothing fills it). The tiger faces the player's perceived position when the circle appears, so the safe wedge is on the far side of it. The player runs at 6 and has to get past the tiger, about 8 units. At the end of the fill the tiger falls in 0.12 s and the whole circle outside the wedge hurts for 2, with a splash ring.
5. The wedge is always reachable: `TigerBeast.FindLandingPoint` tries 16 directions round the player and only takes one where the wedge's middle (3 and 6 units behind the tiger) and both sides (6 units, +-40% of the wedge angle) are on the NavMesh and a complete path leads from the player to the wedge. Checked on 300 random player positions: wedge on the floor and path complete every time. If none of the 16 qualifies it falls back to the first spot on the floor.

Code: `UlvaneBoss.SkyFallCombo`, `DeerBeast.ComboGore`, `UlvaneBeast.ComboFlyUp` / `ComboFallDown` / `SetVisible`, `ChickenBeast.ComboEggRain` / `ComboGlideDown`, `TigerBeast.ComboSlam`, `SkillIndicator.ShowCircleWithSafeWedge` (shader pattern 4), `GroundBlast` got an optional falling visual. A combo cut short by the last stand puts the animals back on the floor (`UlvaneBeast.PutBackOnFloor`).

## Hairball Toss (kitty + penguin, 2026-10-14)

Phase 2 only, harness key `-` (minus). Hits for 2. Danger icons on both; the penguin's icon is hidden only while its question mark shows.

1. Ulvane screams. The kitty runs to the penguin's ice (5 units left of the penguin as it looks at the player) and retches three times (bow with puffed cheeks, jerk up; a code-driven pose, the cat has no puke clip); on the last one it spits a hairball (scale 2.2, about 1.1 across, smaller than the penguin is tall) that lands on the ice in front of the penguin.
2. The penguin turns to it with a blue question mark (`UI/QuestionMark`) for 1.4 s, then picks it up over its head.
3. Throw: after a 0.6 s wind-up the hairball (speed 8, radius 0.7, hits the player once for 2 and keeps rolling) rolls from the penguin through the player's perceived position and does not stop until it reaches the trees at the end of the floor (`Path Max Length` 80, cut by `NavMeshUtility.DistanceToEdge`). The whole path is shown as a lane from the start. On the way it bursts at least five times (`Min Burst Count`): the first at 5 units, then every 6 units, packed closer on a short path. Each burst sends 8 small hairballs (scale 0.9, speed 17.5, radius 0.4, 2 each; they fly until they hit a tree or a rock, and their lanes run that far) down 8 lanes; the lanes of a star appear 1 s before its burst and fill until the big ball gets there, and every other star is turned half a step. The bursts happen on schedule even if the big ball has hit the player.

Code: `UlvaneBoss.HairballCombo`, `KittyBeast.ComboPuke`, `PenguinBeast.ComboWonder` / `ComboTakeBall` / `ComboHairballThrow` / `StarBurst`, `UlvaneProjectile.SetHitRadius` / `SetPiercing`, prefab `Prefabs/Characters/Bosses/Ulvane/UlvaneHairball.prefab` (a copy of the snowball with `Materials/Hairball.mat` and 16 fur strands; assigned to `Hairball Prefab` on `Ulvane.prefab`).

## Stun stars

The deer wears four yellow stars circling above its antlers for the 3 s it is stunned after a counter (`UI/StunStars`, `UI/StunStarOrbit`, drawn in code).

## Checked (Play mode, CLI, slow motion plus screenshots)

Stars above the stunned deer; Sky Fall forced several times with the state probed every second (gather, fly up, eggs falling with their circles, chicken glide, tiger circle with the green wedge, landing, animals back on the floor); Hairball Toss forced several times (hairball on the ice, question mark with no warning icon on the penguin, the big lane, several stars of 8 lanes with the big hairball rolling on and bursting again and again); a natural phase 2 run at 3x speed showed Sky Fall, Hairball Toss and the Whirlwind combos picked by Ulvane. 0 console errors, scene not dirty.

**Not checked:** damage actually applied (the player was invulnerable), the real feel and difficulty (every number is a guess: tiger circle 9 with 2 s to reach the wedge, eggs, burst point and lane lengths), the tiger and chicken in the sky from every camera position (the sky height is 45), the puke from a good angle (the cat is black and in tree shade near the ice), the combos near the arena edge, a combo cut by the last stand, and the penguin's ice being at the top edge of the camera view so its question mark can be out of view.

**Second pass (2026-10-14, after the first playtest):** circle 14 and fall 0.12 s, empty wedge that is always reachable, smaller hairball that rolls to the trees with at least five bursts. Checked in Play mode with screenshots (circle with the empty wedge, the big hairball with two overlapping stars) and the wedge test over 300 positions; the player was invulnerable, so damage and difficulty are still unchecked.

**Hairballs fly to the real trees (2026-10-14):** the big and the small hairballs used to stop at the NavMesh edge, which is where the camera stops seeing the player and lies in open grass in front of the trees, so they seemed to vanish. Now `UlvaneProjectile.SetFliesToScenery` makes them stop only on a physical collider (a horizontal raycast at flight height, `NavMeshUtility.DistanceToScenery`), and the lanes (`PenguinBeast.GetFlightLength`) end at the same place. The trees leave gaps (about 20% of horizontal rays from the arena hit nothing within 80), so the flight is also cut at the edge of the ground (`Ground Half Size` 45, centre `UlvaneBoss.ArenaCenter`). The small ones got 25% faster (14 to 17.5). Checked: a ray test from 400 random arena points (80% hit a tree or rock collider, min 2.5 and max 69.6 away), and in Play mode the 5 small hairballs that the (slow) sampling caught all ended next to a tree or rock; 0 console errors. Not checked: every ball of a full combo by eye, balls leaving through a gap in the trees (they end at the ground edge, 45 from the centre, under the canopy or off screen).
