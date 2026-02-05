using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

namespace Bosses.First_Boss
{
    [Serializable]
    public class RockShowerPattern
    {
        public GameObject indicator;
        public Transform[] rocksPositionUp;
        public Transform[] rocksPositionDown;
    }
    
    public class FirstBoss : BossController
    {
        [Header("")] 
        [Header("Jump Attack")]
        public int earthShatterDamage;
        [SerializeField] private GameObject _jumpingAttackParticlesPrefab;
        [SerializeField] private BossValuesRange _jumpAttackDistance;
        [SerializeField] private GameObject[] _tripleSmashParticlesPrefab;

        [Header("Cataclysm")] 
        [SerializeField] private int _cataclysmProjectileCount;
        [SerializeField] private float _cataclysmProjectileSpeed;
        
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
        
        [Header("Rock Shower")] 
        [SerializeField] private float _rockShowerSpeed;
        [SerializeField] private RockShowerPattern[] _rockShowerPatterns;

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
        private RockShowerPattern _selectedRockShowerPattern;
        private int _consecutiveNormalAttacks;

        private static readonly int Jump_Attack = Animator.StringToHash("JumpAttack");
        private static readonly int Triple_Smash = Animator.StringToHash("TripleSmash");
        private static readonly int Orbs = Animator.StringToHash("Orbs");
        private static readonly int Fast_Run = Animator.StringToHash("FastRun");
        private static readonly int Melee_Attack = Animator.StringToHash("MeleeAttack");
        private static readonly int Rock_Throw = Animator.StringToHash("RockThrow");
        private static readonly int Frontal_Attack = Animator.StringToHash("FrontalAttack");
        private static readonly int Meteors = Animator.StringToHash("Meteors");
        private static readonly int Rock_Shower = Animator.StringToHash("RockShower");
        private static readonly int Cataclysm = Animator.StringToHash("Cataclysm");

        /// *** Unity Events *** ///

        protected override void Start()
        {
            base.Start();
            _originalRocksToThrow = _rocksToThrow;
        }

        /// *** Base Methods *** ///
        
        protected override IEnumerator EnterSecondPhase()
        {
            yield return base.EnterSecondPhase();
            
            // Reset consecutive attack counter when entering phase 2
            _consecutiveNormalAttacks = 0;
            
            // Halve the time between attacks for more aggressive second phase (2x faster attacks)
            timeBetweenAttacks /= 2f;
            auxTimeBetweenAttacks /= 2f;
            
            // Double the fast run speed for more aggressive chase
            fastRunSpeed *= 2f;
            
            // Double the navMeshAgent speed for faster movement
            navMeshAgent.speed *= 2f;
            navMeshOriginalSpeed *= 2f;
            
            // Add two more rocks to base amount for second phase
            _originalRocksToThrow += 2;
            _rocksToThrow = Random.Range(_originalRocksToThrow - 1, _originalRocksToThrow + 2);
        }

