using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Bosses.First_Boss.Slime;
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
        
        [Header("Intermission")]
        [SerializeField] private Transform _intermissionCenterPosition;
        [SerializeField] private Transform[] _slimePositions; // 4 positions in the scene
        [SerializeField] private SlimeController[] _slimes; // The slime controllers to move
        [SerializeField] private GameObject _rockShellPrefab; // Rock prefab for shell game
        [SerializeField] private float _rockYOffset; // Y offset to align rocks with slimes
        [SerializeField] private int _shellGameSwapCount; // Number of swaps to perform
        [SerializeField] private float _swapSpeed; // Duration of each swap animation (lower = faster)
        [SerializeField] [Range(0, 100)] private int _singleSwapChance; // % chance for single swap
        [SerializeField] [Range(0, 100)] private int _parallelSwapChance; // % chance for parallel swap (remainder = chain rotation)
        
        private int _tripleSmashCount;
        private int _originalRocksToThrow;
        private bool _isJumpingAttacking;
        private bool _isTripleSmashAttacking;
        private bool _isOrbsCasted;
        private bool _areMeteorsActive;
        private int _lastAttackIndex = -1;
        private RockShowerPattern _selectedRockShowerPattern;
        private int _consecutiveNormalAttacks;
        
        // Shell game tracking
        private IntermissionStone[] _intermissionStones; // All 4 stones
        private int _kickedStoneIndex; // Track which stone was kicked for punishment

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
        private static readonly int Grab_Slimes = Animator.StringToHash("GrabSlimes");
        private static readonly int BackFlip = Animator.StringToHash("BackFlip");
        private static readonly int IntermissionPunish = Animator.StringToHash("IntermissionPunish");
        private static readonly int RevealSlimeStones = Animator.StringToHash("RevealSlimeStones");

        /// *** Unity Events *** ///

        protected override void Start()
        {
            base.Start();
            _originalRocksToThrow = _rocksToThrow;
        }

        /// *** Base Methods *** ///
        
        protected override IEnumerator EnterSecondPhase()
        {
            // Boss enters "Agony" animation
            yield return base.EnterSecondPhase();
            
            // Make boss immune to damage during phase 2 intermission
            isImmuneToDamage = true;
            
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
            
            // Intermission slimes preparation
            foreach (SlimeController slime in _slimes)
            {
                slime.SetDamageImmunity(true);
                // quickly resets all slimes to idle animation
                slime.SetAnimationSpeed(10f);
            }
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
            navMeshAgent.isStopped = false;
            navMeshAgent.speed = fastRunSpeed;
            anim.SetTrigger(Fast_Run);
            ResetParticlesToParent(_firstMeleeParticles.transform.parent.gameObject);
            
            while (navMeshAgent.remainingDistance > stopBetweenPlayer)
            {
                navMeshAgent.SetDestination(player.position);
                yield return null;
            }
            
            navMeshAgent.isStopped = true;
            anim.SetBool(Walking, false);
            StartMeleeAttack();
        }

        private void StartMeleeAttack()
        {
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
        
        /// *** Phase 2 Intermission *** ///
        
        // Called in Agony animation (phase 2 start animation)
        private IEnumerator GoToCenterForIntermission()
        {
            // Get the center position from the assigned transform
            Vector3 center = _intermissionCenterPosition.position;
            
            // Look towards the center
            Vector3 directionToCenter = center - transform.position;
            if (directionToCenter != Vector3.zero)
                transform.rotation = Quaternion.LookRotation(directionToCenter);
            
            // Start walking to center (use normal speed)
            navMeshAgent.isStopped = false;
            anim.SetBool(Walking, true);
            navMeshAgent.SetDestination(center);
            
            // Wait until boss reaches the center using manual distance check
            while (Vector3.Distance(transform.position, center) > 0.1f)
            {
                yield return null;
            }
            
            navMeshAgent.isStopped = true;
            anim.SetBool(Walking, false);
            anim.SetTrigger(Grab_Slimes);

            // Rotate boss to look down (south)
            Quaternion lookDownRotation = Quaternion.Euler(0, 220, 0);
            float rotationDuration = 0.5f;
            float elapsedTime = 0f;
            Quaternion startRotation = transform.rotation;
            
            while (elapsedTime < rotationDuration)
            {
                elapsedTime += Time.deltaTime;
                transform.rotation = Quaternion.Slerp(startRotation, lookDownRotation, elapsedTime / rotationDuration);
                yield return null;
            }

            foreach (var slime in _slimes)
                slime.SetSlimeDizzyLoop();
        }
        
        // Move slimes to random positions (3 slimes to 3 of 4 positions, leaving one empty)
        private IEnumerator MoveSlimesToPositions()
        {
            // Create a list of available positions (0, 1, 2, 3)
            List<int> availablePositions = new List<int> { 0, 1, 2, 3 };
            
            // Shuffle and select 3 positions (one will remain empty)
            for (int i = availablePositions.Count - 1; i > 0; i--)
            {
                int randomIndex = Random.Range(0, i + 1);
                (availablePositions[i], availablePositions[randomIndex]) = (availablePositions[randomIndex], availablePositions[i]);
            }
            
            // Take only the first 3 positions (leaving one empty)
            List<int> selectedPositions = availablePositions.GetRange(0, 3);
            
            // Move each slime to its assigned position using SlimeController
            List<Coroutine> movementCoroutines = new List<Coroutine>();
            for (int i = 0; i < _slimes.Length && i < selectedPositions.Count; i++)
            {
                SlimeController slimeController = _slimes[i];
                Transform targetPosition = _slimePositions[selectedPositions[i]];
                movementCoroutines.Add(StartCoroutine(slimeController.MoveToIntermissionPosition(targetPosition.position, 20)));
            }
            
            // Wait for all slimes to reach their positions
            foreach (var coroutine in movementCoroutines)
                yield return coroutine;
            
            // After all slimes have moved and rotated, levitate them up
            List<Coroutine> levitationCoroutines = new List<Coroutine>();
            foreach (var slimeController in _slimes)
                levitationCoroutines.Add(StartCoroutine(slimeController.LevitateUp(0.6f, 1.5f)));
            
            // Wait for all slimes to finish levitating
            foreach (var coroutine in levitationCoroutines)
                yield return coroutine;
            
            // Trigger BackFlip animation on the boss
            anim.SetTrigger(BackFlip);
        }
        
        // Called from BackFlip animation - converts all slimes into rocks for shell game
        public void ConvertSlimesToRocks()
        {
            _intermissionStones = new IntermissionStone[4];
            
            // Track the Y position from slimes for consistency
            float slimeYPosition = _slimes[0].transform.position.y;
            
            // Create a mapping of which slimes are at which positions
            bool[] positionHasSlime = new bool[4];
            int[] slimeAtPosition = new int[4]; // Index of slime at each position (-1 if none)
            
            for (int i = 0; i < 4; i++)
            {
                slimeAtPosition[i] = -1;
                positionHasSlime[i] = false;
            }
            
            // Find which slimes are at which positions
            for (int i = 0; i < _slimes.Length; i++)
            {
                SlimeController slime = _slimes[i];
                for (int j = 0; j < _slimePositions.Length; j++)
                {
                    // Check if slime is close to this position (within 1 unit on XZ plane)
                    Vector3 slimePos = slime.transform.position;
                    Vector3 positionPos = _slimePositions[j].position;
                    float distance = Vector3.Distance(new Vector3(slimePos.x, 0, slimePos.z), new Vector3(positionPos.x, 0, positionPos.z));
                    
                    if (distance < 1f)
                    {
                        positionHasSlime[j] = true;
                        slimeAtPosition[j] = i;
                        break;
                    }
                }
            }
            
            // Instantiate rocks at all 4 positions
            for (int i = 0; i < 4; i++)
            {
                Vector3 rockPosition;
                Transform slimeTransform = null;
                
                if (positionHasSlime[i])
                {
                    // This position has a slime - use its actual position
                    slimeTransform = _slimes[slimeAtPosition[i]].transform;
                    rockPosition = new Vector3(slimeTransform.position.x, slimeYPosition + _rockYOffset, slimeTransform.position.z);
                }
                else
                    // This position is empty - use the marker position
                    rockPosition = new Vector3(_slimePositions[i].position.x, slimeYPosition + _rockYOffset, _slimePositions[i].position.z);
                
                // Instantiate the rock
                GameObject rockObject = Instantiate(_rockShellPrefab, rockPosition, Quaternion.identity);
                IntermissionStone stone = rockObject.GetComponent<IntermissionStone>();
                
                // Initialize the stone with its slime (or null if empty)
                stone.Initialize(slimeTransform, rockPosition);
                stone.SetBossReference(this, i); // Set boss reference for player interaction
                stone.SetSwapSpeed(_swapSpeed); // Set the swap animation speed
                _intermissionStones[i] = stone;
            }
        }
        
        /// *** Shell Game Logic *** ///
        
        // Start the shell game from dance animation
        private IEnumerator StartShellGame()
        {
            yield return ShuffleStones();
            
            // After shuffle completes, make all stones counterable (green)
            foreach (var stone in _intermissionStones)
            {
                if (stone != null)
                    stone.MakeCounterable();
            }
            
            // Now wait for player to kick stones
            // The stones will call OnStoneKicked() when the player kicks them
        }
        
        // Perform the shell game shuffle - various swap patterns for visual complexity
        private IEnumerator ShuffleStones()
        {
            for (int swapIndex = 0; swapIndex < _shellGameSwapCount; swapIndex++)
            {
                // First 2 swaps are always simple single swaps
                if (swapIndex < 2)
                {
                    // Single swap - classic shell game move
                    int stoneA = Random.Range(0, 4);
                    int stoneB;
                    do
                    {
                        stoneB = Random.Range(0, 4);
                    } while (stoneB == stoneA);
                    
                    yield return _intermissionStones[stoneA].SwapWith(_intermissionStones[stoneB]);
                    
                    // Add small delay after first two swaps to make them clearly separate
                    yield return new WaitForSeconds(0.3f);
                }
                else
                {
                    // After first 2 swaps, randomly choose a swap pattern based on configured probabilities
                    int swapPattern = Random.Range(0, 100);
                    
                    if (swapPattern < _singleSwapChance)
                    {
                        // Single swap - classic shell game move
                        int stoneA = Random.Range(0, 4);
                        int stoneB;
                        do
                        {
                            stoneB = Random.Range(0, 4);
                        } while (stoneB == stoneA);
                        
                        yield return _intermissionStones[stoneA].SwapWith(_intermissionStones[stoneB]);
                    }
                    else if (swapPattern < _singleSwapChance + _parallelSwapChance)
                    {
                        // Parallel swap - two pairs swap simultaneously!
                        // Pick 4 different stones and swap them in pairs
                        List<int> availableStones = new List<int> { 0, 1, 2, 3 };
                        
                        // Pick first pair
                        int stone1 = availableStones[Random.Range(0, availableStones.Count)];
                        availableStones.Remove(stone1);
                        int stone2 = availableStones[Random.Range(0, availableStones.Count)];
                        availableStones.Remove(stone2);
                        
                        // Pick second pair from remaining stones
                        int stone3 = availableStones[0];
                        int stone4 = availableStones[1];
                        
                        // Start both swaps simultaneously
                        Coroutine swap1 = StartCoroutine(_intermissionStones[stone1].SwapWith(_intermissionStones[stone2]));
                        Coroutine swap2 = StartCoroutine(_intermissionStones[stone3].SwapWith(_intermissionStones[stone4]));
                        
                        // Wait for both to complete
                        yield return swap1;
                        yield return swap2;
                    }
                    else
                    {
                        // Chain rotation - 3 stones rotate in a circle (A→B→C→A)
                        List<int> availableStones = new List<int> { 0, 1, 2, 3 };
                        
                        // Pick 3 random stones
                        int stoneA = availableStones[Random.Range(0, availableStones.Count)];
                        availableStones.Remove(stoneA);
                        int stoneB = availableStones[Random.Range(0, availableStones.Count)];
                        availableStones.Remove(stoneB);
                        int stoneC = availableStones[Random.Range(0, availableStones.Count)];
                        
                        // Perform chain rotation: A→B, B→C, C→A (simultaneously!)
                        Coroutine swapAB = StartCoroutine(_intermissionStones[stoneA].SwapWith(_intermissionStones[stoneB]));
                        Coroutine swapBC = StartCoroutine(_intermissionStones[stoneB].SwapWith(_intermissionStones[stoneC]));
                        Coroutine swapCA = StartCoroutine(_intermissionStones[stoneC].SwapWith(_intermissionStones[stoneA]));
                        
                        // Wait for all three to complete
                        yield return swapAB;
                        yield return swapBC;
                        yield return swapCA;
                    }
                }
            }
        }
        
        // Called by IntermissionStone when player kicks it
        public void OnStoneKicked(int stoneIndex)
        {
            // Remove counterable from ALL stones (player can only choose one)
            foreach (var stone in _intermissionStones)
            {
                if (stone != null)
                    stone.RemoveCounterable();
            }
            // Store the kicked stone index for later use in animation event
            _kickedStoneIndex = stoneIndex;
        }
        
        // Called by IntermissionStone after the reveal animation completes
        public void OnStoneRevealed(int stoneIndex)
        { 
            if (_intermissionStones[stoneIndex].HasSlime)
                // Trigger boss punishment animation which will call "ExecutePunishment"
                anim.SetTrigger(IntermissionPunish);
            else
                // Trigger boss animation to reveal remaining stones
                anim.SetTrigger(RevealSlimeStones);
        }
        
        // Called from IntermissionPunish animation event - destroys stone and explodes slime
        public void ExecutePunishment()
        {
            IntermissionStone kickedStone = _intermissionStones[_kickedStoneIndex];
            
            if (kickedStone == null)
                return;
            
            // Get the slime before triggering deactivation
            Transform revealedSlime = kickedStone.SlimeTransform;
            
            // Trigger stone deactivate animation (stone will be destroyed via animation event)
            kickedStone.TriggerDeactivate();
            
            // Explode the revealed slime
            if (revealedSlime != null)
            {
                SlimeController slimeController = revealedSlime.GetComponent<SlimeController>();
                if (slimeController != null)
                    slimeController.StartExplosion();
            }
        }
        
        // Called from RevealSlimeStones animation event - lifts all remaining stones to reveal slimes
        public void LiftRemainingStones()
        {
            // Trigger reveal animation for all remaining stones
            foreach (var stone in _intermissionStones)
            {
                if (stone != null)
                {
                    stone.TriggerReveal();
                }
            }
        }

        // Called from move slimes back to spawn animation
        private IEnumerator MoveSlimesBackToSpawnCoroutine()
        {
            List<Coroutine> movementCoroutines = new List<Coroutine>();
            // Move all slimes back to their spawn positions quickly
            foreach (var slime in _slimes)
                movementCoroutines.Add(StartCoroutine(slime.MoveBackToSpawn(80f)));
            // Wait for all slimes to reach spawn
            foreach (var coroutine in movementCoroutines)
                yield return coroutine;
        }
        
        // End the intermission phase - called in warming up animation
        private void EndIntermission()
        {
            // Reset boss state to resume combat
            isImmuneToDamage = false;
            StopPerformingAction();
        }
    }
}

