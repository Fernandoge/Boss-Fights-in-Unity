using System;
using System.Collections;
using System.Collections.Generic;
using Manager.GameManager;
using Shared;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

namespace Bosses.First_Boss
{
    public class FirstBoss : BossController
    {
        [Header("")] 
        [Header("Jump Attack")]
        public int earthShatterDamage;
        [SerializeField] private GameObject _jumpingAttackParticlesPrefab;
        [SerializeField] private GameObject[] _tripleSmashParticlesPrefab;
        [SerializeField] private BossValuesRange _jumpAttackDistance;

        [Header("Orbs")] 
        public int orbsDamage;
        public GameObject orbsPrefab;
        
        [Header("Fast Run Melees")] 
        public int meleesDamage;
        [SerializeField] private GameObject _meleeHitIndicator;
        [SerializeField] private GameObject _lastMeleeHitIndicator;
        [SerializeField] private GameObject _firstMeleeParticles;
        [SerializeField] private GameObject _SecondMeleeParticles;
        [SerializeField] private GameObject _lastMeleeParticles;

        [Header("Rock Throwing")] 
        public int rockDamage;
        [SerializeField] private GameObject _rock;
        [SerializeField] private GameObject _rockThrowSkillIndicator;
        [SerializeField] private Transform _rockShootPosition;
        [SerializeField] private float _rockSpeed;
        [SerializeField] private int _rocksToThrow;
        [SerializeField] private float _rockAimVariance;

        [Header("Frontal Attack")] 
        [SerializeField] private GameObject _frontalAttackParticles;
        [SerializeField] private GameObject _frontalAttackSkillIndicator;
        [SerializeField] private float _frontalAimVariance;
        
        private int _tripleSmashCount;
        private int _originalRocksToThrow;
        private bool _isJumpingAttacking;
        private bool _isTripleSmashAttacking;
        private bool _isOrbsCasted;

        private static readonly int Jump_Attack = Animator.StringToHash("JumpAttack");
        private static readonly int Triple_Smash = Animator.StringToHash("TripleSmash");
        private static readonly int Orbs = Animator.StringToHash("Orbs");
        private static readonly int Fast_Run = Animator.StringToHash("FastRun");
        private static readonly int Melee_Attack = Animator.StringToHash("MeleeAttack");
        private static readonly int Rock_Throw = Animator.StringToHash("RockThrow");
        private static readonly int Frontal_Attack = Animator.StringToHash("FrontalAttack");

        /// *** Unity Events *** ///

        protected override void Start()
        {
            base.Start();
            _originalRocksToThrow = _rocksToThrow;
        }

        /// *** Base Methods *** ///

        protected override void PerformAttack()
        {
            base.PerformAttack();
            // TODO: if HP values
            // REMOVE DEBUG
            _isOrbsCasted = true;
            if (!_isOrbsCasted)
            {
                StartOrbs();
            }
            else
            {
                StartCoroutine(FastRun());
                // StartCoroutine(JumpAttack());
                // StartThrowingRocks();
                // StartFrontalAttack();
            }  
        }

        ///// ******* Skills ******* /////
        
        /// ***** Jump Attack ***** ///

        private IEnumerator JumpAttack()
        {
            ResetParticlesToParent(_jumpingAttackParticlesPrefab);
            transform.LookAt(player);
            anim.SetTrigger(Jump_Attack);
            _jumpingAttackParticlesPrefab.SetActive(false);
            yield return new WaitUntil(() => _isJumpingAttacking);
            playerNavMeshAgent.obstacleAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance;
            var jumpAttackDistance = Random.Range(_jumpAttackDistance.minValue, _jumpAttackDistance.maxValue);
            while (_isJumpingAttacking)
            {
                var bossTransform = transform;
                bossTransform.position += bossTransform.forward * (Time.deltaTime * jumpAttackDistance);
                yield return null; 
            }
            playerNavMeshAgent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
        }

        public void JumpAttackStart() => _isJumpingAttacking = true;
    
        public void JumpAttackEnd() => _isJumpingAttacking = false;

        // Used in Mutant Jump Attack
        public void ActivateJumpingAttackParticles()
        {
            _jumpingAttackParticlesPrefab.SetActive(true);
            _jumpingAttackParticlesPrefab.transform.parent = transform.parent;
        }

        // Used in Mutant Jump Attack
        public void TriggerTripleSmash() => StartTripleSmash();
    
        /// *** Phase 2: Triple Smash *** ///

        private void StartTripleSmash()
        {
            // TODO: if phase 2
            _tripleSmashCount = 0;
            foreach (GameObject particlePrefab in _tripleSmashParticlesPrefab)
            {
                ResetParticlesToParent(particlePrefab);
            }
            StartCoroutine(TripleSmash()); 
        }
    
