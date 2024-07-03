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
        private bool _isDamageImmune;
        private bool _isAnimationLocked;
        private bool _isKickWindowActive;
        private bool _isKickFlipping;
        private bool _isAbleToKickFlip;
        
        private static readonly int Casting = Animator.StringToHash("Casting");
        private static readonly int Running = Animator.StringToHash("Running");
        private static readonly int Shooting = Animator.StringToHash("Shooting");
        private static readonly int Damaged = Animator.StringToHash("Damaged");
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
            _wallParticles = _wallPrefab.GetComponentInChildren<ParticleSystem>().gameObject;
        }

        private void Update()
        {
            NavMeshAgentPathCheck();
            PlayerInputs();
        }

        private void NavMeshAgentPathCheck()
        {
            if (_navMeshAgent.pathPending) 
                return;
            if (_navMeshAgent.remainingDistance <= _navMeshAgent.stoppingDistance + 0.25f)
                _anim.SetBool(Running, false);
        }
        
        private void PlayerInputs()
        {
            MovementAndAttackInput();
            SkillsInput();
        }

        private void MovementAndAttackInput()
        {
            if (!Input.GetMouseButton(1) && !Input.GetMouseButton(0))
                return;
            
            if (_isAnimationLocked || _anim.GetCurrentAnimatorStateInfo(0).IsTag("AnimationLock"))
                return;
                
            var ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
            if (!Physics.Raycast(ray, out var hit)) 
                return;
            
            if (Input.GetMouseButton(1))
            {
                _anim.SetBool(Running, true);
                _anim.SetBool(Shooting, false);
                _navMeshAgent.SetDestination(hit.point);
                _navMeshAgent.isStopped = false;
            }
            if (Input.GetMouseButton(0))
            {
                LookAtMouse();
                _navMeshAgent.isStopped = true;
                _anim.SetBool(Running, false);
                _anim.SetBool(Shooting, !_anim.GetCurrentAnimatorStateInfo(0).IsName("Shoot"));
            }
        }
        
        private void SkillsInput()
        {
            if (!Input.GetKeyDown(KeyCode.Q) && !Input.GetKeyDown(KeyCode.W) && !Input.GetKeyDown(KeyCode.E)) 
                return;

            if (Input.GetKeyDown(KeyCode.E) && _anim.GetCurrentAnimatorStateInfo(0).IsName("Kick"))
                StartCoroutine(StartSkillFlipKick());
                
            if (_anim.GetCurrentAnimatorStateInfo(0).IsTag("AnimationLock"))
                return;
                
            ResetAnimIdle();

            if (Input.GetKeyDown(KeyCode.Q))
                StartCastingSkill(Skill_Heal, _castTimeQ, false);
            else if (Input.GetKeyDown(KeyCode.W))
                StartCastingSkill(Skill_Wall, _castTimeW, true);
            else if (Input.GetKeyDown(KeyCode.E))
                StartCoroutine(StartSkillKick());
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

        private void LookAtMouse()
        {
            var ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
            if (!Physics.Raycast(ray, out var hit)) 
                return;
            var targetRotation = Quaternion.LookRotation(hit.point - transform.position);
            transform.rotation = targetRotation;
        }

        private void ResetAnimIdle()
        {
            _anim.SetBool(Running, false);
            _anim.SetBool(Shooting, false);
            _navMeshAgent.isStopped = true;
        } 
        
        #region Damaged Logic
        
        public void DamagePlayer(int damage)
        {
            if (_isDamageImmune)
                return;
            
            ResetAnimIdle();
            ResetKickValues();
            _health -= damage;
            _healthText.text = _health.ToString();
            _isDamageImmune = true;
            _isAnimationLocked = true;
            _anim.SetTrigger(Damaged);
            StartCoroutine(StartDamagedCooldown(_damageCooldown));
        }

        private IEnumerator StartDamagedCooldown(float damageCooldown)
        {
            while (damageCooldown > 0)
            {
                damageCooldown -= Time.deltaTime;
                yield return null;
            }
            _isDamageImmune = false;
        }
        

        public void DamageAnimStopped() => _isAnimationLocked = false;

        #endregion
        
        #region Skills

        private void StartCastingSkill(int skillToTrigger, float skillCastTime, bool lookAtMouse)
        {
            _isAnimationLocked = true;
            _anim.SetBool(Casting, true);
            _anim.SetTrigger(skillToTrigger);
            StartCoroutine(StartCastTime(skillCastTime));
            if (lookAtMouse)
                LookAtMouse();
        }
        
        private IEnumerator StartCastTime(float castTime)
        {
            while (castTime > 0)
            {
                castTime -= Time.deltaTime;
                yield return null;
            }
            _anim.SetBool(Casting, false);
            _isAnimationLocked = false;
        }
        
        #region Heal
        
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
            _isAnimationLocked = true;
            _anim.SetTrigger(Skill_Kick);
            yield return new WaitUntil(() => _isKickWindowActive);
            while (_isKickWindowActive)
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
            _isKickWindowActive = true;
        }

        // Called in case something cancels a Kick
        private void ResetKickValues()
        {
            _isKickFlipping = false;
            _isAbleToKickFlip = false;
            _isKickWindowActive = false;
            StopCoroutine(StartSkillKick());
            StopCoroutine(StartSkillFlipKick());
        }

        // Used in Kick and FlipKick animation
        private void StopKickHit()
        {
            _isAnimationLocked = false;
            _isKickWindowActive = false;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(_kickHitPosition.position, _kickHitArea);
        }
        
        #endregion

        #endregion
    }
}