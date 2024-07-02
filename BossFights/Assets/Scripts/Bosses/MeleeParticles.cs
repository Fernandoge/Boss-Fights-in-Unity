using Manager.GameManager;
using UnityEngine;

namespace Bosses
{
    public class MeleeParticles : MonoBehaviour
    {
        private void OnParticleTrigger()
        {
            GameManager.Instance.player.DamagePlayer(GameManager.Instance.firstBoss.meleesDamage);
        }
    }
}
