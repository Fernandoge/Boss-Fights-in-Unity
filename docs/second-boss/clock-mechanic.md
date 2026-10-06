# Second boss: the clock mechanic

The wizard's core mechanic, the equivalent of the first boss's Cataclysm. It is a memory test.

**1. Numbers.** Every attack the boss casts (spell circles or diagonal lines) drops one clock number next to its head: 3, 6, 9 or 12. The four numbers come in a random order each cycle and none repeats, so after four attacks all four have appeared. A number stays while the skill is being cast and fades out when the skill ends. (While the debug harness forces a single attack with F5, the mechanic is paused so one skill can be tested in a loop.)

**2. Intermission.** After the fourth attack the next attack is replaced by the intermission. The boss teleports to the centre of the arena, is invulnerable for its whole duration, and sends four waves, one per number, in the order the numbers appeared. There is no on-screen legend: the player has to remember both the numbers and the order.

**3. Waves.** Each wave warns the whole arena in red (including the safe line, so the warning gives nothing away) for 1.4 s, then hits everything except one straight line (the "spoke") from the centre towards the number: 12 is up on the screen, 3 right, 6 down and 9 left. A player not standing on the spoke takes 2 damage (the player has 3 hearts, so two misses are lethal; the 2.2 s hit immunity does not block the next wave because waves are 2.4 s apart). Right after the explosion the safe line lights up in green for about a second, so players who died can learn what the correct spot was. The spoke is 4 m wide and starts 2 m from the boss.

**4. After the fourth wave** the boss teleports to a random spot and the cycle starts over with a new random order.

**Pieces**
- `Bosses/Second Boss/ClockNumberPopup.cs` (prefab `ClockNumber`, 3D text with the `ClockNumber` material): the floating number; it follows the boss, faces the camera and is hidden with `Hide()`.
- `Bosses/Second Boss/ClockWaveBlast.cs` (prefab `ClockWaveBlast`): one wave. Owns its timers, draws the red warning (`DiagonalLineIndicator`), the explosion flash on everything except the spoke (`LineBlastFlash`, drawn as up to four rectangles around the spoke), the green safe line (`ClockSafeIndicator`, material `ClockSafeLine`) and the damage check.
- `SecondBoss.cs`: the counter (`_clockNumbers`, `_clockNumbersShown`), `ShowNextClockNumber`, `ClockIntermissionSequence`. The intermission replaces the attack in `PerformAttack` once four numbers have been shown. Tuning is under `Clock Mechanic` on the boss: `Clock Wave Interval` 2.4, `Clock Wave Telegraph Time` 1.4, `Clock Wave Damage` 2, `Clock Spoke Width` 4, `Clock Spoke Start Distance` 2, `Clock Intro Time` 1 and `Clock End Time` 1.
- Harness with the second boss: key 4 forces an intermission.

**Verified:** over about ten cycles in a sped-up natural run, each cycle showed four different numbers and the four waves used the safe lines in exactly that order, then the counter reset; a number never stayed after its skill. Damage was checked in play: standing off the safe line cost 2 hearts (3 to 1), standing on it cost none, and the green safe line showed after the explosion.

**Known limits / ideas:** no sound and no particles on the waves; the cast animation during the intermission is the same area cast as the diagonal lines; the player has no death handling yet, so after two misses the health just goes to 0 or below.
