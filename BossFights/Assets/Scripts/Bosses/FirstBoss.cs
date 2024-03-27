using System;
using System.Collections;
using System.Collections.Generic;
using Manager.GameManager;
using UnityEngine;
using UnityEngine.AI;
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
        [Header("Skill Values")] 
        public int earthShatterDamage;
        [SerializeField] private BossValuesRange _jumpAttackDistance;
        public int orbsDamage; 
        [Header("Skills Particles")]
        public GameObject orbsPrefab;
        public GameObject jumpingAttackParticlesPrefab;
        public GameObject[] tripleSmashParticlesPrefab;

        private Transform _player;
        private float auxTimeBetweenAttacks;
        private NavMeshAgent _navMeshAgent;
        private NavMeshAgent _playerNavMeshAgent;
        private Animator _anim;
        private List<IEnumerator> _attacks;
        private bool _jumpingAttack;
        private bool _tripleSmashAttack;
        private int _tripleSmashCount;
        private bool _orbsCasted;
        private bool _performingAttack;
        private float _navMeshOriginalSpeed;
        private Material _meshMaterial;
        private Color _meshMaterialOriginalColor;
        private Collider _collider;
        private string _colliderOriginalTag;
    
        private static readonly int Jump_Attack = Animator.StringToHash("JumpAttack");
        private static readonly int Triple_Smash = Animator.StringToHash("TripleSmash");
        private static readonly int Orbs = Animator.StringToHash("Orbs");
        private static readonly int Walking = Animator.StringToHash("Walking");
        private static readonly int Fast_Run = Animator.StringToHash("FastRun");
        private static readonly int Melee_Attack = Animator.StringToHash("MeleeAttack");
        
        private void Start()
        {
            _player = GameManager.Instance.player.transform;
            _navMeshAgent = GetComponent<NavMeshAgent>();
            _playerNavMeshAgent = _player.GetComponent<NavMeshAgent>();
            _anim = GetComponent<Animator>();
            auxTimeBetweenAttacks = timeBetweenAttacks;
            _navMeshOriginalSpeed = _navMeshAgent.speed;
            _meshMaterial = GetComponentInChildren<SkinnedMeshRenderer>().material;
            _meshMaterialOriginalColor = _meshMaterial.color;
            _collider = GetComponentInChildren<Collider>();
            _colliderOriginalTag = transform.tag;
        }

        private void Update()
        {
            if (timeBetweenAttacks <= 0)
                PerformAttack();

            if (!_performingAttack)
            {
                timeBetweenAttacks -= Time.deltaTime;
                IdleMovement();
            }
        }

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
            _performingAttack = true;
            _navMeshAgent.isStopped = true;
            timeBetweenAttacks = auxTimeBetweenAttacks;
        
            // TODO: if HP values
            // REMOVE DEBUG
            _orbsCasted = true;
            if (!_orbsCasted)
            {
                StartOrbs();
            }
            else
            {
                transform.LookAt(_player);
                StartCoroutine(FastRun());
                // StartCoroutine(JumpAttack());
            }
        }
    
        // Used in Idle animation
        private void StoppedPerformingAttack()
        {
            if (!_anim.GetCurrentAnimatorStateInfo(0).IsTag("WalkingIdle"))
                _performingAttack = false;
        }
        
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
            _anim.SetTrigger("Countered");
        }

        #region Jump Attack
    
        // --- Jump Attack --- //

        private IEnumerator JumpAttack()
        {
            jumpingAttackParticlesPrefab.transform.parent = transform;
            jumpingAttackParticlesPrefab.transform.localPosition = new Vector3();
            jumpingAttackParticlesPrefab.transform.localRotation = Quaternion.identity;
            _anim.SetTrigger(Jump_Attack);
            jumpingAttackParticlesPrefab.SetActive(false);
            yield return new WaitUntil(() => _jumpingAttack);
            _playerNavMeshAgent.obstacleAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance;
            var jumpAttackDistance = Random.Range(_jumpAttackDistance.minValue, _jumpAttackDistance.maxValue);
            while (_jumpingAttack)
            {
                var bossTransform = transform;
                bossTransform.position += bossTransform.forward * (Time.deltaTime * jumpAttackDistance);
                yield return null; 
            }
            _playerNavMeshAgent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
        }

        public void JumpAttackStart() => _jumpingAttack = true;
    
        public void JumpAttackEnd() => _jumpingAttack = false;

        public void ActivateJumpingAttackParticles()
        {
            jumpingAttackParticlesPrefab.SetActive(true);
            jumpingAttackParticlesPrefab.transform.parent = transform.parent;
        }

        public void TriggerTripleSmash() => StartTripleSmash();
        
        #endregion

        #region Triple Smash 
    
        // --- Phase 2: Triple Smash --- //

        private void StartTripleSmash()
        {
            // if phase 2
            _tripleSmashCount = 0;
            foreach (GameObject particlePrefab in tripleSmashParticlesPrefab)
            {
                particlePrefab.transform.parent = transform;
                particlePrefab.transform.localPosition = new Vector3();
                particlePrefab.transform.localRotation = Quaternion.identity;
            }
            StartCoroutine(TripleSmash()); 
        }
    
        private IEnumerator TripleSmash()
        {
            if (_tripleSmashCount == 3)
                yield break;

            _tripleSmashAttack = true;
            tripleSmashParticlesPrefab[_tripleSmashCount].SetActive(false);
            StartCoroutine(TripleSmashRotateOverTime());
            _anim.SetTrigger(Triple_Smash);
            yield return new WaitUntil(() => _tripleSmashAttack == false);
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
    
        public void TripleSmashEnd() => _tripleSmashAttack = false;                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                       
    
        public void ActivateTripleSmashParticles()
        {
            tripleSmashParticlesPrefab[_tripleSmashCount].SetActive(true);
            tripleSmashParticlesPrefab[_tripleSmashCount].transform.parent = transform.parent;
        }

        #endregion

        #region Orbs

        private void StartOrbs()
        {
            _anim.SetTrigger(Orbs);
        }

        private void ActivateOrbs()
        {
            orbsPrefab.SetActive(false);
            orbsPrefab.SetActive(true);
            _orbsCasted = true;
        }

        #endregion
        
        #region FastRun And Melee Attack
        
        private IEnumerator FastRun()
        {
            _performingAttack = false;
            _navMeshAgent.isStopped = false;
            _navMeshAgent.speed = 15;
            _anim.SetTrigger(Fast_Run);
            yield return new WaitUntil(() => _anim.GetBool(Walking) == false);
            StartMeleeAttack();
        }

        private void StartMeleeAttack()
        {
            _performingAttack = true;
            _navMeshAgent.isStopped = true;
            _navMeshAgent.speed = _navMeshOriginalSpeed;
            // We reset the trigger in case Animator transitions to MeleeAttack instantly
            _anim.ResetTrigger(Fast_Run);
            _anim.SetTrigger(Melee_Attack);
        }

        private void StartSecondMelee() => StartCoroutine(SecondMelee());

        private IEnumerator SecondMelee()
        {
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
            Quaternion currentRotation = transform.rotation;
            Quaternion targetRotation = currentRotation * Quaternion.Euler(0, 180, 0);
            while (transform.rotation != targetRotation)
            {
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, 600f * Time.deltaTime);
                yield return null;
            }
        }
        
        #endregion
    
    }
}