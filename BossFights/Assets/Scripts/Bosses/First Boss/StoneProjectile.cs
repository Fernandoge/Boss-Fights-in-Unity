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
        private float _ignoreCollisionDuration = 0f;

        private void Start()
        {
            _spawnTime = Time.time;
        }
        
        public void SetIgnoreCollisionDuration(float duration)
        {
            _ignoreCollisionDuration = duration;
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
                // Ignore NavMeshObstacle collisions for the specified duration
                // This is required for rock shower, so it doesn't collide with level walls
                if (Time.time - _spawnTime < _ignoreCollisionDuration)
                    return;
                
                Destroy(transform.parent.gameObject);
            }
        }
    }
}