        private IEnumerator TripleSmash()
        {
            if (_tripleSmashCount == 3)
                yield break;

            _isTripleSmashAttacking = true;
            _tripleSmashParticlesPrefab[_tripleSmashCount].SetActive(false);
            StartCoroutine(TripleSmashRotateOverTime());
            anim.SetTrigger(Triple_Smash);
            yield return new WaitUntil(() => _isTripleSmashAttacking == false);
            _tripleSmashCount++;    
        
            StartCoroutine(TripleSmash());
        }
    
        private IEnumerator TripleSmashRotateOverTime()
        {
            var targetRotation = transform.rotation * Quaternion.Euler(0f, 90f, 0f);
            while (Quaternion.Angle(transform.rotation, targetRotation) > 0.01f)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 12);
                yield return null;
            }
            transform.rotation = targetRotation;
        }
    
        public void TripleSmashEnd() => _isTripleSmashAttacking = false;                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                       
    
        public void ActivateTripleSmashParticles()
        {
            _tripleSmashParticlesPrefab[_tripleSmashCount].SetActive(true);
            _tripleSmashParticlesPrefab[_tripleSmashCount].transform.parent = transform.parent;
        }

        /// *** Orbs *** ///
        
        private void StartOrbs()
        {
            anim.SetTrigger(Orbs);
        }

        private void ActivateOrbs()
        {
            orbsPrefab.SetActive(false);
            orbsPrefab.SetActive(true);
            _isOrbsCasted = true;
        }
        
        /// *** FastRun and Melee Attack *** ///
        
        private IEnumerator FastRun()
        {
            isPerformingAttack = false;
            navMeshAgent.isStopped = false;
            navMeshAgent.speed = 15;
            anim.SetTrigger(Fast_Run);
            ResetParticlesToParent(_firstMeleeParticles.transform.parent.gameObject);
            yield return new WaitUntil(() => anim.GetBool(Walking) == false);
            StartMeleeAttack();
        }

        private void StartMeleeAttack()
        {
            isPerformingAttack = true;
            navMeshAgent.isStopped = true;
            navMeshAgent.speed = navMeshOriginalSpeed;
            _firstMeleeParticles.transform.parent.parent = transform.parent;
            // We reset the trigger in case Animator transitions to MeleeAttack instantly
            anim.ResetTrigger(Fast_Run);
            TriggerSkillWithIndicator(Melee_Attack, _meleeHitIndicator, _firstMeleeParticles);
        }

        private void StartSecondMelee() => StartCoroutine(SecondMelee());

        private IEnumerator SecondMelee()
        {
            TriggerSkillWithIndicator(0, _meleeHitIndicator, _SecondMeleeParticles);
            Quaternion currentRotation = transform.rotation;
            Quaternion targetRotation = currentRotation * Quaternion.Euler(0, 180, 0);
            while (transform.rotation != targetRotation)
            {
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, 600f * Time.deltaTime);
                yield return null;
            }
        }
        
        private void StartThirdMelee() => StartCoroutine(ThirdMelee());
        
        private IEnumerator ThirdMelee()
        {
            TriggerSkillWithIndicator(0, _lastMeleeHitIndicator, _lastMeleeParticles);
            Quaternion currentRotation = transform.rotation;
            Quaternion targetRotation = currentRotation * Quaternion.Euler(0, 180, 0);
            while (transform.rotation != targetRotation)
            {
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, 600f * Time.deltaTime);
                yield return null;
            }
        }

        /// *** Skill Rock Throwing *** *///
        
        private void StartThrowingRocks()
        {
            if (_rocksToThrow > 0)
                RockThrow();
            else
                _rocksToThrow = _originalRocksToThrow;
        }
        
        private void RockThrow()
        {
            LookAtWithVariance(player, _rockAimVariance);
            TriggerSkillWithIndicator(Rock_Throw, skillIndicator: _rockThrowSkillIndicator);
        }

        private void ShootRock()
        {
            GameObject rock = Instantiate(_rock, _rockShootPosition.position, _rockShootPosition.rotation);
            Vector3 rockPosition = rock.transform.position;
            Vector3 bulletDirection = _rockShootPosition.forward;
            var bulletScript = rock.GetComponentInChildren<Projectile>();
            bulletScript.Shoot(_rockSpeed, rockPosition, bulletDirection);

            _rocksToThrow -= 1;
            StartThrowingRocks();
        }

        /// *** Skill Frontal Attack *** ///
        
        private void StartFrontalAttack()
        {
            LookAtWithVariance(player, _frontalAimVariance);
            TriggerSkillWithIndicator(Frontal_Attack, _frontalAttackSkillIndicator, _frontalAttackParticles);
        }
    }
}