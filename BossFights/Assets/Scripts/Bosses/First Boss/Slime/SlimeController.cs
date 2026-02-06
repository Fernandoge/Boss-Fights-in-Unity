using System.Collections;
using Interfaces;
using Manager.GameManager;
using UnityEngine;
using UnityEngine.AI;

namespace Bosses.First_Boss.Slime
{
    public class SlimeController : MonoBehaviour, IDamageableByPlayer
    {
        [Header("Health")]
        [SerializeField] private int maxHealth = 50;
    
        [Header("Movement")]
        [SerializeField] private float roamRadius;
        [SerializeField] private float minMoveDistance;
        [SerializeField] private float wallClearance; // Distance to maintain from walls
        [SerializeField] private float slimeSeparationDistance; // Minimum distance to maintain from other slimes
        
        [Header("Intermission")]
        [SerializeField] private GameObject explosionEffectPrefab;
        
        private int currentHealth;
        private SlimeController[] cachedSlimes; // Cached list of other slimes
        private Renderer meshRenderer;
        private MaterialPropertyBlock propertyBlock;
        private static readonly int ColorPropertyId = Shader.PropertyToID("_Color");
        private NavMeshAgent navMeshAgent;
        private Animator animator;
        private bool isDead;
        private bool isFlashing; // Track if we're currently flashing
        private bool isIntermissionActive; // Disable roaming when moving to intermission position
        private bool isImmuneToDamage; // Disable damage during intermission
        private Vector3 spawnPosition; // Original spawn position to return to after intermission
        private static readonly int Dizzy = Animator.StringToHash("Dizzy");
        private static readonly int Dizzy_Loop = Animator.StringToHash("DizzyLoop");
        private static readonly int Explode = Animator.StringToHash("Explode");
        private static readonly int DieAfterPushed = Animator.StringToHash("DieAfterPushed");
        private static readonly int BackToWalk = Animator.StringToHash("BackToWalk");

        void Start()
        {
            // Save spawn position for returning after intermission
            spawnPosition = transform.position;
            currentHealth = maxHealth;
        
            // Get renderer and initialize MaterialPropertyBlock for flash effect
            meshRenderer = GetComponentInChildren<Renderer>();
            if (meshRenderer != null)
                propertyBlock = new MaterialPropertyBlock();
            
            animator = GetComponent<Animator>();
        
            // Find all slimes once at startup and cache the list (excluding ourselves)
            SlimeController[] allSlimes = FindObjectsOfType<SlimeController>();
            System.Collections.Generic.List<SlimeController> slimeList = new System.Collections.Generic.List<SlimeController>();
            foreach (SlimeController slime in allSlimes)
            {
                if (slime != this)
                    slimeList.Add(slime);
            }
            cachedSlimes = slimeList.ToArray();
        
            // Get NavMeshAgent component
            navMeshAgent = GetComponent<NavMeshAgent>();
            // Start moving immediately
            Vector3 randomPoint = GetRandomPointOnNavMesh();
            navMeshAgent.SetDestination(randomPoint);
        }

        void Update()
        {
            if (navMeshAgent != null && !isDead && !isIntermissionActive)
                Roam();
        }

        void LateUpdate()
        {
            // Force the flash color to persist even after Animator writes to material
            if (isFlashing && meshRenderer != null)
            {
                meshRenderer.GetPropertyBlock(propertyBlock);
                propertyBlock.SetColor(ColorPropertyId, Color.red);
                meshRenderer.SetPropertyBlock(propertyBlock);
            }
        }

        /// *** Movement Logic *** ///
        
        // Called by boss during intermission to move slime to specific position
        public IEnumerator MoveToIntermissionPosition(Vector3 targetPosition, float speed)
        {
            // Disable roaming behavior
            isIntermissionActive = true;
            
            // Store original settings
            float originalSpeed = navMeshAgent.speed;
            float originalStoppingDistance = navMeshAgent.stoppingDistance;
            ObstacleAvoidanceType originalAvoidance = navMeshAgent.obstacleAvoidanceType;
            
            // Disable obstacle avoidance so player can't push slimes during intermission
            navMeshAgent.obstacleAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance;
            
            // Set fast intermission speed and zero stopping distance for precise positioning
            navMeshAgent.speed = speed;
            navMeshAgent.stoppingDistance = 0f;
            navMeshAgent.isStopped = false;
            
            // Set destination
            navMeshAgent.SetDestination(targetPosition);
            
            // Wait for path to be calculated
            yield return new WaitUntil(() => !navMeshAgent.pathPending);
            
            // Wait until slime reaches the position with tight threshold
            while (Vector3.Distance(transform.position, targetPosition) > 0.1f)
                yield return null;
            
            // Stop at position
            navMeshAgent.isStopped = true;
            
            // Rotate slime to look south
            Quaternion lookSouthRotation = Quaternion.Euler(0, 220, 0);
            float rotationDuration = 0.6f;
            float elapsedTime = 0f;
            Quaternion startRotation = transform.rotation;
            
            while (elapsedTime < rotationDuration)
            {
                elapsedTime += Time.deltaTime;
                transform.rotation = Quaternion.Slerp(startRotation, lookSouthRotation, elapsedTime / rotationDuration);
                yield return null;
            }
            
            // Ensure final rotation is set
            transform.rotation = lookSouthRotation;
            
            // Restore original movement settings (but keep obstacle avoidance disabled during intermission)
            navMeshAgent.speed = originalSpeed;
            navMeshAgent.stoppingDistance = originalStoppingDistance;
        }
        
