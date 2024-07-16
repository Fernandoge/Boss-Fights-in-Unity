using System;
using System.Collections;
using System.Collections.Generic;
using Manager.GameManager;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Serialization;
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
        
        protected Transform player;
        protected NavMeshAgent navMeshAgent;
        protected NavMeshAgent playerNavMeshAgent;
        protected Animator anim;
        protected float navMeshOriginalSpeed;
        protected bool isPerformingAttack;
        
        private Material meshMaterial;
        private Collider colliderComponent;
        private GameObject currentSkillIndicator;
        private GameObject currentSkillParticles;
        private Coroutine skillToCastCoroutine;
        private Color meshMaterialOriginalColor;
        private string colliderOriginalTag;
        private float auxTimeBetweenAttacks; 
        
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
        }
        
        private void Update()
        {
            if (timeBetweenAttacks <= 0)
                PerformAttack();
            
            if (!isPerformingAttack)
            {
                timeBetweenAttacks -= Time.deltaTime;
                IdleMovement();
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
            colliderComponent.transform.tag = "Counterable";
            meshMaterial.SetColor("_Color", Color.green);
        }
        
        protected void StopCounterWindow() 
        {
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
    }
}
