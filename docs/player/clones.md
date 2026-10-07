# Player skills: Clones (W) quick cast

The concept for the ninja's skills: after the key combination the skill is cast quickly. The Clones skill is the first one reworked. Branch `feature/player-skills`.

**How the cast works:** W starts `CastingSkill` (animator trigger `Skill_Clones`, bool `Casting`, QTE of `Cast Inputs Clones` keys). Each correct key sets the trigger `CastInput`; when the last one is pressed `CompletedQTE` clears `Casting`, the animator goes from the `Casting` state to `Skill_Clones` and the animation event `SkillClones` (in `Ninja Clones V2.anim`, at 1.62 s of the clip) spawns the two clones.

**What changed:** only the animator (`Animations/Ninja/Ninja Controller.controller`), no code.
- The transition `Casting` to `Skill_Clones` no longer waits for an exit time (it was 0.21 of the clapping loop), blends in 0.1 s (was 0.25 s) and starts the clone animation **40% of the way through** (transition Offset 0.4, so the first second of the wind-up is skipped and the arms are already opening). The clip is 2.57 s, the event is at 1.62 s, so the clones now appear 0.47 s after the animation starts.
- The `Skill_Clones` speed is back to its original 1.25. (A first version raised it to 3 to get the same quickness, and looked funny.)

**Measured** (in play, simulating the end of the QTE by clearing `Casting`, polled every 0.1 s of game time): from the last key to both clones on the field, 1.31 s before, 0.78 s with the 3x speed version and 0.53 s now. The real key input was not used (the CLI cannot press keys), so the feel by hand is untested. The pose at 40% of the clip (clip time 1.03 s) is close to the arms-out pose seen at 0.9 s.

**To tune:** the transition Offset on `Casting` to `Skill_Clones` (higher skips more, 0.55 would make the clones come out at about 0.27 s) or the state speed. Cooldown, duration and clone behaviour are unchanged.
