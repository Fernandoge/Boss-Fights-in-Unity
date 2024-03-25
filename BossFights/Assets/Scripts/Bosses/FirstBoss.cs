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
    
        private static readonly int Jump_Attack = Animator.StringToHash("JumpAttack");
        private static readonly int Triple_Smash = Animator.StringToHash("TripleSmash");
        private static readonly int Orbs = Animator.StringToHash("Orbs");
        private static readonly int Walking = Animator.StringToHash("Walking");

        private void Start()
        {
            _player = GameManager.Instance.player.transform;
            _navMeshAgent = GetComponent<NavMeshAgent>();
            _playerNavMeshAgent = _player.GetComponent<NavMeshAgent>();
            _anim = GetComponent<Animator>();
            auxTimeBetweenAttacks = timeBetweenAttacks;
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
            if (!_orbsCasted)
            {
                StartOrbs();
            }
            else
            {
                transform.LookAt(_player);
                StartJumpAttack();
            }
        }
    
        // Used in Idle animation
        private void StoppedPerformingAttack()
        {
            if (!_anim.GetCurrentAnimatorStateInfo(0).IsTag("WalkingIdle"))
                _performingAttack = false;
        }

        #region Jump Attack
    
        // --- Jump Attack --- //
    
        private void StartJumpAttack()
        {
            jumpingAttackParticlesPrefab.transform.parent = transform;
            jumpingAttackParticlesPrefab.transform.localPosition = new Vector3();
            jumpingAttackParticlesPrefab.transform.localRotation = Quaternion.identity;
            StartCoroutine(JumpAttack());
        }

        private IEnumerator JumpAttack()
        {
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
    
    }
}