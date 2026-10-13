# Ulvane combined animal commands (idea #2)

Two animals work together and one skill feeds the other. Combos are the only Ulvane skills that hit for 2 (the danger icon shows above him); every single skill (arrow, tiger, horse, kitty and the rest) now hits for 1.

## How a combo runs

`UlvaneBoss` drives both beasts step by step: `BeastX.BeginCombo()` takes the beast off its patrol and stops its own skill, the boss calls the beast's public combo methods, and `EndCombo()` gives it back (cooldown restarts as after a normal skill). `Enrage()` or the mourning request also release a held beast, and `OnComboReleased()` cleans up what it built. Forced from the harness with key 9 (`UlvaneAttack.SnowTornado`); nothing picks it on its own yet.

## Snow Tornado (dog + penguin)

Phase 2 only. In phase 2 Ulvane's attack picker chooses it once the cooldown is over (first 5 s after the phase starts, then every 16 to 24 s, `Snow Tornado Combo` fields on the boss) and both animals are free; otherwise he shoots. The harness key 9 forces it in any phase.

1. Ulvane only screams (the roar clip) and goes back to his own attacks. The combo runs beside him as its own coroutine (`SnowTornadoCombo`), and the other animals keep using their own skills. A danger icon shows above the dog and the penguin, not above Ulvane.
2. The dog runs to 5 units left of the penguin (as it looks at the player; the other side if that is outside the arena), spins up a whirlwind and stays there spinning. The whirlwind circle hurts for 2 every 0.7 s.
3. The penguin lobs a harmless snowball at the dog every 0.6 s. Each one that lands joins the balls circling inside the whirlwind.
4. With 5 stacked, and the dog still spinning, the snowballs go one after the other like Gorath's rock shower: every 0.25 s a lane starts on where the player is (perceived position plus a short lead), fills for 0.45 s, and the snowball flies down it at 48 (about 220% of the penguin's 21.84) for 2 damage. After the last one the whirlwind winds down, the dog is dizzy 0.9 s, and both go back to patrolling.

Code: `DogBeast` (`StormStart`, `AddSnowball`, `ThrowStack`), `PenguinBeast.ComboFeed`, `UlvaneBeast.BeginCombo` / `ShowDanger`, `UlvaneProjectile.LaunchAtTarget` / `Hold`, `UlvaneBoss.SnowTornadoSequence` and `SnowTornadoCombo`. Tuning is under `Snow Tornado Combo` on the dog, the penguin and the boss.

Checked in Play mode through the CLI: the combo forced and also picked naturally after the phase 2 transition, the dog running over, the whirlwind with the snowballs circling inside, the stack counting to 5, the lane aimed at the player while the whirlwind spins, the danger icons on both animals only, 0 console errors, scene not dirty. Not checked: the feel and difficulty of speed 48 and the 0.25 s rhythm, whether the 5 hits can be dodged, the combo near the arena edge, the combo being cut by phase 3, and the first throw frame by eye.

**Dog run fix (2026-10-13):** the dog ran to the penguin in his idle pose (sliding). The combo starts the run, then the dog's patrol step notices the command and calls `Idle()` once, which cancelled the `Gallop()` that `RunTo` had set before its loop. `RunTo` now sets `Gallop()` every frame. Checked in Play mode: during the run `Vert` and `State` stay at 1 and the run cycle advances. Not checked by eye at normal speed.