        // Called by boss to make slime levitate upward
        public IEnumerator LevitateUp(float liftHeight, float duration)
        {
            float elapsedTime = 0f;
            float startOffset = navMeshAgent.baseOffset;
            float targetOffset = startOffset + liftHeight;
            
            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                navMeshAgent.baseOffset = Mathf.Lerp(startOffset, targetOffset, elapsedTime / duration);
                yield return null;
            }
            
            // Ensure final offset is set
            navMeshAgent.baseOffset = targetOffset;
        }

        public void StartExplosion() => animator.SetTrigger(Explode);

        // Called in explosion animation event to hide slime and spawn explosion effect
        private void ExplodeSlime()
        {
            meshRenderer.enabled = false; // Hide the slime mesh
            Vector3 explosionPosition = transform.position + Vector3.up * 1.5f;
            Instantiate(explosionEffectPrefab, explosionPosition, Quaternion.identity);
            GameManager.Instance.player.DamagePlayer(1000);
        }
        
        // Move slime back to its spawn position after intermission
        public IEnumerator MoveBackToSpawn(float speed)
        {
            // Store initial base offset (current levitation height) and original nav speed
            float initialBaseOffset = navMeshAgent.baseOffset;
            float originalNavSpeed = navMeshAgent.speed;
            
            navMeshAgent.isStopped = false;
            navMeshAgent.speed = speed;
            navMeshAgent.SetDestination(spawnPosition);
            
            // Wait for path to be calculated
            yield return new WaitUntil(() => !navMeshAgent.pathPending);
            
            // Store initial distance for lerp calculation
            float initialDistance = navMeshAgent.remainingDistance;
            
            // Gradually lower slime to ground while moving to spawn position
            while (navMeshAgent.remainingDistance > 4f)
            {
                // Calculate progress (0 to 1) based on remaining distance on NavMesh path
                float progress = 1f - (navMeshAgent.remainingDistance / initialDistance);
                
                // Remap progress so baseOffset reaches 0 at 66% progress (0.66)
                // After 66% progress, baseOffset stays at 0
                float baseOffsetProgress = Mathf.Clamp01(progress / 0.66f);
                
                // Lerp base offset from initial height to 0 (ground level)
                navMeshAgent.baseOffset = Mathf.Lerp(initialBaseOffset, 0f, baseOffsetProgress);
                yield return null;
            }
            animator.SetTrigger(DieAfterPushed);
            navMeshAgent.speed = originalNavSpeed * 2;
        }
        
        // Set animation speed (useful for quickly resetting to idle) - Now called in idle walk animation
        public void SetAnimationSpeed(float speed) => animator.speed = speed;

        public void SetSlimeDizzyLoop()
        {
            animator.SetTrigger(Dizzy_Loop);
            navMeshAgent.isStopped = true;
        }

        // Set damage immunity (used during intermission)
        public void SetDamageImmunity(bool immune) => isImmuneToDamage = immune;

        private void Roam()
        {
            // Pick a new destination when we've reached the current one
            if (!navMeshAgent.pathPending && navMeshAgent.remainingDistance <= navMeshAgent.stoppingDistance)
            {
                Vector3 randomPoint = GetRandomPointOnNavMesh();
                navMeshAgent.SetDestination(randomPoint);
            }
        }
    
