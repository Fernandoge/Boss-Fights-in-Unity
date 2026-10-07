# Player skills: Kick faster, Wall and Katon removed

Branch `feature/player-skills`.

**Kick (E), 25% faster.** In `Animations/Ninja/Ninja Controller.controller` the `Kick` state speed went from 1 to 1.25. The hit window of the counter (animation events `StartKickHit` at 0.47 s and `StopKickHit` at 0.67 s of the 1.30 s clip) is tied to the animation, so it all shrinks with it: the window opens 0.38 s after the kick starts instead of 0.47 s. The follow-up `Flip Kick` (pressing E again during the kick) was sped up by the same factor (speed 1.5 to 1.875) so the combo keeps its rhythm, and `Flip Kick Distance` on the player prefab went from 10 to 12.5 so the flip still travels the same distance (the flip moves at that speed while its window lasts, and the window is 25% shorter). The kick still only counters things tagged `Counterable`; nothing else about it changed.

**Wall (D) removed.** The skill is gone completely:
- `NinjaController`: the `Skill Wall` fields (`_wallPrefab`, `_castInputsWall`, `_wallSpellIcon`), the `Skill_Wall` hash, the D key, the input branch, the `SkillWall` animation-event method and the particle lookup.
- Animator: the `Skill_Wall` state (with the `SkillWall` event it carried) and the `Skill_Wall` parameter. The clip `Ninja Wall` is still in the project, unused.
- Player prefab: the `Skills/NinjaWall` child (the wall object with its particles, NavMeshObstacle and collider).
- HUD: the `Spell Wall` icon was deleted from the spell icon list in `TestScene` and `SecondBossScene`, so the list has 5 icons (Space, Q, W, E, A).
D is still one of the keys a QTE can ask for, because the QTE picks from all of Q, W, E, R, A, S, D, F.

**Verified:** compiles with 0 console errors; in play the player has no `Skill_Wall` animator parameter, the HUD shows the 5 icons and there are no errors. **Not verified:** the kick by hand (the CLI cannot press keys, and setting the kick trigger alone did not enter the `Kick` state), how the faster kick and flip look, and that the flip travels the same distance.

## Second round: kick again 25% faster, kick icon highlight, Katon removed

**Kick, 25% faster again.** `Kick` state speed 1.25 to 1.5625 (1.56x the original), `Flip Kick` 1.875 to 2.34, `Flip Kick Distance` 12.5 to 15.6 so the flip still covers the same ground. The counter window now opens about 0.30 s after pressing E (0.47 s of the clip at 1.56x) and the whole kick lasts about 0.8 s of game time. Measured in play: the kick state ran from about 7.5 s to 8.4 s of game time.

**Highlight while the flying kick is available.** `SpellIcon` has a `highlight` image (a gold frame, child `Highlight` of `Spell Kick` in `TestScene` and `SecondBossScene`) and `SetHighlighted(bool)`: while on, the frame pulses (alpha 0.45 to 1). `NinjaController.SetKickFlipAvailable` sets `_isAbleToKickFlip` and the highlight together: on when the kick starts, off when the kick hit begins (`StartKickHit`), when the flip starts, or when the player state is reset. So the frame shows exactly while pressing E again would trigger the flying kick (about 0.3 s at the current speed). Seen in play: highlight on at the start of the kick, off once the hit window started, and the frame visible around the icon in a capture (thickness set with `Pixels Per Unit Multiplier` 0.3).

**Two icons in the E slot.** The slot shows the old boot icon (`iconKick.png`) normally and switches to the new flying kick picture (`iconFlyingKick.png`, 128 px, generated in code: a dark figure in mid-air with the front leg stretched out, a boot, a hit spark and speed lines) while it is highlighted, so the gold frame and the picture both say "press E again for the flying kick". `SpellIcon` has an optional `Highlighted Sprite` (set on `Spell Kick` in both scenes); `SetHighlighted` swaps the picture and back. Checked in play: iconKick, then iconFlyingKick when highlighted, then iconKick again.

**Katon (A) removed.** `NinjaController` lost the `Skill Katon` fields (`_katonPrefab`, `_fireballSpawnPoint`, `_fireballSpeed`, `_castInputsKaton`, `_katonDamage`, `_katonSpellIcon`), the `Skill_Katon` hash, the A key and its input branch, the multi-charge QTE (`chargesRemaining`, `ChargedKatonQTE`) and `SkillKaton`. `CastingSkill` keeps its `targetedSkill` option (click to confirm) for the next skill. Animator: `Skill_Katon` state and parameter removed. Player prefab: the `Skills/FireballPosition` child removed. HUD: `Spell Katon` icon deleted from both scenes (4 icons left: Space, Q, W, E). The fireball prefab and the `iconFireEruption.png` icon are still in the project, unused. The A slot now belongs to the Huuma shuriken (see `huuma-shuriken.md`).

**Verified:** compiles with 0 console errors; kick state and highlight timing in play, highlight frame and the 4-icon HUD in captures. **Not verified:** the kick by hand and how the faster kick and flip look, the flip distance, the new icon at real size on your screen.
