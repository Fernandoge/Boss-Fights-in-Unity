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
        [Header("Skill Heal")]
        [SerializeField] private float _castTimeQ;
        [SerializeField] private GameObject _healingPrefab;
        [SerializeField] private int _skillHealAmount;
        [Header("Skill Wall")] 
        [SerializeField] private float _castTimeW;
        [SerializeField] private GameObject _wallPrefab;

        private GameObject _wallParticles;
        private NavMeshAgent _navMeshAgent;
        private Camera _mainCamera;
        private Animator _anim;
        private float _shootDelay;
        private float _originalDamageCooldown;
        private float _castTime;
        
        private static readonly int Casting = Animator.StringToHash("Casting");
        private static readonly int Running = Animator.StringToHash("Running");
        private static readonly int Shooting = Animator.StringToHash("Shooting");
        private static readonly int Skill_Heal = Animator.StringToHash("Skill_Heal");
        private static readonly int Skill_Wall = Animator.StringToHash("Skill_Wall");

        private void Awake()
        {
            _navMeshAgent = GetComponent<NavMeshAgent>();
            _mainCamera = Camera.main;
            _anim = GetComponent<Animator>();
            _healthText.text = _health.ToString();
            _originalDamageCooldown = _damageCooldown;
            _wallParticles = _wallPrefab.GetComponentInChildren<ParticleSystem>().gameObject;
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
                
                var ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out var hit))
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
                            var targetRotation = Quaternion.LookRotation(hit.point - transform.position);
                            transform.rotation = targetRotation;
                            _anim.SetBool(Running, false);
                            _anim.SetBool(Shooting, !_anim.GetCurrentAnimatorStateInfo(0).IsName("Shoot"));
                        }
                    }
                }
            }

            if (Input.GetKeyDown(KeyCode.Q) || Input.GetKeyDown(KeyCode.W))
            {
                if (_anim.GetCurrentAnimatorStateInfo(0).IsTag("AnimationLock"))
                    return;
                
                _navMeshAgent.isStopped = true;
                _anim.SetBool(Casting, true);
                _anim.SetBool(Shooting, false);
                _anim.SetBool(Running, false);

                if (Input.GetKeyDown(KeyCode.Q))
                {
                    _castTime = _castTimeQ;
                    _anim.SetBool(Skill_Heal, true);
                }
                else if (Input.GetKeyDown(KeyCode.W))
                {
                    _castTime = _castTimeW;
                    var ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
                    _anim.SetBool(Skill_Wall, true);
                    if (Physics.Raycast(ray, out var hit))
                    {
                        var targetRotation = Quaternion.LookRotation(hit.point - transform.position);
                        transform.rotation = targetRotation;
                    }
                }
            }
            
            if (_castTime < 0)
                _anim.SetBool(Casting, false);
        }
        
        public void DamagePlayer(int damage)
        {
            if (_damageCooldown > 0)
                return;
            
            _health -= damage;
            _healthText.text = _health.ToString();
            _damageCooldown = _originalDamageCooldown;
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

        #region Skills

        // Used in Skill animations
        public void StopSkill(string animBoolName)
        {
            _anim.SetBool(animBoolName, false);
        }
        
        // Used in Skill_Health animation
        public void SkillHeal()
        {
            _healingPrefab.SetActive(false);
            _health += _skillHealAmount;
            _healthText.text = _health.ToString();
            _healingPrefab.SetActive(true);
        }
        
        // Used in Skill_Wall animation
        public void SkillWall() 
        {
            _wallPrefab.transform.parent = transform;
            _wallPrefab.transform.localPosition = new Vector3();
            _wallPrefab.transform.localRotation = Quaternion.identity;
            _wallPrefab.SetActive(false);
            
            _wallPrefab.transform.parent = transform.parent;
            _wallPrefab.SetActive(true);
            _wallParticles.SetActive(true);
        }

        #endregion
    }
}