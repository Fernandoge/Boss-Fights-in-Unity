using System;
using System.Collections;
using System.Collections.Generic;
using Characters;
using Manager.GameManager;
using Shared;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

namespace Bosses
{
    public class FirstBoss : MonoBehaviour
    {
        [Serializable]
        private class BossValuesRange
        {
            [SerializeField] private float _minValue;
            [SerializeField] private float _maxValue;
            public float minValue => _minValue;
            public float maxValue => _maxValue;
        }
        
        [Header("Basic Values")] 
        [SerializeField] private float _rotationSpeed;
        [SerializeField] private float _stopBetweenPlayer;
        public float timeBetweenAttacks;
        
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
        public int frontalDamage;
        [SerializeField] private GameObject _frontalAttackParticles;
        [SerializeField] private GameObject _frontalAttackSkillIndicator;
        [SerializeField] private float _frontalAimVariance;
        
        private Transform _player;
        private NavMeshAgent _navMeshAgent;
        private NavMeshAgent _playerNavMeshAgent;
        private Animator _anim;
        private Material _meshMaterial;
        private Collider _collider;
        private GameObject _currentSkillIndicator;
        private GameObject _currentSkillParticles;
        private Coroutine _skillToCastCoroutine;
        private List<IEnumerator> _attacks;
        private Color _meshMaterialOriginalColor;
        private string _colliderOriginalTag;
        private float auxTimeBetweenAttacks; 
        private float _navMeshOriginalSpeed;
        private int _tripleSmashCount;
        private int _originalRocksToThrow;
        private bool _isJumpingAttacking;
        private bool _isTripleSmashAttacking;
        private bool _isOrbsCasted;
        private bool _isPerformingAttack;
    
        private static readonly int Jump_Attack = Animator.StringToHash("JumpAttack");
        private static readonly int Triple_Smash = Animator.StringToHash("TripleSmash");
        private static readonly int Orbs = Animator.StringToHash("Orbs");
        private static readonly int Walking = Animator.StringToHash("Walking");
        private static readonly int Fast_Run = Animator.StringToHash("FastRun");
        private static readonly int Melee_Attack = Animator.StringToHash("MeleeAttack");
        private static readonly int Rock_Throw = Animator.StringToHash("RockThrow");
        private static readonly int Frontal_Attack = Animator.StringToHash("FrontalAttack");

        /// *** Unity Events *** ///
        
        private void Start()
        {
            _player = GameManager.Instance.player.transform;
            _navMeshAgent = GetComponent<NavMeshAgent>();
            _playerNavMeshAgent = _player.GetComponent<NavMeshAgent>();
            _anim = GetComponent<Animator>();
            _meshMaterial = GetComponentInChildren<SkinnedMeshRenderer>().material;
            _collider = GetComponentInChildren<Collider>();
            auxTimeBetweenAttacks = timeBetweenAttacks;
            _navMeshOriginalSpeed = _navMeshAgent.speed;
            _meshMaterialOriginalColor = _meshMaterial.color;
            _colliderOriginalTag = transform.tag;
            _originalRocksToThrow = _rocksToThrow;
        }

        private void Update()
        {
            if (timeBetweenAttacks <= 0)
                PerformAttack();
            
            if (!_isPerformingAttack)
            {
                timeBetweenAttacks -= Time.deltaTime;
                IdleMovement();
            }
        }
        
        /// *** Base Methods *** ///
        
