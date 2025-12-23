using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Manager.GameManager;
using Shared;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

namespace Bosses.First_Boss
{
    public class FirstBoss : BossController
    {
        [Header("")] 
        [Header("Jump Attack")]
        public int earthShatterDamage;
        [SerializeField] private GameObject _jumpingAttackParticlesPrefab;
        [SerializeField] private BossValuesRange _jumpAttackDistance;
        [SerializeField] private GameObject[] _tripleSmashParticlesPrefab;

        [Header("Orbs")] 
        public int orbsDamage;
        public GameObject orbsPrefab;
        
        [Header("Fast Run Melees")] 
        public int meleesDamage;
        public float fastRunSpeed;
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

        [Header("Meteors")] 
        public int meteorsDamage;
        public float meteorsDuration;
        [SerializeField] private GameObject _meteorPrefab;
        [SerializeField] private GameObject _meteorsBoundaries;
        [SerializeField] private GameObject _meteorsIndicatorParent;
        [SerializeField] private int _meteorsAmount;
        [SerializeField] private float _meteorsMinDistance;
        
        private int _tripleSmashCount;
        private int _originalRocksToThrow;
        private bool _isJumpingAttacking;
        private bool _isTripleSmashAttacking;
        private bool _isOrbsCasted;
        private bool _areMeteorsActive;
        private int _lastAttackIndex = -1;

        private static readonly int Jump_Attack = Animator.StringToHash("JumpAttack");
        private static readonly int Triple_Smash = Animator.StringToHash("TripleSmash");
        private static readonly int Orbs = Animator.StringToHash("Orbs");
        private static readonly int Fast_Run = Animator.StringToHash("FastRun");
        private static readonly int Melee_Attack = Animator.StringToHash("MeleeAttack");
        private static readonly int Rock_Throw = Animator.StringToHash("RockThrow");
        private static readonly int Frontal_Attack = Animator.StringToHash("FrontalAttack");
        private static readonly int Meteors = Animator.StringToHash("Meteors");

        /// *** Unity Events *** ///

        protected override void Start()
        {
            base.Start();
            _originalRocksToThrow = _rocksToThrow;
        }

        /// *** Base Methods *** ///
        
        protected override void EnterSecondPhase()
        {
            base.EnterSecondPhase();
            
            // Halve the time between attacks for more aggressive second phase (2x faster attacks)
            timeBetweenAttacks /= 2f;
            auxTimeBetweenAttacks /= 2f;
            
            // Double the fast run speed for more aggressive chase
            fastRunSpeed *= 2f;
        }

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
                // Randomly select an attack, but never the same as the last one
                int attackIndex;
                do
                {
                    attackIndex = Random.Range(0, 5); // 5 different attacks
                    
                    // If meteors are selected but still active, reroll
                    if (attackIndex == 4 && _areMeteorsActive)
                        continue;
                    
                    // Break if we found a valid attack
                    if (attackIndex != _lastAttackIndex)
                        break;
                        
                } while (true);
                
                _lastAttackIndex = attackIndex;
                
                switch (attackIndex)
                {
                    case 0:
                        StartCoroutine(FastRun());
                        break;
                    case 1:
                        StartCoroutine(JumpAttack());
                        break;
                    case 2:
                        StartThrowingRocks();
                        break;
                    case 3:
                        StartFrontalAttack();
                        break;
                    case 4:
                        StartMeteors();
                        break;
                }
            }  
        }

        ///// ******* Skills ******* /////
        
        /// ***** Skill 1-1: Jump Attack ***** ///
        
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
        public void TriggerTripleSmash()
        {
            // Only trigger triple smash in phase 2
            if (IsInSecondPhase)
                StartTripleSmash();
        }
    
        /// *** Skill 1-2 Triple Smash *** ///

        private void StartTripleSmash()
        {
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

        /// *** Skill 2-1: Orbs *** ///
        
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
        
        /// *** Skill 3-1: FastRun and Melee Attack *** ///
        
        private IEnumerator FastRun()
        {
            isPerformingAttack = false;
            isTimeBetweenAttacksFrozen = true;
            navMeshAgent.isStopped = false;
            navMeshAgent.speed = fastRunSpeed;
            anim.SetTrigger(Fast_Run);
            ResetParticlesToParent(_firstMeleeParticles.transform.parent.gameObject);
            yield return new WaitUntil(() => anim.GetBool(Walking) == false);
            StartMeleeAttack();
        }

        private void StartMeleeAttack()
        {
            isPerformingAttack = true;
            isTimeBetweenAttacksFrozen = false;
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

        /// *** Skill 4-1: Skill Rock Throwing *** *///
        
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
            var bulletScript = rock.GetComponentInChildren<StoneProjectile>();
            bulletScript.Shoot(_rockSpeed, rockPosition, bulletDirection);

            _rocksToThrow -= 1;
            StartThrowingRocks();
        }

        /// *** Skill 5-1: Frontal Attack *** ///
        
        private void StartFrontalAttack()
        {
            LookAtWithVariance(player, _frontalAimVariance);
            TriggerSkillWithIndicator(Frontal_Attack, _frontalAttackSkillIndicator, _frontalAttackParticles);
        }

        /// *** Skill 6-1: Skill Meteors *** ///

        private void StartMeteors()
        {
            _areMeteorsActive = true;
            anim.SetTrigger(Meteors);
        }

        // Used in Standing Yell Animation
        private void InstantiateMeteors()
        {
            var bounds = _meteorsBoundaries.GetComponent<Renderer>().bounds;
            List<Vector3> spawnedPositions = new();
            
            for (var meteorIndex = 0; meteorIndex < _meteorsAmount; meteorIndex++)
            {
                Vector3 randomPosition = default;
                var attemptsFindingMinDistance = 0;
                var validPosition = false;
                
                while (!validPosition && attemptsFindingMinDistance < 100)
                {
                    attemptsFindingMinDistance++;
                    randomPosition = new Vector3(
                        Random.Range(bounds.min.x, bounds.max.x), 0,
                        Random.Range(bounds.min.z, bounds.max.z)
                    );
                    // Check if the new randomPosition is too close to other instantiated meteors
                    validPosition = spawnedPositions.All(pos => !(Vector3.Distance(randomPosition, pos) < _meteorsMinDistance));
                }
                if (attemptsFindingMinDistance == 100)
                    print($"Failed to meet minimum meteor distance criteria of {_meteorsMinDistance}");
                
                Instantiate(_meteorPrefab, randomPosition, Quaternion.identity, _meteorsIndicatorParent.transform);
                spawnedPositions.Add(randomPosition);
            }
            _meteorsIndicatorParent.transform.rotation *= Quaternion.Euler(0, 45, 0);
            
            // Start coroutine to reset flag after meteors duration
            StartCoroutine(ResetMeteorsFlag());
        }
        
        private IEnumerator ResetMeteorsFlag()
        {
            // Wait for meteors to finish
            yield return new WaitForSeconds(meteorsDuration);
            _areMeteorsActive = false;
        }
    }
}