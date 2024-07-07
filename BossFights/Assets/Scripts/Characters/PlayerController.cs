using System;
using System.Collections;
using System.Collections.Generic;
using Bosses;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

namespace Characters
{
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] private GameObject[] _basicAttackPrefabs;
        [SerializeField] private Transform _basicAttackSpawnPoint;
        [SerializeField] private float _basicAttackSpeed;
        [SerializeField] private int _health;
        [SerializeField] private TextMeshProUGUI _healthText;
        [SerializeField] private TextMeshProUGUI _QTEText;
        [SerializeField] private float _damageImmuneCD;
        [Header("Skill Dash")]
        [SerializeField] private float _dashDistance;
        [SerializeField] private float _dashFreezeTime;
        [SerializeField] private float _dashCD;
        
        [Header("Skill Heal")]
        [SerializeField] private int _castInputsQ;
        [SerializeField] private GameObject _healingPrefab;
        [SerializeField] private int _skillHealAmount;
        [Header("Skill Wall")] 
        [SerializeField] private int _castInputsW;
        [SerializeField] private GameObject _wallPrefab;
        [Header("Skill Kick")] 
        [SerializeField] private Transform _kickHitPosition;
        [SerializeField] private float _kickHitArea;
        [SerializeField] private float _flipKickDistance;

        private GameObject _wallParticles;
        private NavMeshAgent _navMeshAgent;
        private Camera _mainCamera;
        private Animator _anim;
        private bool _isAnimationLocked;
        private bool _isKickWindowActive;
        private bool _isKickFlipping;
        private bool _isAbleToKickFlip;
        private float _originalDashCD;
        private float _originalDamagedImmuneCD;
        
        private static readonly int Casting = Animator.StringToHash("Casting");
        private static readonly int Running = Animator.StringToHash("Running");
        private static readonly int Shooting = Animator.StringToHash("Shooting");
        private static readonly int Damaged = Animator.StringToHash("Damaged");
        private static readonly int Skill_Dash = Animator.StringToHash("Skill_Dash");
        private static readonly int Skill_Heal = Animator.StringToHash("Skill_Heal");
        private static readonly int Skill_Wall = Animator.StringToHash("Skill_Wall");
        private static readonly int Skill_Kick = Animator.StringToHash("Skill_Kick");
        private static readonly int Skill_FlipKick = Animator.StringToHash("Skill_FlipKick");
        private static readonly int CastInput = Animator.StringToHash("CastInput");

        private void Awake()
        {
            _navMeshAgent = GetComponent<NavMeshAgent>();
            _mainCamera = Camera.main;
            _anim = GetComponent<Animator>();
            _healthText.text = _health.ToString();
            _wallParticles = _wallPrefab.GetComponentInChildren<ParticleSystem>().gameObject;
            
        }

        private void Start()
        {
            // Initialize all cooldowns
            _originalDashCD = _dashCD;
            _originalDamagedImmuneCD = _damageImmuneCD;
            _dashCD = 0;
            _damageImmuneCD = 0;
        }

        private void Update()
        {
            NavMeshAgentPathCheck();
            PlayerInputs();
            PlayerCooldowns();
        }

        #region Base Methods
        
        private void NavMeshAgentPathCheck()
        {
            if (!_anim.GetBool(Running) || _navMeshAgent.pathPending) 
                return;
            if (_navMeshAgent.remainingDistance <= _navMeshAgent.stoppingDistance + 0.25f)
                _anim.SetBool(Running, false);
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

        private void ResetPlayerState(bool includeCoroutines)
        {
            _navMeshAgent.isStopped = true;
            _anim.SetBool(Running, false);
            _anim.SetBool(Shooting, false);
            _anim.SetBool(Casting, false);
            _anim.ResetTrigger(Skill_Heal);
            _anim.ResetTrigger(Skill_Wall);
            _isAnimationLocked = false;
            _isKickFlipping = false;
            _isKickWindowActive = false;
            _isAbleToKickFlip = false;
            _QTEText.transform.parent.gameObject.SetActive(false);
            if (includeCoroutines)
                StopAllCoroutines();
        } 
        
        #endregion
        
        #region Inputs
        
        private void PlayerInputs()
        {
            MovementAndAttackInput();
            SkillsInput();
        }

        private void PlayerCooldowns()
        {
            if (_dashCD > 0)
                _dashCD -= Time.deltaTime;
            if (_damageImmuneCD > 0)
                _damageImmuneCD -= Time.deltaTime;
        }

        private void MovementAndAttackInput()
        {
            if (_isAnimationLocked || _anim.GetCurrentAnimatorStateInfo(0).IsTag("AnimationLock"))
                return;
            
            if (Input.GetMouseButton(1))
            {
                var ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
                if (!Physics.Raycast(ray, out var hit)) 
                    return;
                
                _navMeshAgent.isStopped = false;
                _navMeshAgent.SetDestination(hit.point);
                _anim.SetBool(Running, true);
                _anim.SetBool(Shooting, false);
            }
            
            if (Input.GetMouseButton(0))
            {
                var ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
                if (!Physics.Raycast(ray, out var hit)) 
                    return;
                
                LookAtMouse();
                _navMeshAgent.isStopped = true;
                _anim.SetBool(Running, false);
                _anim.SetBool(Shooting, !_anim.GetCurrentAnimatorStateInfo(0).IsName("Shoot"));
            }
        }
        
        private void SkillsInput()
        {
            if (Input.GetKeyDown(KeyCode.Space))
                SkillDash();

            if (Input.GetKeyDown(KeyCode.E) && _anim.GetCurrentAnimatorStateInfo(0).IsName("Kick"))
                StartCoroutine(StartSkillFlipKick());
            
            if (_isAnimationLocked || _anim.GetCurrentAnimatorStateInfo(0).IsTag("AnimationLock"))
                return;
            
            if (Input.GetKeyDown(KeyCode.Q))
                StartCoroutine(CastingSkill(Skill_Heal, _castInputsQ, false, KeyCode.Q));
            else if (Input.GetKeyDown(KeyCode.W))
                StartCoroutine(CastingSkill(Skill_Wall, _castInputsW, true, KeyCode.W));
            else if (Input.GetKeyDown(KeyCode.E))
                StartCoroutine(SkillKick());
        }
        
        #endregion
        
        #region Damaged Logic

        public void DamagePlayer(int damage)
        {
            if (_damageImmuneCD > 0)
                return;

            // Important to reset player state and coroutines since this method cancels player animations
            ResetPlayerState(true);
            _anim.SetTrigger(Damaged);
            _isAnimationLocked = true;
            _health -= damage;
            _healthText.text = _health.ToString();
            _damageImmuneCD = _originalDamagedImmuneCD;
        }

        // Used in Damaged animation
        public void DamageAnimStopped() => _isAnimationLocked = false;

        #endregion
        
        #region Skill Casting
        
        private IEnumerator CastingSkill(int skillToTrigger, int skillCastInputs, bool targetedSkill, KeyCode keycodeToRemove)
        {
            // May be overkill here, but it is needed to reset some animator booleans 
            ResetPlayerState(false);
            _anim.SetTrigger(skillToTrigger);
            _anim.SetBool(Casting, true);
            _isAnimationLocked = true;

            // Prepare random KeyCodes to Cast for the QTE
            KeyCode[] totalKeyCodes = { KeyCode.Q, KeyCode.W, KeyCode.E, KeyCode.R, KeyCode.A, KeyCode.S, KeyCode.D, KeyCode.F };
            var keycodesToCast = GenerateKeycodesToCast(totalKeyCodes, keycodeToRemove, skillCastInputs);
            
            // Show QTE Keycodes in Screen
            _QTEText.text = string.Join(" ", keycodesToCast);
            _QTEText.text += " ";
            _QTEText.color = Color.black;
            _QTEText.transform.parent.gameObject.SetActive(true);
            
            // This is so the KeyCode to trigger the Cast doesn't enter the loop
            yield return new WaitUntil(() => !Input.GetKeyDown(keycodeToRemove));
            
            // Start QTE
            while (keycodesToCast.Count > 0)
            {
                if (Input.GetKeyDown(keycodesToCast[0]))
                {
                    _anim.SetTrigger(CastInput);
                    keycodesToCast.RemoveAt(0);
                    _QTEText.text = _QTEText.text[2..];
                }
                else
                {
                    foreach (var key in totalKeyCodes)
                    {
                        if (!Input.GetKeyDown(key)) 
                            continue;
                        FailedQTE();
                        yield break;
                    }
                }
                yield return null;
            }
            StartCoroutine(CompletedQTE(targetedSkill));
        }

        private List<KeyCode> GenerateKeycodesToCast(KeyCode[] totalKeyCodes, KeyCode keycodeToRemove, int skillCastInputs)
        {
            var keycodesList = new List<KeyCode>(totalKeyCodes);
            keycodesList.Remove(keycodeToRemove);
            var keycodesToCast = new List<KeyCode>();
            for (var i = skillCastInputs; i > 0; i--)
            {
                var randomCastKey = keycodesList[Random.Range(0, keycodesList.Count)];
                keycodesToCast.Add(randomCastKey);
                keycodesList.Remove(randomCastKey);
            }
            return keycodesToCast;
        }
        
        private void FailedQTE()
        {
            _anim.SetTrigger(Damaged);
            _anim.SetBool(Casting, false);
            _QTEText.transform.parent.gameObject.SetActive(false);
        }

        private IEnumerator CompletedQTE(bool targetedSkill)
        {
            if (targetedSkill)
            {
                _QTEText.text = "CLICK!";
                _QTEText.color = Color.red;
                yield return new WaitUntil(() => Input.GetMouseButton(0));
                LookAtMouse();
            }
            _anim.SetBool(Casting, false);
            _isAnimationLocked = false;
            _QTEText.transform.parent.gameObject.SetActive(false);
        }
        
        #endregion
        
        // **** Skills **** //

        #region Dash
        
        private void SkillDash()
        {
            if (_dashCD > 0)
                return;
            
            // Important to reset player state and coroutines since this method cancels player animations
            ResetPlayerState(true); 
            _anim.SetTrigger(Skill_Dash);
            var ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out var hit))
            {
                _navMeshAgent.enabled = false;
                Vector3 direction = (hit.point - transform.position).normalized;
                transform.position += direction * _dashDistance;
                Quaternion lookRotation = Quaternion.LookRotation(direction);
                transform.rotation = lookRotation;
                _navMeshAgent.enabled = true;
            }
            _dashCD = _originalDashCD;
            StartCoroutine(StartDashFreezeTime(_dashFreezeTime));
        }
        
        private IEnumerator StartDashFreezeTime(float dashFreezeTime)
        {
            _isAnimationLocked = true;
            while (dashFreezeTime > 0)
            {
                dashFreezeTime -= Time.deltaTime;
                yield return null;
            }
            _isAnimationLocked = false;
        }
        
        #endregion
        
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

        private IEnumerator SkillKick()
        {
            // May be overkill here, but it is needed to reset some animator booleans 
            ResetPlayerState(false);
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

        // Used in Kick and FlipKick animation
        private void StopKickHit()
        {
            _isAnimationLocked = false;
            _isKickWindowActive = false;
        }

        // Debug for kick counter collider
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(_kickHitPosition.position, _kickHitArea);
        }
        
        #endregion
    }
}