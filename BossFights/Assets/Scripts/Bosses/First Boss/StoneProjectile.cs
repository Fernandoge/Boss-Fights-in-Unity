using Characters;
using Manager.GameManager;
using Shared;
using UnityEngine;
using UnityEngine.AI;

namespace Bosses.First_Boss
{
    public class StoneProjectile : Projectile
    {
        protected override void OnTriggerEnter(Collider col)
        {
            if (col.GetComponent<PlayerController>())
            {
                GameManager.Instance.player.DamagePlayer(GameManager.Instance.firstBoss.rockDamage);
                Destroy(transform.parent.gameObject);
            }
            
            if (col.GetComponent<NavMeshObstacle>())
                Destroy(transform.parent.gameObject);
        }
    }
}