        private void IdleMovement()
        {
            _navMeshAgent.SetDestination(_player.position);
            if (_navMeshAgent.remainingDistance > _stopBetweenPlayer)
            {
                _anim.SetBool(Walking, true);
                if (_anim.GetCurrentAnimatorStateInfo(0).IsName("Walking"))
                    _navMeshAgent.isStopped = false;
            }
            else
            {
                _navMeshAgent.isStopped = true;
                _anim.SetBool(Walking, false);
                Vector3 direction = _player.position - transform.position;
                if (direction != Vector3.zero)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(direction);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * _rotationSpeed);
                }
            }
        }
        
        private void PerformAttack()
        {
            _isPerformingAttack = true;
            _navMeshAgent.isStopped = true;
            timeBetweenAttacks = auxTimeBetweenAttacks;
        
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
    
        // Used in Idle animation
        private void StoppedPerformingAttack()
        {
            if (!_anim.GetCurrentAnimatorStateInfo(0).IsTag("WalkingIdle"))
                _isPerformingAttack = false;
        }
        
        private void LookAtWithVariance(Transform transformToLookAt, float variance)
        {
            var playerPositionWithVariance = transformToLookAt.position + new Vector3(
                Random.Range(-variance, variance), 0, Random.Range(-variance, variance));
            transform.LookAt(playerPositionWithVariance);
        }
        
        private void ResetParticlesToParent(GameObject particlesGameObject)
        {
            particlesGameObject.transform.parent = transform;
            particlesGameObject.transform.localPosition = new Vector3();
            particlesGameObject.transform.localRotation = Quaternion.identity;
        }
        
        /// *** Skills with indicator logic *** ///
        
        private void TriggerSkillWithIndicator(int trigger, GameObject skillIndicator = null, GameObject skillParticles = null)
        {
            _currentSkillIndicator = skillIndicator;
            _currentSkillParticles = skillParticles;
            if (trigger != 0)
                _anim.SetTrigger(trigger);
        }
        
        private void ActivateSkillIndicator() => _currentSkillIndicator.SetActive(true);

        private void DeactivateSkillIndicator()
        {
            _currentSkillIndicator.SetActive(false);
            if (_currentSkillParticles)
                _skillToCastCoroutine = StartCoroutine(ActivateSkillWithIndicatorParticles());
        }

        private IEnumerator ActivateSkillWithIndicatorParticles()
        {
            // Wait a little bit, so it appears briefly after skill indicator is deactivated
            yield return new WaitForSeconds(0.2f);
            _currentSkillParticles.SetActive(false);
            _currentSkillParticles.SetActive(true);
        }
        
        /// *** Counter Logic *** ///
        
        private void ActivateCounterWindow()
        {
            _collider.transform.tag = "Counterable";
            _meshMaterial.SetColor("_Color", Color.green);
        }
        
        private void StopCounterWindow() 
        {
            _collider.transform.tag = _colliderOriginalTag;
            _meshMaterial.SetColor("_Color", _meshMaterialOriginalColor);
        }
        
        public void Countered()
        {
            StopCounterWindow();
            StopCoroutine(_skillToCastCoroutine);
            _currentSkillIndicator.SetActive(false);
            _anim.SetTrigger("Countered");
        }

        ///// ******* Skills ******* /////
        
        /// ***** Jump Attack ***** ///

        private IEnumerator JumpAttack()
        {
            ResetParticlesToParent(_jumpingAttackParticlesPrefab);
            transform.LookAt(_player);
            _anim.SetTrigger(Jump_Attack);
            _jumpingAttackParticlesPrefab.SetActive(false);
            yield return new WaitUntil(() => _isJumpingAttacking);
            _playerNavMeshAgent.obstacleAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance;
            var jumpAttackDistance = Random.Range(_jumpAttackDistance.minValue, _jumpAttackDistance.maxValue);
            while (_isJumpingAttacking)
            {
                var bossTransform = transform;
                bossTransform.position += bossTransform.forward * (Time.deltaTime * jumpAttackDistance);
                yield return null; 
            }
            _playerNavMeshAgent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
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
            _anim.SetTrigger(Triple_Smash);
            yield return new WaitUntil(() => _isTripleSmashAttacking == false);
            _tripleSmashCount++;    
        
            StartCoroutine(TripleSmash());
        }
    
        private IEnumerator TripleSmashRotateOverTime()
        {
            Quaternion targetRotation = transform.rotation * Quaternion.Euler(0f, 90f, 0f);
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
            _anim.SetTrigger(Orbs);
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
            _isPerformingAttack = false;
            _navMeshAgent.isStopped = false;
            _navMeshAgent.speed = 15;
            _anim.SetTrigger(Fast_Run);
            ResetParticlesToParent(_firstMeleeParticles.transform.parent.gameObject);
            yield return new WaitUntil(() => _anim.GetBool(Walking) == false);
            StartMeleeAttack();
        }

        private void StartMeleeAttack()
        {
            _isPerformingAttack = true;
            _navMeshAgent.isStopped = true;
            _navMeshAgent.speed = _navMeshOriginalSpeed;
            _firstMeleeParticles.transform.parent.parent = transform.parent;
            // We reset the trigger in case Animator transitions to MeleeAttack instantly
            _anim.ResetTrigger(Fast_Run);
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
            LookAtWithVariance(_player, _rockAimVariance);
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
            LookAtWithVariance(_player, _frontalAimVariance);
            TriggerSkillWithIndicator(Frontal_Attack, _frontalAttackSkillIndicator, _frontalAttackParticles);
        }
    }
}