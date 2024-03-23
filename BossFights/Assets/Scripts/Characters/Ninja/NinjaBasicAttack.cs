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
            
            StopCoroutine(moveBulletCoroutine);
            transform.parent.SetParent(other.transform);
            Destroy(transform.parent.gameObject, 2f);
            colliderComponent.enabled = false;
            if (animator != null)
                animator.enabled = false;
        }
    }
}

