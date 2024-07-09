using UnityEngine;
using UnityEngine.AI;

namespace Characters.Ninja
{
    public class NinjaBasicAttack : Projectile
    {
        // Override just in case Ninja basic attack collides with an obstacle
        protected override void OnTriggerEnter(Collider collider)
        {
            base.OnTriggerEnter(collider);
            if (!collider.GetComponent<NavMeshObstacle>()) 
                return;
            
            // In case of any particles like walls that destroy Ninja basic attacks
            if (collider.GetComponent<ParticleSystem>())
                Destroy(transform.parent.gameObject);
            
            // Collided with a solid obstacle
            animator.enabled = false;
            colliderComponent.enabled = false;
            StopCoroutine(moveProjectileCoroutine);
            Destroy(transform.parent.gameObject, 2f);
        }
    }
}

