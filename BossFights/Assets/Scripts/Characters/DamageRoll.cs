using UnityEngine;

namespace Characters
{
    // Every player damage source rolls through here, so the crit chance and multiplier are tuned in one place
    public static class DamageRoll
    {
        public const float CritChance = 0.1f;
        public const int CritMultiplier = 2;

        public static int Roll(int baseDamage, out bool isCrit)
        {
            isCrit = Random.value < CritChance;
            return isCrit ? baseDamage * CritMultiplier : baseDamage;
        }
    }
}
