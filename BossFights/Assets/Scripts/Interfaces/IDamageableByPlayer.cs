﻿namespace Interfaces
{
    /// <summary>
    /// Interface for entities that can be damaged by player attacks (bosses, enemies, destructible objects, etc.)
    /// Implement this interface on any GameObject that should take damage from the player's weapons and abilities.
    /// </summary>
    public interface IDamageableByPlayer
    {
        /// <summary>
        /// Apply damage to this entity from a player attack
        /// </summary>
        /// <param name="damage">Amount of damage to apply (already doubled when it is a critical hit)</param>
        /// <param name="isCrit">Whether the hit was critical, so the damage number can be shown differently</param>
        void TakeDamage(int damage, bool isCrit);
    }
}
