# Gorath: extra information for the player

Idea #5: make the first fight easier to read by showing more, without changing how the boss behaves. Built on the branch `feature/gorath-more-information`, waiting for the user's review.

## Cataclysm bars

Three vertical bars float above Gorath's head, dark ("shadowy") at first. They show only **briefly between skills**: once a skill is done and he has been free for 0.3 s, they appear with one more bar red (the new one pops) and stay for half of the wait for the next skill (`CataclysmBarsShowShare`, about 2.3 s in phase 1 and 1.1 s in phase 2), then vanish; they also vanish the instant the next skill starts, so they never share the screen with the danger icon of the Cataclysm. With all three red the next skill is the Cataclysm and the bars pulse; after the Cataclysm they show three dark bars again. They are hidden from the phase 2 transition until the first skill after the intermission is done, and when Gorath dies.

- `UI/CataclysmBars` builds the marker in code on a `TargetMarker` (no scene wiring). `GorathBoss` owns it: `StartAttack` hides it and notes that a skill was cast, the `IdleMovement` override (runs only on the frames the boss is free) shows it after `CataclysmBarsIdleDelay`; `BossController` already has a private `LateUpdate`, so none was added. The count is `_consecutiveNormalAttacks`; the old literal 3 is now `NormalAttacksBeforeCataclysm`.
- Forcing attacks from the debug harness hides the bars like any skill, but only a natural attack adds a bar (as it did the counter before); forcing the Cataclysm resets them.
- `TargetMarker.AddShape` now returns its `SpriteRenderer`.
- History: the first version was a HUD panel under the boss health; the user did not want it. The camera-space HUD canvas has a plane distance of 0 and gives each element a large Z depth, so a new HUD element at depth 0 was invisible. Then horizontal bars, then the user asked for vertical, higher, and only after a skill.

## Intermission icons

- `UI/SlimeWarning`: a red disc with a white skull (kills on touch). `UI/SafeIcon`: a green disc with a white check mark. Both are `TargetMarker`s built in code, flashing like the danger icon.
- While the slimes levitate in plain view each has a skull (`SlimeController.ShowWarning`). When the stones cover them (`ConvertSlimesToRocks`) the icons move onto the stones (`IntermissionStone.ShowInfoIcon`): a skull on each stone that hides a slime, a safe mark on the empty one, floating at 2.9 above the stone so the F prompt fits below.
- **They exist only before the shuffle.** `StartShellGame` first waits `Stone Icon Preview Time` (2.5 s, a field on `GorathBoss`) with the stones still and the icons up, then removes them and starts the shuffle; nothing is shown during the shuffle, when the stones can be kicked or when they are revealed. Without the wait the icons were visible for only about 0.6 s, because the shuffle started right after the stones appeared; the wait delays the shuffle by that much (the dance animation is not retimed).
- Earlier versions kept the icons through the shuffle (the safe mark made the shell game trivial) and put a skull on each revealed slime; both were removed on request.

## Checked and not checked

Checked in Play mode through the CLI: the bars are hidden in a skill and shown after it for about 2.3 s with the count 1, 2, 3, then dark after the Cataclysm; never shown while the danger icon is up; hidden in the intermission and back only after the first skill that follows; the skulls on the levitating slimes; the skulls and the safe mark on the still stones for about 2.5 s, none after that (shuffle, kickable stones, kicking the safe stone); 0 game errors, scene not dirty.

Not checked: how the dance animation looks with the 2.5 s wait, pressing the real keys, the feel of the bar size and height at the normal camera, kicking a slime stone with the real kick (it explodes and kills), a long phase 2 fight, and the other bosses (the code only runs for Gorath).
