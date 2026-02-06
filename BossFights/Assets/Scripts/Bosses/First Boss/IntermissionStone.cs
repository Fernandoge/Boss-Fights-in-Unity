using System.Collections;
using Interfaces;
using UnityEngine;

namespace Bosses.First_Boss
{
    /// <summary>
    /// Controls individual shell game stones during the boss intermission phase.
    /// Handles movement, swapping positions, and tracking which slime is underneath.
    /// Becomes counterable (kickable) after shuffle completes.
    /// </summary>
    public class IntermissionStone : MonoBehaviour, ICounterable
    {
        [Header("Movement Settings")]
        [SerializeField] private float moveHeight = 2f; // How high the stone lifts during swap
        [SerializeField] private float moveDuration = 0.8f; // Duration of one swap movement
        [SerializeField] private AnimationCurve moveCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        
        // References
        private Transform slimeTransform; // The slime underneath this stone (can be null for empty stone)
        private Vector3 basePosition; // Current base position on ground
        private bool isMoving;
        private bool isCounterable; // Can player kick this stone?
        private Renderer stoneRenderer;
        private MaterialPropertyBlock propertyBlock;
        private Color originalColor;
        private Collider stoneCollider;
        private string originalTag;
        private FirstBoss bossReference; // Reference to the boss
        private int stoneIndex; // This stone's index in the boss's array
        private Animator anim; // Animator reference
        
        private static readonly int ColorPropertyId = Shader.PropertyToID("_Color");
        private static readonly int Deactivate = Animator.StringToHash("Deactivate");
        
        // Gets whether a slime is hidden under this stone
        public bool HasSlime => slimeTransform != null;
        
        // Gets the slime transform hidden under this stone (can be null)
        public Transform SlimeTransform => slimeTransform;

        private void Awake()
        {
            // Get renderer from children (like slimes and boss)
            stoneRenderer = GetComponentInChildren<Renderer>();
            if (stoneRenderer != null)
            {
                propertyBlock = new MaterialPropertyBlock();
                // Get original color from material
                originalColor = stoneRenderer.sharedMaterial.color;
            }

            stoneCollider = GetComponentInChildren<Collider>();
            if (stoneCollider != null)
                originalTag = stoneCollider.tag;
        }

        private void Start()
        {
            basePosition = transform.position;
            anim = GetComponent<Animator>();
        }
        
        // Initialize the stone with a slime underneath (or null for empty stone)
        public void Initialize(Transform slime, Vector3 startPosition)
        {
            slimeTransform = slime;
            basePosition = startPosition;
            transform.position = startPosition;
            
            // If there's a slime, hide it by setting all its renderers inactive
            if (slimeTransform != null)
            {
                Renderer[] slimeRenderers = slimeTransform.GetComponentsInChildren<Renderer>();
                foreach (var slimeRenderer in slimeRenderers)
                {
                    slimeRenderer.enabled = false;
                }
            }
        }
        
        // Set the boss reference and stone index for player interaction
        public void SetBossReference(FirstBoss boss, int index)
        {
            bossReference = boss;
            stoneIndex = index;
        }
        
        // Set the swap speed/duration for this stone
        public void SetSwapSpeed(float duration)
        {
            moveDuration = duration;
        }
        
        // Make stone counterable (green highlight) - called after shuffle completes
        public void MakeCounterable()
        {
            if (!isMoving)
            {
                isCounterable = true;
                
                if (stoneCollider != null)
                    stoneCollider.tag = "Counterable";
                
                if (stoneRenderer != null && propertyBlock != null)
                {
                    stoneRenderer.GetPropertyBlock(propertyBlock);
                    propertyBlock.SetColor(ColorPropertyId, Color.green);
                    stoneRenderer.SetPropertyBlock(propertyBlock);
                }
                
                // Show the slime now so it appears to be behind the stone during reveal
                if (slimeTransform != null)
                {
                    // Re-enable all child renderers (some slimes might have multiple)
                    Renderer[] slimeRenderers = slimeTransform.GetComponentsInChildren<Renderer>();
                    foreach (var slimeRenderer in slimeRenderers)
                    {
                        slimeRenderer.enabled = true;
                    }
                    
                    // Make sure slime is at the correct position
                    Vector3 slimePos = slimeTransform.position;
                    slimeTransform.position = new Vector3(basePosition.x, slimePos.y, basePosition.z);
                }
            }
        }
        