        private Vector3 GetRandomPointOnNavMesh()
        {
            int maxAttempts = 100;
        
            // Keep trying until we find a point that's at least minMoveDistance away and has wall clearance
            for (int i = 0; i < maxAttempts; i++)
            {
                // Generate a random point within the roam radius
                Vector3 randomDirection = Random.insideUnitSphere * roamRadius;
                randomDirection += transform.position;
        
                // Find the nearest valid point on the NavMesh
                NavMeshHit hit;
                if (NavMesh.SamplePosition(randomDirection, out hit, roamRadius, NavMesh.AllAreas))
                {
                    // Check if the point is far enough from current position and has clearance from walls
                    float distance = Vector3.Distance(transform.position, hit.position);
                    if (distance >= minMoveDistance && HasWallClearance(hit.position) && !IsNearOtherSlime(hit.position))
                    {
                        return hit.position;
                    }
                }
            }
        
            // If no valid point found after max attempts, return a point at minMoveDistance
            Vector3 fallbackDirection = Random.insideUnitSphere.normalized * minMoveDistance;
            fallbackDirection += transform.position;
        
            NavMeshHit fallbackHit;
            if (NavMesh.SamplePosition(fallbackDirection, out fallbackHit, roamRadius, NavMesh.AllAreas))
            {
                return fallbackHit.position;
            }
        
            return transform.position;
        }

        private bool HasWallClearance(Vector3 position)
        {
            // Check multiple points around the position to see if they're all on valid NavMesh
            // If any point at wallClearance distance is OFF the NavMesh, we're too close to a wall
            Vector3[] directions = { Vector3.forward, Vector3.back, Vector3.left, Vector3.right,
                                     (Vector3.forward + Vector3.right).normalized, 
                                     (Vector3.forward + Vector3.left).normalized,
                                     (Vector3.back + Vector3.right).normalized,
                                     (Vector3.back + Vector3.left).normalized };
            
            int invalidPoints = 0;
            
            foreach (Vector3 dir in directions)
            {
                // Check a point at wallClearance distance in this direction
                Vector3 checkPoint = position + (dir * wallClearance);
                
                NavMeshHit hit;
                // If we can't find NavMesh nearby, or it's too far away, this direction is blocked
                if (!NavMesh.SamplePosition(checkPoint, out hit, wallClearance * 0.5f, NavMesh.AllAreas))
                {
                    invalidPoints++;
                }
                else
                {
                    // Check if the sampled position is significantly different from our check point
                    // This indicates we're near a NavMesh edge
                    float distance = Vector3.Distance(checkPoint, hit.position);
                    if (distance > wallClearance * 0.3f)
                    {
                        invalidPoints++;
                    }
                }
            }
            
            // If more than 2 directions are blocked/invalid, we're too close to walls
            return invalidPoints <= 2;
        }

        private bool IsNearOtherSlime(Vector3 position)
        {
            // Use cached slimes list instead of finding them every time
            if (cachedSlimes == null || cachedSlimes.Length == 0)
                return false;
            
            foreach (SlimeController otherSlime in cachedSlimes)
            {
                // Check if the position is too close to another slime
                float distance = Vector3.Distance(position, otherSlime.transform.position);
                if (distance < slimeSeparationDistance)
                {
                    return true; // Too close to another slime
                }
            }
            
            return false; // No slimes nearby
        }

        /// *** Health Logic *** ///
    
        // IDamageableByPlayer interface implementation
        public void TakeDamage(int damage)
        {
            // Don't take damage if immune (during intermission) or already dead
            if (isImmuneToDamage || isDead) return;
            
            currentHealth -= damage;
        
            if (currentHealth <= 0)
            {
                Die();
            }
            else if (meshRenderer != null)
            {
                StartCoroutine(FlashOnHit());
            }
        }
    
        private void Die()
        {
            isDead = true;
            // Stop the NavMeshAgent completely
            if (navMeshAgent != null)
            {
                navMeshAgent.isStopped = true;
                navMeshAgent.velocity = Vector3.zero;
                navMeshAgent.enabled = false; // Disable to ensure no movement during animation
            }
            animator.SetTrigger(Dizzy);
        }
        
        public void Resurrect()
        {
            isDead = false;
            
            // Restore health to full
            currentHealth = maxHealth;
        
            // Re-enable the NavMeshAgent
            if (navMeshAgent != null)
            {
                navMeshAgent.enabled = true;
                navMeshAgent.isStopped = false;
            
                // Set a new destination to start moving
                Vector3 randomPoint = GetRandomPointOnNavMesh();
                navMeshAgent.SetDestination(randomPoint);
            }
            
            if (isIntermissionActive)
            {
                isImmuneToDamage = false;
                isIntermissionActive = false;
                // Re-enable obstacle avoidance when returning to normal roaming
                navMeshAgent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
            }
        }
    
        private IEnumerator FlashOnHit()
        {
            // Set flag to trigger LateUpdate override
            isFlashing = true;
            
            yield return new WaitForSeconds(0.04f);
        
            // Clear flag and property block to return control to animator/material
            isFlashing = false;
            propertyBlock.Clear();
            meshRenderer.SetPropertyBlock(propertyBlock);
        }
    }
}
