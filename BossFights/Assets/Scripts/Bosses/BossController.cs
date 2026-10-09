using System;
using System.Collections;
using Interfaces;
using Manager.GameManager;
using Shared;
using UI;
using TMPro;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

namespace Bosses
{
    public class BossController : MonoBehaviour, IDamageableByPlayer, ICounterable
    {
        [Serializable]
        protected class BossValuesRange
        {
            [SerializeField] private float _minValue;
            [SerializeField] private float _maxValue;
            public float minValue => _minValue;
            public float maxValue => _maxValue;
        }
        
        [Header("Basic Values")] 
        [SerializeField] protected float rotationSpeed;
        [SerializeField] protected float stopBetweenPlayer;
        [SerializeField] protected float timeBetweenAttacks;
        
        [Header("Health")]
        [SerializeField] protected int maxHealth = 100;
        // The second phase starts when the health falls to this share of the maximum
        [SerializeField, Range(0.1f, 0.9f)] protected float secondPhaseHealthShare = 0.5f;
        [SerializeField] private TextMeshProUGUI _healthText;

        [Header("Death")]
        // How long the death animation plays before the victory screen freezes the game
        [SerializeField] private float _victoryScreenDelay = 4f;

        [Header("Player Perception")]
        // Skills that aim at the player use PerceivedPlayerPosition, never player.position, so movement skills (dashes, leaps, swaps) cannot be answered instantly: a move faster than the player can run freezes the aim for a moment
        [SerializeField] private float _fastMoveSpeedFactor = 1.3f;
        [SerializeField] private float _fastMoveReactionDelay = 0.8f;

        public bool IsInSecondPhase => hasEnteredSecondPhase;
        public bool IsDead { get; private set; }
        public bool DebugAutoAttacksDisabled { get; set; }
        public bool DebugIsBusy => isPerformingAttack || isPerformingAction;

        private int SecondPhaseHealth => Mathf.RoundToInt(maxHealth * secondPhaseHealthShare);

        protected Vector3 PerceivedPlayerPosition { get; private set; }

        // Where the player is heading, or nothing while the boss is still reacting to a fast move
        protected Vector3 PerceivedPlayerVelocity
        {
            get
            {
                if (_perceptionHoldTimer > 0f)
                    return Vector3.zero;

                Vector3 velocity = playerNavMeshAgent.velocity;
                velocity.y = 0f;
                return velocity;
            }
        }

        protected Transform player;
        protected NavMeshAgent navMeshAgent;
        protected NavMeshAgent playerNavMeshAgent;
        protected Animator anim;
        protected float navMeshOriginalSpeed;
        protected bool isPerformingAttack;
        protected float auxTimeBetweenAttacks;
        protected bool isPerformingAction;
        
        private Material meshMaterial;
        private Collider colliderComponent;
        private CounterPrompt _counterPrompt;
        private GameObject currentSkillIndicator;
        private GameObject currentSkillParticles;
        private Coroutine skillToCastCoroutine;
        private Color meshMaterialOriginalColor;
        private string colliderOriginalTag;
        private int currentHealth;
        private Vector3 _lastPlayerPosition;
        private float _perceptionHoldTimer;
        protected bool hasEnteredSecondPhase;
        private bool isCounterWindowActive;
        protected bool isImmuneToDamage; 
        
        protected static readonly int Walking = Animator.StringToHash("Walking");
        private static readonly int Enter_Second_Phase = Animator.StringToHash("EnterSecondPhase");
        private static readonly int Countered = Animator.StringToHash("Countered");
        private static readonly int Die = Animator.StringToHash("Die");
        private static readonly int PerformingAction = Animator.StringToHash("PerformingAction");

        protected virtual void Start()
        {
            player = GameManager.Instance.player.transform;
            navMeshAgent = GetComponent<NavMeshAgent>();
            playerNavMeshAgent = player.GetComponent<NavMeshAgent>();
            PerceivedPlayerPosition = player.position;
            _lastPlayerPosition = player.position;
            anim = GetComponent<Animator>();
            meshMaterial = GetComponentInChildren<SkinnedMeshRenderer>().material;
            colliderComponent = GetComponentInChildren<Collider>();
            auxTimeBetweenAttacks = timeBetweenAttacks;
            navMeshOriginalSpeed = navMeshAgent.speed;
            meshMaterialOriginalColor = meshMaterial.color;
            colliderOriginalTag = transform.tag;
            
            // Initialize health
            currentHealth = maxHealth;
            _healthText.text = currentHealth.ToString();
        }
        