        // Remove counterable status
        public void RemoveCounterable()
        {
            isCounterable = false;
            
            if (stoneCollider != null)
                stoneCollider.tag = originalTag;
            
            if (stoneRenderer != null && propertyBlock != null)
            {
                stoneRenderer.GetPropertyBlock(propertyBlock);
                propertyBlock.SetColor(ColorPropertyId, originalColor);
                stoneRenderer.SetPropertyBlock(propertyBlock);
            }
        }
        
        // Handle player kicking/countering this stone (ICounterable implementation)
        public void TriggerCounter()
        {
            if (isCounterable && !isMoving)
            {
                // Notify boss immediately so it can turn all stones gray
                if (bossReference != null)
                {
                    bossReference.OnStoneKicked(stoneIndex);
                }
                
                // Player kicked this stone - reveal it and notify boss when done!
                StartCoroutine(RevealStone());
            }
        }
        
        // Swap positions with another stone, including their slimes
        public IEnumerator SwapWith(IntermissionStone otherStone)
        {
            if (isMoving || otherStone.isMoving)
                yield break;

            isMoving = true;
            otherStone.isMoving = true;

            Vector3 startPosA = basePosition;
            Vector3 startPosB = otherStone.basePosition;
            
            // Swap base positions (where each stone should end up)
            basePosition = startPosB;
            otherStone.basePosition = startPosA;
            
            // Calculate perpendicular offset direction to make stones arc to the side
            Vector3 swapDirection = (startPosB - startPosA).normalized;
            Vector3 perpendicularOffset = Vector3.Cross(swapDirection, Vector3.up) * 1.5f; // Offset to the side

            float elapsedTime = 0f;

            while (elapsedTime < moveDuration)
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / moveDuration;
                float curveValue = moveCurve.Evaluate(t);

                // Calculate arc movement with different heights AND horizontal offsets
                float heightOffsetA = Mathf.Sin(t * Mathf.PI) * moveHeight;
                float heightOffsetB = Mathf.Sin(t * Mathf.PI) * (moveHeight * 0.7f);
                
                // Calculate lateral offset (creates a curved path to avoid collision)
                // One stone curves left, the other curves right
                float lateralCurve = Mathf.Sin(t * Mathf.PI); // 0 at start/end, peaks at middle
                Vector3 lateralOffsetA = perpendicularOffset * lateralCurve;
                Vector3 lateralOffsetB = -perpendicularOffset * lateralCurve; // Opposite direction

                // Move this stone from A to B (higher arc, curves to one side)
                Vector3 newPosA = Vector3.Lerp(startPosA, startPosB, curveValue);
                newPosA.y += heightOffsetA;
                newPosA += lateralOffsetA;
                transform.position = newPosA;

                // Move other stone from B to A (lower arc, curves to other side)
                Vector3 newPosB = Vector3.Lerp(startPosB, startPosA, curveValue);
                newPosB.y += heightOffsetB;
                newPosB += lateralOffsetB;
                otherStone.transform.position = newPosB;

                yield return null;
            }

            // Ensure final positions are exact
            transform.position = basePosition;
            otherStone.transform.position = otherStone.basePosition;

            isMoving = false;
            otherStone.isMoving = false;
        }
        
        // Trigger the reveal animation - can be called by boss to reveal all stones
        public void TriggerReveal()
        {
            StartCoroutine(RevealStone(false)); // false = don't notify boss
        }
        
        // Lift the stone to reveal what's underneath
        private IEnumerator RevealStone(bool notifyBoss = true)
        {
            float liftDuration = 1f;
            float elapsedTime = 0f;
            Vector3 startPos = transform.position;
            Vector3 targetPos = startPos + Vector3.up * (moveHeight * 2);

            // Lift the stone (slime is already visible behind it)
            while (elapsedTime < liftDuration)
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / liftDuration;
                transform.position = Vector3.Lerp(startPos, targetPos, t);
                yield return null;
            }

            TriggerDeactivate();
            
            // Notify boss that reveal is complete (only when player kicks stone)
            if (notifyBoss && bossReference != null)
            {
                bossReference.OnStoneRevealed(stoneIndex);
            }
        }
        
        // Trigger the deactivate animation for this stone
        public void TriggerDeactivate() => anim.SetTrigger(Deactivate);
        
        // Destroy this stone - called from animation event at the end of deactivate animation
        private void DestroyStone() => Destroy(gameObject);
    }
}
