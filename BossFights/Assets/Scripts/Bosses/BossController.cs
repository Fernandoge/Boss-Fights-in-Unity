using System;
using System.Collections;
using Manager.GameManager;
using TMPro;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

namespace Bosses
{
    public class BossController : MonoBehaviour
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
        [SerializeField] private TextMeshProUGUI _healthText;

        private int currentHealth;
        private bool hasEnteredSecondPhase;
        
        public bool IsInSecondPhase => hasEnteredSecondPhase;
        
        protected Transform player;
        protected NavMeshAgent navMeshAgent;
        protected NavMeshAgent playerNavMeshAgent;
        protected Animator anim;
        protected float navMeshOriginalSpeed;
        protected bool isPerformingAttack;
        protected bool isTimeBetweenAttacksFrozen;
        
        private Material meshMaterial;
        private Collider colliderComponent;
        private GameObject currentSkillIndicator;
        private GameObject currentSkillParticles;
        private Coroutine skillToCastCoroutine;
        private Color meshMaterialOriginalColor;
        private string colliderOriginalTag;
        protected float auxTimeBetweenAttacks;
        private bool isCounterWindowActive; 
        
        protected static readonly int Walking = Animator.StringToHash("Walking");
        
        protected virtual void Start()
        {
            player = GameManager.Instance.player.transform;
            navMeshAgent = GetComponent<NavMeshAgent>();
            playerNavMeshAgent = player.GetComponent<NavMeshAgent>();
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
            if (timeBetweenAttacks <= 0)
                PerformAttack();
            
            if (!isPerformingAttack)
            {
                IdleMovement();
                if (!isTimeBetweenAttacksFrozen)
                    timeBetweenAttacks -= Time.deltaTime;
            }
        }
        
        private void IdleMovement()
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
        
        protected virtual void PerformAttack()
        {
            isPerformingAttack = true;
            navMeshAgent.isStopped = true;
            timeBetweenAttacks = auxTimeBetweenAttacks;
        }
        
        // Used in Idle animation
        protected void StoppedPerformingAttack()
        {
            if (!anim.GetCurrentAnimatorStateInfo(0).IsTag("WalkingIdle"))
                isPerformingAttack = false;
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
            isCounterWindowActive = true;
            colliderComponent.transform.tag = "Counterable";
            meshMaterial.SetColor("_Color", Color.green);
        }
        
        protected void StopCounterWindow() 
        {
            isCounterWindowActive = false;
            colliderComponent.transform.tag = colliderOriginalTag;
            meshMaterial.SetColor("_Color", meshMaterialOriginalColor);
        }
        
        public void Countered()
        {
            StopCounterWindow();
            StopCoroutine(skillToCastCoroutine);
            currentSkillIndicator.SetActive(false);
            anim.SetTrigger("Countered");
        }
        
        /// *** Health Logic *** ///
        
        public void DamageBoss(int damage)
        {
            currentHealth -= damage;
            _healthText.text = currentHealth.ToString();
            
            // Check if boss should enter second phase
            if (!hasEnteredSecondPhase && currentHealth <= maxHealth / 2)
            {
                hasEnteredSecondPhase = true;
                EnterSecondPhase();
            }
            
            // Don't flash red if counter window is active (already flashing green)
            if (!isCounterWindowActive)
                StartCoroutine(FlashOnHit());
        }
        
        protected virtual void EnterSecondPhase()
        {
            // Override this method in specific boss implementations to define second phase behavior
            Debug.Log($"{gameObject.name} has entered second phase!");
        }
        
        private IEnumerator FlashOnHit()
        {
            // Flash red
            meshMaterial.SetColor("_Color", Color.red);
            yield return new WaitForSeconds(0.04f);
            
            // Return to original color
            meshMaterial.SetColor("_Color", meshMaterialOriginalColor);
        }
    }
}
