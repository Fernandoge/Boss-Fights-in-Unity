using Interfaces;
using Manager.GameManager;
using Shared;
using UnityEngine;
using UnityEngine.AI;

namespace Characters.Ninja
{
    public class NinjaBasicAttack : Projectile
    { 
        private int _attackDamage = 1;
        
        public void SetAttackDamage(int damageAmount)
        {
            _attackDamage = damageAmount;
        }
        
        // Override just in case Ninja basic attack collides with an obstacle
        protected override void OnTriggerEnter(Collider col)
        {
            base.OnTriggerEnter(col);
            
            // Check if hit an entity that can be damaged by the player
            var damageableByPlayer = col.GetComponentInParent<IDamageableByPlayer>();
            if (damageableByPlayer != null)
            {
                damageableByPlayer.TakeDamage(_attackDamage);
                Destroy(transform.parent.gameObject);
                return;
            }
            
            if (!col.GetComponent<NavMeshObstacle>()) 
                return;
            
            // In case of any particles like walls that destroy Ninja basic attacks
            if (col.GetComponent<ParticleSystem>())
                Destroy(transform.parent.gameObject);
            
            // Collided with a solid obstacle
            animator.enabled = false;
            colliderComponent.enabled = false;
            StopCoroutine(moveProjectileCoroutine);
            Destroy(transform.parent.gameObject, 2f);
        }
    }
}
