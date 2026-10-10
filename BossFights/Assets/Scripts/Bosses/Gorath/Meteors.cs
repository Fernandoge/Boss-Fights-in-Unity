using System.Collections;
using Manager.GameManager;
using UnityEngine;

namespace Bosses.Gorath
{
    public class Meteors : MonoBehaviour
    {
        [SerializeField] private GameObject indicator;
        [SerializeField] private GameObject particles;
        [SerializeField] private Collider meteorCollider;
        
        [Header("Movement Settings")]
        [SerializeField] private float moveSpeed;
        [SerializeField] private float rotationSpeed;
        [SerializeField] private float rampUpTime = 1f;
        
        private bool wasSpawnedInPhase2;

        // Used in Meteor prefab animation
        private void EnableMeteor() => StartCoroutine(StartMeteor(GameManager.Instance.gorath.meteorsDuration));

        private IEnumerator StartMeteor(float duration)
        {
            particles.gameObject.SetActive(true);
            meteorCollider.enabled = true;
            
            // Check and store if this meteor was spawned during phase 2
            // This prevents meteors spawned in phase 1 from suddenly moving when phase 2 starts
            wasSpawnedInPhase2 = GameManager.Instance.gorath.IsInSecondPhase;
            
            var elapsedTime = 0f;
            var delay = 0.5f;
            var startPosition = transform.position;
            
            // Randomize direction for variety
            var randomDirection = Random.Range(0, 2) == 0 ? 1f : -1f; // Clockwise or counter-clockwise
            var randomSpeedMultiplier = Random.Range(0.5f, 1.5f); // Vary speed between meteors (more contrast)
            
            var currentAngle = 0f; // Track angle separately for smooth acceleration
            
            // Each meteor starts spiralling out in its own direction instead of all heading the same way
            var startAngle = Random.Range(0f, 2f * Mathf.PI);
            
            while (duration > 0)
            {
                // Disable collider in the last 0.5 seconds
                if (duration <= 0.5f)
                    meteorCollider.enabled = false;
                
                // Only move if this meteor was spawned in phase 2 and after the delay has passed
                if (wasSpawnedInPhase2 && elapsedTime >= delay)
                {
                    var movementTime = elapsedTime - delay;
                    
                    // Radius and rotation speed ease in together (smoothstep) over rampUpTime, so the meteor spirals out from its
                    // spawn point with no dash and no sudden change of speed or direction when the ramp ends
                    var easedProgress = Mathf.SmoothStep(0f, 1f, movementTime / rampUpTime);
                    var currentRadius = moveSpeed * randomSpeedMultiplier * easedProgress;
                    var currentRotationSpeed = rotationSpeed * 2f * easedProgress * randomDirection;
                    
                    // Accumulate angle based on current rotation speed (proper physics integration)
                    currentAngle += currentRotationSpeed * Time.deltaTime;
                    
                    // Calculate circular motion offset from spawn position
                    var x = Mathf.Sin(startAngle + currentAngle) * currentRadius;
                    var z = Mathf.Cos(startAngle + currentAngle) * currentRadius;
                    
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

            var meteorDamage = GameManager.Instance.gorath.meteorsDamage;
            GameManager.Instance.player.DamagePlayer(meteorDamage);
        }
    }
}
