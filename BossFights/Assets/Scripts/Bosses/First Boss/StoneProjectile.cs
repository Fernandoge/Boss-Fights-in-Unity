using Characters;
using Manager.GameManager;
using Shared;
using UnityEngine;
using UnityEngine.AI;

namespace Bosses.First_Boss
{
    public class StoneProjectile : Projectile
    {
        private float _spawnTime;
        private const float IgnoreCollisionDuration = 0.1f;

        private void Start()
        {
            _spawnTime = Time.time;
        }

        protected override void OnTriggerEnter(Collider col)
        {
            if (col.GetComponent<PlayerController>())
            {
                GameManager.Instance.player.DamagePlayer(GameManager.Instance.firstBoss.rockDamage);
                Destroy(transform.parent.gameObject);
            }
            
            if (col.GetComponent<NavMeshObstacle>())
            {
                // Ignore NavMeshObstacle collisions for the first 0.1 seconds
                if (Time.time - _spawnTime < IgnoreCollisionDuration)
                    return;
                
                Destroy(transform.parent.gameObject);
            }
        }
    }
}