        private void Update()
        {
            if (IsDead)
                return;

            if (timeBetweenAttacks <= 0 && !DebugAutoAttacksDisabled)
                PerformAttack();

            if (isPerformingAttack || isPerformingAction) 
                return;
            
            IdleMovement();
            timeBetweenAttacks -= Time.deltaTime;
        }
        
        private void LateUpdate() => UpdatePlayerPerception();

        protected virtual void IdleMovement()
        {
            navMeshAgent.SetDestination(player.position);
            if (navMeshAgent.remainingDistance > stopBetweenPlayer)
            {
                anim.SetBool(Walking, true);
                if (anim.GetCurrentAnimatorStateInfo(0).IsName("Walking"))
                    navMeshAgent.isStopped = false;
            }
            else
            {
                navMeshAgent.isStopped = true;
                anim.SetBool(Walking, false);
                Vector3 direction = player.position - transform.position;
                if (direction != Vector3.zero)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(direction);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
                }
            }
        }
        
        /// *** Base Methods *** ///

        public void DebugSetSecondPhaseFlag(bool value) => hasEnteredSecondPhase = value;

        // One point above the second phase line, so the next hit starts the second phase; the phase flag is cleared so it can be repeated
        public void DebugSetHealthJustAboveHalf()
        {
            hasEnteredSecondPhase = false;
            currentHealth = SecondPhaseHealth + 1;
            _healthText.text = currentHealth.ToString();
        }

        // One point of health left, so the next hit kills
        public void DebugSetHealthToOne()
        {
            currentHealth = 1;
            _healthText.text = currentHealth.ToString();
        }

        private void PerformAction()
        {
            isPerformingAction = true;
            navMeshAgent.isStopped = true;
            timeBetweenAttacks = auxTimeBetweenAttacks;
            anim.SetBool(PerformingAction, true);
        }
        
        protected virtual void PerformAttack()
        {
            isPerformingAttack = true;
            navMeshAgent.isStopped = true;
            timeBetweenAttacks = auxTimeBetweenAttacks;
        }
        
        // Used in Idle animation
        protected void StopPerformingAttack()
        {
            if (!anim.GetCurrentAnimatorStateInfo(0).IsTag("WalkingIdle"))
                isPerformingAttack = false;
        }
        
        protected void StopPerformingAction()
        {
            isPerformingAction = false;
            anim.SetBool(PerformingAction, false);
        }
        
        protected void LookAtWithVariance(Transform transformToLookAt, float variance)
        {
            var playerPositionWithVariance = transformToLookAt.position + new Vector3(
                Random.Range(-variance, variance), 0, Random.Range(-variance, variance));
            transform.LookAt(playerPositionWithVariance);
        }
        
        protected void ResetParticlesToParent(GameObject particlesGameObject)
        {
            particlesGameObject.transform.parent = transform;
            particlesGameObject.transform.localPosition = new Vector3();
            particlesGameObject.transform.localRotation = Quaternion.identity;
        }
        
        /// *** Skills with indicator logic *** ///
        
        protected void TriggerSkillWithIndicator(int trigger, GameObject skillIndicator = null, GameObject skillParticles = null)
        {
            currentSkillIndicator = skillIndicator;
            currentSkillParticles = skillParticles;
            if (trigger != 0)
                anim.SetTrigger(trigger);
        }
        
        protected void ActivateSkillIndicator() => currentSkillIndicator.SetActive(true);

        protected void DeactivateSkillIndicator()
        {
            currentSkillIndicator.SetActive(false);
            if (currentSkillParticles)
                skillToCastCoroutine = StartCoroutine(ActivateSkillWithIndicatorParticles());
        }

        private IEnumerator ActivateSkillWithIndicatorParticles()
        {
            // Wait a little bit, so it appears briefly after skill indicator is deactivated
            yield return new WaitForSeconds(0.2f);
            currentSkillParticles.SetActive(false);
            currentSkillParticles.SetActive(true);
        }
        
        /// *** Counter Logic *** ///
        
