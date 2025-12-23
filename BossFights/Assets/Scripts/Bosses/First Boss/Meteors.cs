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
        
        private bool wasSpawnedInPhase2;

        // Used in Meteor prefab animation
        private void EnableMeteor() => StartCoroutine(StartMeteor(GameManager.Instance.firstBoss.meteorsDuration));

        private IEnumerator StartMeteor(float duration)
        {
            particles.gameObject.SetActive(true);
            meteorCollider.enabled = true;
            
            // Check and store if this meteor was spawned during phase 2
            // This prevents meteors spawned in phase 1 from suddenly moving when phase 2 starts
            wasSpawnedInPhase2 = GameManager.Instance.firstBoss.IsInSecondPhase;
            
            var elapsedTime = 0f;
            var delay = 0.5f;
            var startPosition = transform.position;
            
            // Randomize direction for variety
            var randomDirection = Random.Range(0, 2) == 0 ? 1f : -1f; // Clockwise or counter-clockwise
            var randomSpeedMultiplier = Random.Range(0.5f, 1.5f); // Vary speed between meteors (more contrast)
            
            var currentAngle = 0f; // Track angle separately for smooth acceleration
            
            while (duration > 0)
            {
                // Disable collider in the last 0.5 seconds
                if (duration <= 0.5f)
                    meteorCollider.enabled = false;
                
                // Only move if this meteor was spawned in phase 2 and after the delay has passed
                if (wasSpawnedInPhase2 && elapsedTime >= delay)
                {
                    var movementTime = elapsedTime - delay;
                    
                    // Gradually increase radius over 0.3 seconds to prevent initial jump/teleport
                    var radiusMultiplier = Mathf.Min(movementTime / 0.3f, 1f);
                    var currentRadius = moveSpeed * randomSpeedMultiplier * radiusMultiplier;
                    
                    // Gradually increase rotation speed with quadratic ease-in for more noticeable acceleration
                    var speedRampUp = Mathf.Min(movementTime / 0.25f, 1f);
                    speedRampUp = speedRampUp * speedRampUp; // Square it for exponential acceleration (slow to fast)
                    var currentRotationSpeed = rotationSpeed * 2f * speedRampUp * randomDirection;
                    
                    // Accumulate angle based on current rotation speed (proper physics integration)
                    currentAngle += currentRotationSpeed * Time.deltaTime;
                    
                    // Calculate circular motion offset from spawn position
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