        protected override void PerformAttack()
        {
            base.PerformAttack();
            
            // Check if it's time to trigger Cataclysm
            if (_consecutiveNormalAttacks >= 3)
                StartCataclysm();
            else
            {
                // Randomly select an attack, but never the same as the last one
                int attackIndex;
                do
                {
                    attackIndex = Random.Range(0, 6); // 6 different attacks
                    
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
                    case 5:
                        StartRockShower();
                        break;
                }
                
                // Increment counter after a normal attack
                _consecutiveNormalAttacks++;
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

        /// *** Skill 2-1: Cataclysm *** ///
        
        private void StartCataclysm()
        {
            // Reset the attack counter after triggering Cataclysm
            _consecutiveNormalAttacks = 0;
            
            // Speed up Cataclysm animation in phase 2 (1.5x faster)
            if (IsInSecondPhase)
                anim.speed = 1.5f;
            
            // Trigger the Cataclysm animation
            anim.SetTrigger(Cataclysm);
        }
        
        // Called from animation event - shoots projectiles in a circular pattern around the boss
        public void CataclysmCircleAttack()
        {
            // Reset animation speed to normal once Cataclysm attack appears
            if (IsInSecondPhase)
                anim.speed = 1f;
            
            StartCoroutine(CataclysmCircleAttackCoroutine());
        }
        
        private IEnumerator CataclysmCircleAttackCoroutine()
        {
            float angleStep = 360f / _cataclysmProjectileCount;
            Vector3 bossPosition = transform.position;
            Vector3 spawnPosition = new Vector3(bossPosition.x, bossPosition.y + 2f, bossPosition.z);
            
            // Pre-calculate all directions once (optimization)
            Vector3[] directions = new Vector3[_cataclysmProjectileCount];
            for (int i = 0; i < _cataclysmProjectileCount; i++)
            {
                float angle = i * angleStep;
                float angleInRadians = angle * Mathf.Deg2Rad;
                directions[i] = new Vector3(Mathf.Cos(angleInRadians), 0, Mathf.Sin(angleInRadians)).normalized;
            }
            
            // Shoot 3 waves of rocks with 0.5s delay between waves
            for (int j = 0; j < 3; j++)
            {
                // Shoot all rocks in circle pattern using pre-calculated directions
                for (int i = 0; i < _cataclysmProjectileCount; i++)
                {
                    ShootRockFromPosition(spawnPosition, directions[i], _cataclysmProjectileSpeed);
                }
                
                // Wait 0.5s before next wave (except after the last wave)
                if (j < 2)
                    yield return new WaitForSeconds(0.5f);
            }
            
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

        // Used in Standing Melee Combo Attack animation
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
        
        // Used in Standing Melee Combo Attack animation
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
                _rocksToThrow = Random.Range(_originalRocksToThrow - 1, _originalRocksToThrow + 2);
        }
        
        private void RockThrow()
        {
            LookAtWithVariance(player, _rockAimVariance);
            TriggerSkillWithIndicator(Rock_Throw, skillIndicator: _rockThrowSkillIndicator);
        }

        // Used in Standing Rock Throw animation
        private void ShootRock()
        {
            ShootRockFromPosition(_rockShootPosition.position, _rockShootPosition.forward, _rockSpeed);
            _rocksToThrow -= 1;
            StartThrowingRocks();
        }

        private void ShootRockFromPosition(Vector3 position, Vector3 direction, float speed, float ignoreCollisionDuration = 0f)
        {
            GameObject rock = Instantiate(_rock, position, Quaternion.LookRotation(direction));
            var bulletScript = rock.GetComponentInChildren<StoneProjectile>();
            bulletScript.SetIgnoreCollisionDuration(ignoreCollisionDuration);
            bulletScript.Shoot(speed, rock.transform.position, direction);
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

        /// *** Skill 7-1: Skill Rock Shower *** ///
        private void StartRockShower()
        {
            _selectedRockShowerPattern = IsInSecondPhase ? 
                _rockShowerPatterns[Random.Range(0, _rockShowerPatterns.Length)] : _rockShowerPatterns[Random.Range(0, 2)];
            TriggerSkillWithIndicator(Rock_Shower, skillIndicator: _selectedRockShowerPattern.indicator);
        }

        private void RockShower() 
        {
            Vector3 spawnPosition;
            foreach(var rockPosition in _selectedRockShowerPattern.rocksPositionUp)
            {
                if (_selectedRockShowerPattern.indicator.name.Contains("Vertical"))
                    spawnPosition = rockPosition.position + new Vector3(7f, 1f, 7f);
                else
                    spawnPosition = rockPosition.position + new Vector3(-7f, 1f, 7f);
                ShootRockFromPosition(spawnPosition, rockPosition.up, _rockShowerSpeed, 0.15f);
            }
            foreach(var rockPosition in _selectedRockShowerPattern.rocksPositionDown)
            {
                if (_selectedRockShowerPattern.indicator.name.Contains("Vertical"))
                    spawnPosition = rockPosition.position + new Vector3(-7f, 1f, -7f);
                else
                    spawnPosition = rockPosition.position + new Vector3(7f, 1f, -7f);
                ShootRockFromPosition(spawnPosition, rockPosition.up, _rockShowerSpeed, 0.15f);
            }
        }
    }
}