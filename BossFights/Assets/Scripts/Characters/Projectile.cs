using System;
using System.Collections;
using JetBrains.Annotations;
using UnityEngine;

namespace Characters
{
    public class Projectile : MonoBehaviour
    {
        protected Coroutine moveProjectileCoroutine;
        [CanBeNull] protected Animator animator;
        [CanBeNull] protected Collider colliderComponent;

        private void Awake()
        {
            animator = GetComponent<Animator>();
            colliderComponent = GetComponent<Collider>();
        }

        private void OnBecameInvisible() => Destroy(transform.parent.gameObject);

        protected virtual void OnTriggerEnter(Collider collider) => print($"Projectile {transform.parent.name} collides");

        public void Shoot(float projectileSpeed, Vector3 initialPosition, Vector3 direction, float lifetime = 0)
        {
            moveProjectileCoroutine = StartCoroutine(MoveProjectile(projectileSpeed, initialPosition, direction, lifetime));
        }

        private IEnumerator MoveProjectile(float bulletSpeed, Vector3 initialPosition, Vector3 direction, float lifetime)
        {
            var elapsedTime = 0f;
            // Move projectile infinitely if lifetime value is not used
            if (lifetime == 0)
                lifetime = float.MaxValue;
            
            while (elapsedTime < lifetime)
            {
                elapsedTime += Time.deltaTime;
                var distance = elapsedTime * bulletSpeed;
                transform.position = initialPosition + direction * distance;
                yield return null;
            }
        
            Destroy(transform.parent.gameObject);
        }
    }
}
