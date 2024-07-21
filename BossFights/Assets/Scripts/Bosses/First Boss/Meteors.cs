using System.Collections;
using Manager.GameManager;
using UnityEngine;

namespace Bosses.First_Boss
{
    public class Meteors : MonoBehaviour
    {
        [SerializeField] private GameObject indicator;
        [SerializeField] private GameObject particles;
        [SerializeField] private Collider meteorCollider;
        
        [Header("Movement Settings")]
        [SerializeField] private float moveSpeed;
        [SerializeField] private float rotationSpeed;

        // Used in Meteor prefab animation
        private void EnableMeteor() => StartCoroutine(StartMeteor(GameManager.Instance.firstBoss.meteorsDuration));

        private IEnumerator StartMeteor(float duration)
        {
            particles.gameObject.SetActive(true);
            meteorCollider.enabled = true;
            
            var elapsedTime = 0f;
            var delay = 0.5f;
            var startPosition = transform.position;
            
            // Randomize direction for variety
            var randomDirection = Random.Range(0, 2) == 0 ? 1f : -1f; // Clockwise or counter-clockwise
            var randomSpeedMultiplier = Random.Range(0.2f, 2f); // Vary speed between meteors
            
            var currentAngle = 0f; // Track angle separately for smooth acceleration
            
            while (duration > 0)
            {
                if (duration <= 0.5f)
                    meteorCollider.enabled = false;
                
                // Only move after the delay has passed
                if (elapsedTime >= delay)
                {
                    var movementTime = elapsedTime - delay;
                    
                    // Gradually increase radius over 0.3 seconds to prevent initial jump
                    var radiusMultiplier = Mathf.Min(movementTime / 0.3f, 1f);
                    var currentRadius = moveSpeed * randomSpeedMultiplier * radiusMultiplier;
                    
                    // Gradually increase rotation speed with quadratic ease-in for more noticeable acceleration
                    var speedRampUp = Mathf.Min(movementTime / 0.25f, 1f);
                    speedRampUp = speedRampUp * speedRampUp; // Square it for exponential acceleration
                    var currentRotationSpeed = rotationSpeed * 2f * speedRampUp * randomDirection;
                    
                    // Accumulate angle based on current rotation speed (proper integration)
                    currentAngle += currentRotationSpeed * Time.deltaTime;
                    
                    var x = Mathf.Sin(currentAngle) * currentRadius;
                    var z = Mathf.Cos(currentAngle) * currentRadius;
                    
                    transform.position = startPosition + new Vector3(x, 0, z);
                }
                
                duration -= Time.deltaTime;
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            Destroy(gameObject);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player"))
                return;

            var meteorDamage = GameManager.Instance.firstBoss.meteorsDamage;
            GameManager.Instance.player.DamagePlayer(meteorDamage);
        }
    }
}
