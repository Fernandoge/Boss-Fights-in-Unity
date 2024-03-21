using TMPro;
using UnityEngine;
using UnityEngine.AI;

namespace Characters
{
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] private GameObject[] _basicAttackPrefabs;
        [SerializeField] private Transform _basicAttackSpawnPoint;
        [SerializeField] private float _basicAttackSpeed;
        [SerializeField] private int _health;
        [SerializeField] private TextMeshProUGUI _healthText;
        [SerializeField] private float _damageCooldown;

        private NavMeshAgent _navMeshAgent;
        private Camera _mainCamera;
        private Animator _anim;
        private Animation _ninjaThrow;
        private float _shootDelay;
        private float _originalDamageCooldown;

        private void Awake()
        {
            _navMeshAgent = GetComponent<NavMeshAgent>();
            _mainCamera = Camera.main;
            _anim = GetComponent<Animator>();
            _healthText.text = _health.ToString();
            _originalDamageCooldown = _damageCooldown;
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
            
            if (_damageCooldown >= 0)
            {
                _damageCooldown -= Time.deltaTime;
            }
        }
    
        // Used in Shoot animation
        public void BasicAttack()
        {
            int basicAttackNumber = Random.Range(0, _basicAttackPrefabs.Length);
            GameObject bullet = Instantiate(_basicAttackPrefabs[basicAttackNumber], _basicAttackSpawnPoint.position, _basicAttackSpawnPoint.rotation);
            Vector3 bulletPosition = bullet.transform.position;
            Vector3 bulletDirection = _basicAttackSpawnPoint.forward;
            _anim.SetBool("Shooting", false);  
            var bulletScript = bullet.GetComponentInChildren<BasicAttack>();
            bulletScript.Shoot(_basicAttackSpeed, bulletPosition, bulletDirection);
        }

        public void DamagePlayer(int damage)
        {
            if (_damageCooldown > 0)
                return;
            
            _health -= damage;
            _healthText.text = _health.ToString();
            _damageCooldown = _originalDamageCooldown;
        }
    }
}