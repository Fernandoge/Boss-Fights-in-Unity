using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class PlayerMovement : MonoBehaviour
{
    public GameObject[] basicAttackPrefabs;
    public Transform basicAttackSpawnPoint;
    public float BasicAttackSpeed;
    
    private NavMeshAgent _navMeshAgent;
    private Camera _mainCamera;
    private Animator _anim;
    private Animation _ninjaThrow;
    private float _shootDelay;

    private void Awake()
    {
        _navMeshAgent = GetComponent<NavMeshAgent>();
        _mainCamera = Camera.main;
        _anim = GetComponent<Animator>();
    }

    private void Update()
    {
        _shootDelay -= Time.deltaTime;  
        
        if (!_navMeshAgent.pathPending)
        {
            if (_navMeshAgent.remainingDistance <= _navMeshAgent.stoppingDistance + 0.25f)
            {
                _anim.SetBool("Running", false);
            }
        }

        if (Input.GetMouseButton(1) || Input.GetMouseButton(0)) 
        {
            Ray ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit))
            {
                if (Input.GetMouseButton(1))
                {
                    _anim.SetBool("Running", true);
                    _anim.SetBool("Shooting", false);
                    _navMeshAgent.SetDestination(hit.point);
                    _navMeshAgent.isStopped = false;
                }
                if (Input.GetMouseButton(0))
                {
                    _navMeshAgent.isStopped = true;
                    if (_shootDelay <= 0)
                    {
                        Quaternion targetRotation = Quaternion.LookRotation(hit.point - transform.position);
                        transform.rotation = targetRotation;
                        _anim.SetBool("Running", false);
                        if (!_anim.GetCurrentAnimatorStateInfo(0).IsName("Shoot"))
                            _anim.SetBool("Shooting", true);
                        else
                            _anim.SetBool("Shooting", false);
                    }
                }
            }
        }
    }
    
    // Used in Shoot animation
    public void BasicAttack()
    {
        int basicAttackNumber = Random.Range(0, basicAttackPrefabs.Length);
        GameObject bullet = Instantiate(basicAttackPrefabs[basicAttackNumber], basicAttackSpawnPoint.position, basicAttackSpawnPoint.rotation);
        Vector3 bulletPosition = bullet.transform.position;
        Vector3 bulletDirection = basicAttackSpawnPoint.forward;
        StartCoroutine(MoveBullet(bullet.transform, BasicAttackSpeed, bulletPosition, bulletDirection));
        _anim.SetBool("Shooting", false);  
    }
    
    IEnumerator MoveBullet(Transform bulletTransform, float bulletSpeeed, Vector3 initialPosition, Vector3 direction)
    {
        float elapsedTime = 0f;
        float distance = 0f;

        while (distance < bulletSpeeed)
        {
            elapsedTime += Time.deltaTime;
            distance = elapsedTime * bulletSpeeed;
            bulletTransform.position = initialPosition + direction * distance;
            yield return null;
        }
        
        Destroy(bulletTransform.gameObject);
    }
}