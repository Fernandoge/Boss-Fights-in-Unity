# Next steps (temporary handoff, delete this file once `feature/bouncing-orbs` is merged)

Written when the work moved to another computer. The branch is `feature/bouncing-orbs`; it holds two things.

**1. Clock mechanic tweaks (done, playtested by the numbers, not by hand)**
- The safe line now starts at the boss's feet (`Clock Spoke Start Distance` 1, was 2), and the near edge has no tolerance, so the ground around the boss stays dangerous. See `clock-mechanic.md`.
- The safe line is 3 m wide for 12/6 and 6 m wide for 3/9 so both look equally thick on screen (the camera squashes depth by half). Please playtest it: the sideways ones are now twice as forgiving in world distance.

**2. Orb Barrage (first version, needs your playtest)**: see `orb-barrage.md` for the design and what was and was not verified.

What to do next, in this order:
1. Open `BossFights/` in Unity 6000.3.25f1, open `Scenes/VexaraScene`, press Play and force the orbs with key 5 (F1 stops the boss's own attacks). Check by eye: bounces against all four walls (orb touching the wall, not sinking in or stopping short: tune `Orb Wall Inset` on the Vexara prefab), orbs bouncing off each other, the hit costing 1 heart.
2. Tune feel: `Orb Speed`, `Orb Lifetime`, `Orb Aim Spread`, `Orb Throw Interval` on the Vexara prefab (header `Orb Barrage`).
3. Replace the placeholder look: the trail renders as a pink wedge. Either fix `OrbTrail` (longer, thinner, softer) or drop the trail and copy and trim a pack effect into the orb prefab; add a hit or bounce effect through `Bounce Effect Prefab`.
4. Run the boss 1 regression from CLAUDE.md after anything that touches shared code (this branch only touches Vexara files and the debug harness, but check the harness keys for boss 1 still work).
5. Open the PR into `main` and delete this file in it.

Notes
- The two `UI/BossSelect/*Preview.renderTexture` files and the `Logs/` and `UserSettings/` files change by themselves when the Editor imports; they are not part of this work and were left out of the commits.
- The Unity CLI (`unity`, winget `Unity.CLI`) needs the project open in the Editor; setup notes are in `CLAUDE.md`.
