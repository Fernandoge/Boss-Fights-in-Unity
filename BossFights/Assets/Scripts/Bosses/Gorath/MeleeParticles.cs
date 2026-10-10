using Manager.GameManager;
using UnityEngine;

namespace Bosses.Gorath
{
    public class MeleeParticles : MonoBehaviour
    {
        private void OnParticleTrigger()
        {
            GameManager.Instance.player.DamagePlayer(GameManager.Instance.gorath.meleesDamage);
        }
    }
}
