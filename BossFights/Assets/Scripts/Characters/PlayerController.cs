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
        [SerializeField] private float _castTimeQ;
        [SerializeField] private int _skillHealAmount;

        private NavMeshAgent _navMeshAgent;
        private Camera _mainCamera;
        private Animator _anim;
        private Animation _ninjaThrow;
        private float _shootDelay;
        private float _originalDamageCooldown;
        private float _castTime;
        
        private static readonly int Casting = Animator.StringToHash("Casting");
        private static readonly int Running = Animator.StringToHash("Running");
        private static readonly int Shooting = Animator.StringToHash("Shooting");
        private static readonly int Skill_Heal = Animator.StringToHash("Skill_Heal");

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
            PlayerInputs();
            PlayerTimers();
            NavMeshAgentPathCheck();
        }

        private void NavMeshAgentPathCheck()
        {
            if (_navMeshAgent.pathPending) 
                return;
            if (_navMeshAgent.remainingDistance <= _navMeshAgent.stoppingDistance + 0.25f)
                _anim.SetBool(Running, false);
        }
        
        private void PlayerTimers()
        {
            _shootDelay -= Time.deltaTime;
            _castTime -= Time.deltaTime;
            if (_damageCooldown >= 0)
                _damageCooldown -= Time.deltaTime;
        }
        
        private void PlayerInputs()
        {
            if (Input.GetMouseButton(1) || Input.GetMouseButton(0))
            {
                if (_anim.GetBool(Casting) || _anim.GetCurrentAnimatorStateInfo(0).IsTag("AnimationLock"))
                    return;
                
                Ray ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
                RaycastHit hit;
                if (Physics.Raycast(ray, out hit))
                {
                    if (Input.GetMouseButton(1))
                    {
                        _anim.SetBool(Running, true);
                        _anim.SetBool(Shooting, false);
                        _anim.SetBool(Casting, false);
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
                            _anim.SetBool(Running, false);
                            if (!_anim.GetCurrentAnimatorStateInfo(0).IsName("Shoot"))
                                _anim.SetBool(Shooting, true);
                            else
                                _anim.SetBool(Shooting, false);
                        }
                    }
                }
            }

            if (Input.GetKeyDown(KeyCode.Q))
            {
                if (_anim.GetCurrentAnimatorStateInfo(0).IsTag("AnimationLock"))
                    return;
                
                _navMeshAgent.isStopped = true;
                _castTime = _castTimeQ;
                _anim.SetBool(Casting, true);
                _anim.SetBool(Shooting, false);
                _anim.SetBool(Running, false);
                
                if (Input.GetKeyDown(KeyCode.Q))
                    _anim.SetBool(Skill_Heal, true);
            }
            
            if (_castTime < 0)
                _anim.SetBool(Casting, false);
        }
    
        // Used in Shoot animation
        public void BasicAttack()
        {
            int basicAttackNumber = Random.Range(0, _basicAttackPrefabs.Length);
            GameObject bullet = Instantiate(_basicAttackPrefabs[basicAttackNumber], _basicAttackSpawnPoint.position, _basicAttackSpawnPoint.rotation);
            Vector3 bulletPosition = bullet.transform.position;
            Vector3 bulletDirection = _basicAttackSpawnPoint.forward;
            _anim.SetBool(Shooting, false);
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
        
        // Used in Skill_Health animation
        public void Heal()
        {
            _health += _skillHealAmount;
            _healthText.text = _health.ToString();
            _anim.SetBool(Skill_Heal, false);
        }
    }
}