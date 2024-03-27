using System.Collections;
using Bosses;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Serialization;

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
        [Header("Skill Kick")] 
        [SerializeField] private Transform _kickHitPosition;
        [SerializeField] private float _kickHitArea;
        [SerializeField] private float _flipKickDistance;

        private GameObject _wallParticles;
        private NavMeshAgent _navMeshAgent;
        private Camera _mainCamera;
        private Animator _anim;
        private float _shootDelay;
        private float _originalDamageCooldown;
        private float _castTime;
        private bool _isKicking;
        private bool _isKickFlipping;
        private bool _isAbleToKickFlip;
        
        private static readonly int Casting = Animator.StringToHash("Casting");
        private static readonly int Running = Animator.StringToHash("Running");
        private static readonly int Shooting = Animator.StringToHash("Shooting");
        private static readonly int Skill_Heal = Animator.StringToHash("Skill_Heal");
        private static readonly int Skill_Wall = Animator.StringToHash("Skill_Wall");
        private static readonly int Skill_Kick = Animator.StringToHash("Skill_Kick");
        private static readonly int Skill_FlipKick = Animator.StringToHash("Skill_FlipKick");

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

            if (Input.GetKeyDown(KeyCode.Q) || Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.E))
            {
                if (_anim.GetCurrentAnimatorStateInfo(0).IsName("Kick") && Input.GetKeyDown(KeyCode.E))
                    StartCoroutine(StartSkillFlipKick());
                
                if (_anim.GetCurrentAnimatorStateInfo(0).IsTag("AnimationLock"))
                    return;
                
                _navMeshAgent.isStopped = true;
                _anim.SetBool(Shooting, false);
                _anim.SetBool(Running, false);

                if (Input.GetKeyDown(KeyCode.Q))
                    StartSkillHeal();
                else if (Input.GetKeyDown(KeyCode.W))
                    StartSkillWall();
                else if (Input.GetKeyDown(KeyCode.E))
                    StartCoroutine(StartSkillKick());
            }
            
            if (_castTime < 0)
                _anim.SetBool(Casting, false);
        }

        private void LookAtMouse()
        {
            var ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
            if (!Physics.Raycast(ray, out var hit)) 
                return;
            var targetRotation = Quaternion.LookRotation(hit.point - transform.position);
            transform.rotation = targetRotation;
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

        #region Heal

        private void StartSkillHeal()
        {
            _anim.SetBool(Casting, true);
            _anim.SetTrigger(Skill_Heal);
            _castTime = _castTimeQ;
        }
        
        // Used in Skill_Health animation
        private void SkillHeal()
        {
            _healingPrefab.SetActive(false);
            _health += _skillHealAmount;
            _healthText.text = _health.ToString();
            _healingPrefab.SetActive(true);
        }
        

        #endregion

        #region Wall

        private void StartSkillWall()
        {
            _anim.SetBool(Casting, true);
            _anim.SetTrigger(Skill_Wall);
            _castTime = _castTimeW;
            LookAtMouse();
        }
        
        // Used in Skill_Wall animation
        private void SkillWall() 
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

        #region Kick

        private IEnumerator StartSkillKick()
        {
            LookAtMouse();
            _isAbleToKickFlip = true;
            _anim.SetTrigger(Skill_Kick);
            yield return new WaitUntil(() => _isKicking);
            while (_isKicking)
            {
                Collider[] hitColliders = Physics.OverlapSphere(_kickHitPosition.position, _kickHitArea);
                foreach (Collider hitCollider in hitColliders)
                {
                    if (!hitCollider.CompareTag("Counterable")) 
                        continue;
                    
                    hitCollider.GetComponentInParent<FirstBoss>().Countered();
                }
                yield return null;
            }
        }
        
        private IEnumerator StartSkillFlipKick()
        {
            if (!_isAbleToKickFlip)
                yield break;
            
            _anim.SetTrigger(Skill_FlipKick);
            yield return new WaitUntil(() => _isKickFlipping);
            while (_isKickFlipping)
            {
                var playerTransform = transform;
                playerTransform.position += playerTransform.forward * (Time.deltaTime * _flipKickDistance);
                yield return null;
            }
        }

        // Used in FlipKick animation
        private void StartFlip() => _isKickFlipping = true;
        
        // Used in Kick and FlipKick animation
        private void StartKickHit()
        {
            _isKickFlipping = false;
            _isAbleToKickFlip = false;
            _isKicking = true;
        }

        // Used in Kick and FlipKick animation
        private void StopKickHit() => _isKicking = false;

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(_kickHitPosition.position, _kickHitArea);
        }

#endregion

        #endregion
        
        
    }
}