        protected void ActivateCounterWindow()
        {
            // An animation event of the attack the boss was playing can still arrive during the cross fade into the death animation
            if (IsDead)
                return;

            isCounterWindowActive = true;
            colliderComponent.transform.tag = "Counterable";
            meshMaterial.SetColor("_Color", Color.green);
            if (!_counterPrompt)
                _counterPrompt = CounterPrompt.Show(colliderComponent);
        }
        
        protected void StopCounterWindow() 
        {
            isCounterWindowActive = false;
            colliderComponent.transform.tag = colliderOriginalTag;
            meshMaterial.SetColor("_Color", meshMaterialOriginalColor);
            if (_counterPrompt)
            {
                _counterPrompt.Hide();
                _counterPrompt = null;
            }
        }

        public void TriggerCounter()
        {
            StopCounterWindow();
            StopCoroutine(skillToCastCoroutine);
            currentSkillIndicator.SetActive(false);
            anim.SetTrigger(Countered);
        }
        
        /// *** Health Logic *** ///
        
        // IDamageableByPlayer interface implementation
        public void TakeDamage(int damage, bool isCrit)
        {
            // Don't take damage if immune (e.g., during phase transitions)
            if (isImmuneToDamage || IsDead)
                return;

            currentHealth -= damage;
            DamageNumber.Show(colliderComponent, damage, isCrit);
            _healthText.text = Mathf.Max(currentHealth, 0).ToString();

            if (currentHealth <= 0)
            {
                Defeat();
                return;
            }

            // Check if boss should enter second phase
            if (!hasEnteredSecondPhase && currentHealth <= SecondPhaseHealth && !isPerformingAction)
                StartCoroutine(EnterSecondPhase());
            
            // Don't flash red if counter window is active (already flashing green)
            if (!isCounterWindowActive)
                StartCoroutine(FlashOnHit());
        }
        
        // Override to remove what the boss left on the field when it dies
        protected virtual void OnDefeated()
        {
        }

        private void Defeat()
        {
            IsDead = true;
            StopAllCoroutines();
            StopCounterWindow();
            if (currentSkillIndicator)
                currentSkillIndicator.SetActive(false);
            if (currentSkillParticles)
                currentSkillParticles.SetActive(false);

            navMeshAgent.isStopped = true;
            anim.SetBool(Walking, false);
            anim.SetBool(PerformingAction, false);
            anim.SetTrigger(Die);
            OnDefeated();

            // If the player died on the same hit, the game over screen is already on its way
            if (!GameManager.Instance.player.IsDead)
                GameMenuScreen.ShowVictory(_victoryScreenDelay);
        }

        // Override this method in specific boss implementations to define second phase behavior
        protected virtual IEnumerator EnterSecondPhase()
        {
            // Safety check: prevent multiple coroutines from running if boss takes rapid damage
            // This prevents the race condition where multiple StartCoroutine calls happen
            // before hasEnteredSecondPhase flag is set
            if (hasEnteredSecondPhase)
                yield break;
            
            // Set flag immediately to prevent other coroutines from continuing
            hasEnteredSecondPhase = true;
            
            yield return new WaitUntil(() => !isPerformingAttack && !isPerformingAction);
            PerformAction();
            anim.SetTrigger(Enter_Second_Phase);
        }
        
        private IEnumerator FlashOnHit()
        {
            // Flash red
            meshMaterial.SetColor("_Color", Color.red);
            yield return new WaitForSeconds(0.04f);
            
            // Return to original color
            meshMaterial.SetColor("_Color", meshMaterialOriginalColor);
        }

        // The perceived position follows the player, except for a moment after a move much faster than running
        private void UpdatePlayerPerception()
        {
            Vector3 moved = player.position - _lastPlayerPosition;
            moved.y = 0f;
            _lastPlayerPosition = player.position;

            float deltaTime = Time.deltaTime;
            if (deltaTime > 0f && moved.magnitude / deltaTime > playerNavMeshAgent.speed * _fastMoveSpeedFactor)
                _perceptionHoldTimer = _fastMoveReactionDelay;

            if (_perceptionHoldTimer > 0f)
                _perceptionHoldTimer -= deltaTime;
            else
                PerceivedPlayerPosition = player.position;
        }
    }
}
