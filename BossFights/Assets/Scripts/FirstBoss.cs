using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Serialization;

public class FirstBoss : MonoBehaviour
{
    public Transform player;
    public GameObject jumpingAttackParticlesPrefab;
    public GameObject[] tripleSmashParticlesPrefab;
    public float timeBetweenAttacks;

    private float auxTimeBetweenAttacks;
    private NavMeshAgent _navMeshAgent;
    private NavMeshAgent _playerNavMeshAgent;
    private Animator _anim;
    private List<IEnumerator> _attacks;
    private bool _jumpingAttack;
    private bool _tripleSmashAttack;
    private int _tripleSmashCount;

    private Vector3 Direction;

    private void Awake()
    {
        _navMeshAgent = GetComponentInChildren<NavMeshAgent>();
        _playerNavMeshAgent = player.GetComponent<NavMeshAgent>();
        _anim = GetComponent<Animator>();
        auxTimeBetweenAttacks = timeBetweenAttacks;
    }

    private void Update()
    {
        timeBetweenAttacks -= Time.deltaTime;
        if (timeBetweenAttacks <= 0)
        {
            PerformAttack();
        }
    }

    private void InitializeAttacks()
    {
        
    }
    
    private void PerformAttack()
    {
        transform.LookAt(player);
        StartJumpAttack();
        timeBetweenAttacks = auxTimeBetweenAttacks;
    }
    
    // --- Jump Attack ---

    private void StartJumpAttack()
    {
        jumpingAttackParticlesPrefab.transform.parent = transform;
        jumpingAttackParticlesPrefab.transform.localPosition = new Vector3();
        jumpingAttackParticlesPrefab.transform.localRotation = Quaternion.identity;
        StartCoroutine(JumpAttack());
    }

    private IEnumerator JumpAttack()
    {
        _anim.SetTrigger("JumpAttack");
        jumpingAttackParticlesPrefab.SetActive(false);
        yield return new WaitUntil(() => _jumpingAttack);
        _playerNavMeshAgent.obstacleAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance;
        while (_jumpingAttack)
        {
            transform.position += transform.forward * (Time.deltaTime * 10);
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

    // --- Phase 2: Triple Smash ---

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
        transform.Rotate(0f, 90f, 0f);
        _anim.SetTrigger("TripleSmash");
        yield return new WaitUntil(() => _tripleSmashAttack == false);
        timeBetweenAttacks = auxTimeBetweenAttacks;
        _tripleSmashCount++;    
        
        StartCoroutine(TripleSmash());
    }
    
    public void TripleSmashEnd() => _tripleSmashAttack = false;
    
    public void ActivateTripleSmashParticles()
    {
        tripleSmashParticlesPrefab[_tripleSmashCount].SetActive(true);
        tripleSmashParticlesPrefab[_tripleSmashCount].transform.parent = transform.parent;
    }
}
