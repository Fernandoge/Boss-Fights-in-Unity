using System;
using UnityEngine;
using UnityEngine.AI;

public class PlayerMovement : MonoBehaviour
{
    private NavMeshAgent navMeshAgent;
    private Camera mainCamera;
    private Animator anim;
    private Animation ninjaThrow;
    private float shootDelay;

    private void Awake()
    {
        navMeshAgent = GetComponent<NavMeshAgent>();
        mainCamera = Camera.main;
        anim = GetComponent<Animator>();
    }

    private void Update()
    {
        shootDelay -= Time.deltaTime;  
        
        if (!navMeshAgent.pathPending)
        {
            if (navMeshAgent.remainingDistance <= navMeshAgent.stoppingDistance + 0.25f)
            {
                anim.SetBool("Running", false);
            }
        }

        if (Input.GetMouseButton(1) || Input.GetMouseButton(0)) 
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit))
            {
                if (Input.GetMouseButton(1))
                {
                    anim.SetBool("Running", true);
                    anim.SetBool("Shooting", false);
                    navMeshAgent.SetDestination(hit.point);
                    navMeshAgent.isStopped = false;
                }
                if (Input.GetMouseButton(0))
                {
                    navMeshAgent.isStopped = true;
                    if (shootDelay <= 0)
                    {
                        Quaternion targetRotation = Quaternion.LookRotation(hit.point - transform.position);
                        transform.rotation = targetRotation;
                        anim.SetBool("Running", false);
                        if (!anim.GetCurrentAnimatorStateInfo(0).IsName("Shoot"))
                            anim.SetBool("Shooting", true);
                        else
                            anim.SetBool("Shooting", false);
                    }
                }
            }
        }
    }
    
    // Used in Shoot animation
    public void ShootingEnded()                    
    {                                              
        anim.SetBool("Shooting", false);           
    }

    // Used in Shoot animation
    public void BasicAttack()
    {
        
    }
}