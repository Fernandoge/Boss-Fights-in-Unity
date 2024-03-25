using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Serialization;

public class FirstBoss : MonoBehaviour
{
    public Transform player;
    public float timeBetweenAttacks;
    [Header("EarthShatter")] 
    public int earthShatterDamage;
    public GameObject jumpingAttackParticlesPrefab;
    public GameObject[] tripleSmashParticlesPrefab;
    [Header("Orbs")] 
    public GameObject orbsPrefab;
    public int orbsDamage;
    

    private float auxTimeBetweenAttacks;
    private NavMeshAgent _playerNavMeshAgent;
    private Animator _anim;
    private List<IEnumerator> _attacks;
    private bool _jumpingAttack;
    private bool _tripleSmashAttack;
    private int _tripleSmashCount;
    private bool _orbsCasted;
    private bool _stopTimeBetweenAttacks;
    
    private static readonly int Jump_Attack = Animator.StringToHash("JumpAttack");
    private static readonly int Triple_Smash = Animator.StringToHash("TripleSmash");
    private static readonly int Orbs = Animator.StringToHash("Orbs");

    private void Awake()
    {
        _playerNavMeshAgent = player.GetComponent<NavMeshAgent>();
        _anim = GetComponent<Animator>();
        auxTimeBetweenAttacks = timeBetweenAttacks;
    }

    private void Update()
    {
        if (!_stopTimeBetweenAttacks)
        {
            timeBetweenAttacks -= Time.deltaTime;
        }
        
        if (timeBetweenAttacks <= 0)
            PerformAttack();

        // Debugging Only
        if (Input.GetKeyDown(KeyCode.K))
        {
            _orbsCasted = false;
        }
    }

    private void PerformAttack()
    {
        // TODO: if HP values
        if (!_orbsCasted)
        {
            StartOrbs();
        }
        else
        {
            transform.LookAt(player);
            StartJumpAttack();
            RestartTimeBetweenAttacks();
            StopTimeBetweenAttacks();
        }
    }
    
    private void RestartTimeBetweenAttacks() => timeBetweenAttacks = auxTimeBetweenAttacks;
    private void StopTimeBetweenAttacks() => _stopTimeBetweenAttacks = true;
    private void ActivateTimeBetweenAttacks() => _stopTimeBetweenAttacks = false;

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
        while (_jumpingAttack)
        {
            var bossTransform = transform;
            bossTransform.position += bossTransform.forward * (Time.deltaTime * 10);
            yield return null; 
        }
        _playerNavMeshAgent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
    }

    public void JumpAttackStart() => _jumpingAttack = true;
    
    public void JumpAttackEnd()
    {
        ActivateTimeBetweenAttacks();
        _jumpingAttack = false;
    }

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
        
        StopTimeBetweenAttacks();
        StartCoroutine(TripleSmash()); 
    }
    
    private IEnumerator TripleSmash()
    {
        if (_tripleSmashCount == 3)
        {
            ActivateTimeBetweenAttacks();
            yield break;
        }
        
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
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 8);
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
        RestartTimeBetweenAttacks();
    }

    private void ActivateOrbs()
    {
        orbsPrefab.SetActive(false);
        orbsPrefab.SetActive(true);
        _orbsCasted = true;
    }

    #endregion
    
}
