using System.Collections;
using Interfaces;
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
        
        private int currentHealth;
        private SlimeController[] cachedSlimes; // Cached list of other slimes
        private Renderer meshRenderer;
        private MaterialPropertyBlock propertyBlock;
        private static readonly int ColorPropertyId = Shader.PropertyToID("_Color");
        private NavMeshAgent navMeshAgent;
        private Animator animator;
        private bool isDead;
        private bool isFlashing; // Track if we're currently flashing
        private static readonly int Dizzy = Animator.StringToHash("Dizzy");

        void Start()
        {
            // Initialize health
            currentHealth = maxHealth;
        
            // Get renderer and initialize MaterialPropertyBlock for flash effect
            meshRenderer = GetComponentInChildren<Renderer>();
            if (meshRenderer != null)
            {
                propertyBlock = new MaterialPropertyBlock();
            }
        
            // Get Animator component
            animator = GetComponent<Animator>();
            if (animator == null)
            {
                Debug.LogWarning("Animator component is missing on " + gameObject.name);
            }
        
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
            if (navMeshAgent == null)
            {
                Debug.LogError("NavMeshAgent component is missing on " + gameObject.name);
            }
            else
            {
                // Start moving immediately
                Vector3 randomPoint = GetRandomPointOnNavMesh();
                navMeshAgent.SetDestination(randomPoint);
            }
        }

        void Update()
        {
            if (navMeshAgent != null && !isDead)
            {
                Roam();
            }
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
            // Don't take damage if already dead
            if (isDead) return;
            
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
        
            // Trigger the Dizzy animation
            if (animator != null)
                animator.SetTrigger(Dizzy);
        }
    
        // This is called at the beginning of walk animation
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
