# Damage numbers and crits

- Every player hit goes through `Characters/DamageRoll.Roll(baseDamage, out isCrit)`: 10% crit chance, crit = double damage (`CritChance`, `CritMultiplier`). Rolled per hit, so it applies to every skill that calls it.
- `IDamageableByPlayer.TakeDamage(int damage, bool isCrit)`: bosses (`BossController`) and slimes show a `UI/DamageNumber` above their collider; nothing is shown while they are immune.
- `UI/DamageNumber` builds itself in code (TextMeshPro, billboard, rises and fades), no prefab or scene wiring. White for normal hits, gold and bigger with "!" for crits. Sizes and timing are constants at the top of the class.
- Base damage: basic attack 2 (`PlayerController._basicAttackDamage` on the ninja prefab), Huuma shuriken 5 (`NinjaController._huumaDamage`), clone shots 1 (`NinjaClone._basicAttackDamage` on the clone prefab).
- A new damage source: roll with `DamageRoll.Roll`, then call `TakeDamage(rolled, isCrit)`.
