using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class FirstBoss : MonoBehaviour
{
    public Transform player;
    public GameObject jumpingAttackParticlePrefab;
    public float timeBetweenAttacks;

    private float auxTimeBetweenAttacks;
    private NavMeshAgent _navMeshAgent;
    private NavMeshAgent _playerNavMeshAgent;
    private Animator _anim;
    private List<IEnumerator> _attacks;
    private bool _jumpingAttack;

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
        StartCoroutine(nameof(JumpAttack));
        timeBetweenAttacks = auxTimeBetweenAttacks;
    }

    private IEnumerator JumpAttack()
    {
        jumpingAttackParticlePrefab.SetActive(false);
        _anim.SetTrigger("JumpAttack");
        yield return new WaitUntil(() => _jumpingAttack);
        _playerNavMeshAgent.obstacleAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance;
        while (_jumpingAttack)
        {
            transform.position += transform.forward * Time.deltaTime * 10;
            yield return null; 
        }
        _playerNavMeshAgent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
    }

    public void JumpAttackStarts() => _jumpingAttack = true;
    public void JumpAttackEnded() => _jumpingAttack = false;
    public void ActivateJumpingAttackParticles() => jumpingAttackParticlePrefab.SetActive(true);
}
