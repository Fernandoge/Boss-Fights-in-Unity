using UnityEngine;
using UnityEngine.AI;

namespace Characters.Ninja
{
    public class NinjaBasicAttack : BasicAttack
    {
        protected override void OnTriggerEnter(Collider other)
        {
            base.OnTriggerEnter(other);
            if (!other.GetComponent<NavMeshObstacle>()) 
                return;
            
            if (other.GetComponent<ParticleSystem>())
            {
                Destroy(transform.parent.gameObject);
            }
            
            StopCoroutine(moveBulletCoroutine);
            Destroy(transform.parent.gameObject, 2f);
            colliderComponent.enabled = false;
            if (animator != null)
                animator.enabled = false;
        }
    }
}

