using System.Collections;
using System.Collections.Generic;
using Interfaces;
using Shared;
using TMPro;
using UI;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

namespace Characters.Ninja
{
    public class NinjaController : PlayerController
    {
        [Header("")]
        [Header("Ninja")]
        [SerializeField] private TextMeshProUGUI _QTEText;
        [SerializeField] private TextMeshProUGUI _QTEAlert;
        [Header("Skill Heal")]
        [SerializeField] private GameObject _healingPrefab;
        [SerializeField] private int _castInputsHeal;
        [SerializeField] private int _skillHealAmount;
        [SerializeField] private float _healCD;
        [SerializeField] private SpellIcon _healSpellIcon;
        [Header("Skill Clones")]
        [SerializeField] private GameObject _clonePrefab;
        [SerializeField] private int _castInputsClones;
        [SerializeField] private float _clonesDuration;
        [SerializeField] private float _clonesCD;
        [SerializeField] private SpellIcon _clonesSpellIcon;
        [Header("Skill Huuma")]
        [SerializeField] private HuumaShuriken _huumaPrefab;
        [SerializeField] private GameObject _huumaSwapEffectPrefab;
        [SerializeField] private int _castInputsHuuma = 2;
        [SerializeField] private float _huumaRange = 20f;
        [SerializeField] private float _huumaSpeed = 40f;
        [SerializeField] private float _huumaLifetime = 5f;
        [SerializeField] private float _huumaCD = 8f;
        [SerializeField] private SpellIcon _huumaSpellIcon;
        [Header("Skill Kick")]
        [SerializeField] private Transform _kickHitPosition;
        [SerializeField] private float _kickHitArea;
        [SerializeField] private float _flipKickDistance;
        [SerializeField] private SpellIcon _kickSpellIcon;

        private bool _isKickWindowActive;
        private bool _isKickFlipping;
        private bool _isAbleToKickFlip;
        private float _originalHealCD;
        private float _originalClonesCD;
        private float _originalHuumaCD;
        private HuumaShuriken _activeHuuma;
        private List<NinjaClone> _activeClones;
        private Vector3 _storedShootTargetPoint;
        private bool _wasShootingLastFrame;
        private int _currentSkillBeingCast;
        
        private static readonly int Skill_Heal = Animator.StringToHash("Skill_Heal");
        private static readonly int Skill_Clones = Animator.StringToHash("Skill_Clones");
        private static readonly int Skill_Huuma = Animator.StringToHash("Skill_Huuma");
        private static readonly int Skill_Kick = Animator.StringToHash("Skill_Kick");
        private static readonly int Skill_FlipKick = Animator.StringToHash("Skill_FlipKick");
        private static readonly int CastInput = Animator.StringToHash("CastInput");
        private static readonly int Shooting = Animator.StringToHash("Shooting");
        private const KeyCode HealKey = KeyCode.Q;
        private const KeyCode ClonesKey = KeyCode.W;
        private const KeyCode KickKey = KeyCode.E;
        private const KeyCode HuumaKey = KeyCode.A;

        /// *** Unity Events *** ///
        
        protected override void Start()
        {
            base.Start();
            
            // Initialize heal cooldown
            _originalHealCD = _healCD;
            _healCD = 0;
            
            // Initialize clones cooldown
            _originalClonesCD = _clonesCD;
            _clonesCD = 0;
            _activeClones = new List<NinjaClone>();

            // Initialize huuma cooldown
            _originalHuumaCD = _huumaCD;
            _huumaCD = 0;

            _healSpellIcon.SetKeyLabel(HealKey);
            _clonesSpellIcon.SetKeyLabel(ClonesKey);
            _kickSpellIcon.SetKeyLabel(KickKey);
            _huumaSpellIcon.SetKeyLabel(HuumaKey);
        }
        
        /// *** Base Methods *** ///

        protected override void PlayerCooldowns()
        {
            base.PlayerCooldowns();

            if (_healCD > 0)
                _healCD -= Time.deltaTime;
            if (_clonesCD > 0)
                _clonesCD -= Time.deltaTime;
            if (_huumaCD > 0)
                _huumaCD -= Time.deltaTime;

            // Capture mouse world point when shooting animation starts
            bool isShootingNow = anim.GetBool(Shooting);
            if (isShootingNow && !_wasShootingLastFrame)
            {
                if (GetMouseWorldPoint(out Vector3 mouseWorldPoint))
                    _storedShootTargetPoint = mouseWorldPoint;
                else
                    _storedShootTargetPoint = transform.position + transform.forward * 10f;
            }
            _wasShootingLastFrame = isShootingNow;
        }

        protected override void ResetPlayerState(bool includeCoroutines)
        {
            base.ResetPlayerState(includeCoroutines);
            
            anim.ResetTrigger(Skill_Heal);
            anim.ResetTrigger(Skill_Clones);
            anim.ResetTrigger(Skill_Huuma);
            _isKickFlipping = false;
            _isKickWindowActive = false;
            SetKickFlipAvailable(false);
            _QTEAlert.gameObject.SetActive(false);
            _QTEText.transform.parent.gameObject.SetActive(false);
        }

        public override void DamagePlayer(int damage)
        {
            StartCooldownForCurrentSkill();
            base.DamagePlayer(damage);
        }
        
        public override void BasicAttack()
        {
            base.BasicAttack();
            float normalizedTime = anim.GetCurrentAnimatorStateInfo(0).normalizedTime % 1f;
            
            // Use stored target point captured when shooting started
            foreach (NinjaClone clone in _activeClones)
            {
                if (clone != null)
                    clone.TriggerShoot(normalizedTime, _storedShootTargetPoint);
            }
        }

        private void StartCooldownForCurrentSkill()
        {
            // Start cooldown if casting Heal or Clones skill
            if (_currentSkillBeingCast == Skill_Heal)
            {
                _healCD = _originalHealCD;
                _healSpellIcon.StartCooldown(_originalHealCD);
            }
            else if (_currentSkillBeingCast == Skill_Clones)
            {
                _clonesCD = _originalClonesCD;
                _clonesSpellIcon.StartCooldown(_originalClonesCD);
            }
            else if (_currentSkillBeingCast == Skill_Huuma)
            {
                _huumaCD = _originalHuumaCD;
                _huumaSpellIcon.StartCooldown(_originalHuumaCD);
            }
            
            _currentSkillBeingCast = 0;
        }
        
        /// *** Inputs *** ///
        
        protected override void SkillsInput()
        {
            base.SkillsInput();
            
            if (Input.GetKeyDown(KickKey) && anim.GetCurrentAnimatorStateInfo(0).IsName("Kick"))
                StartCoroutine(StartSkillFlipKick());
            
            if (isAnimationLocked || anim.GetCurrentAnimatorStateInfo(0).IsTag("AnimationLock"))
                return;
            
            if (Input.GetKeyDown(HealKey) && _healCD <= 0)
                StartCoroutine(CastingSkill(Skill_Heal, _castInputsHeal, false, HealKey));
            else if (Input.GetKeyDown(ClonesKey) && _clonesCD <= 0)
                StartCoroutine(CastingSkill(Skill_Clones, _castInputsClones, false, ClonesKey));
            else if (Input.GetKeyDown(KickKey))
                StartCoroutine(SkillKick());
            else if (Input.GetKeyDown(HuumaKey))
            {
                // While the shuriken waits on the ground the key swaps places with it; otherwise it casts the throw
                if (_activeHuuma != null)
                {
                    if (_activeHuuma.IsStuck)
                        SwapWithHuuma();
                }
                else if (_huumaCD <= 0)
                    StartCoroutine(CastingSkill(Skill_Huuma, _castInputsHuuma, false, HuumaKey));
            }
        }
        
        /// *** Skill Casting *** ///
        
        private IEnumerator CastingSkill(int skillToTrigger, int skillCastInputs, bool targetedSkill, KeyCode keycodeToRemove)
        {
            // May be overkill here, but it is needed to reset some animator booleans 
            ResetPlayerState(false);
            _currentSkillBeingCast = skillToTrigger;
            anim.SetTrigger(skillToTrigger);
            anim.SetBool(Casting, true);
            isAnimationLocked = true;

            // Prepare random KeyCodes to Cast for the QTE
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
                    anim.SetTrigger(CastInput);
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
            for (var inputCount = skillCastInputs; inputCount > 0; inputCount--)
            {
                var randomCastKey = keycodesList[Random.Range(0, keycodesList.Count)];
                keycodesToCast.Add(randomCastKey);
                keycodesList.Remove(randomCastKey);
            }
            return keycodesToCast;
        }
        
        private void FailedQTE()
        {
            StartCooldownForCurrentSkill();
            
            // To cancel QTE Coroutines
            ResetPlayerState(true);
            anim.SetTrigger(Damaged);
            anim.SetBool(Casting, false);
            _QTEText.transform.parent.gameObject.SetActive(false);
            _QTEAlert.gameObject.SetActive(false);
        }

        private IEnumerator CompletedQTE(bool targetedSkill)
        {
            _QTEText.transform.parent.gameObject.SetActive(false);
            if (targetedSkill)
            {
                _QTEAlert.gameObject.SetActive(true);
                _QTEAlert.text = "Click!";
                yield return new WaitUntil(() => Input.GetMouseButton(0));
                LookAtMouse();
                
                //Debug
                print("click completed coroutine");
            }
            _QTEAlert.gameObject.SetActive(false);
            anim.SetBool(Casting, false);
            isAnimationLocked = false;
        }
        
        /// ***** Skills ***** ///
        
        /// *** Heal *** ///
        
        // Used in Skill_Health animation
        private void SkillHeal()
        {
            _healingPrefab.SetActive(false);
            SetHealth(_skillHealAmount);
            _healingPrefab.SetActive(true);
            
            // Start cooldown when skill executes
            _healCD = _originalHealCD;
            _healSpellIcon.StartCooldown(_originalHealCD);
            _currentSkillBeingCast = 0;
        }

        /// *** Clones *** ///

        // Used in Skill_Clones animation
        private void SkillClones()
        {
            foreach (NinjaClone clone in _activeClones)
            {
                if (clone != null)
                    clone.StopClone();
            }
            _activeClones.Clear();

            Vector3 leftOffset = transform.position + transform.right * -2.5f;
            Vector3 rightOffset = transform.position + transform.right * 2.5f;

            GameObject leftCloneObj = Instantiate(_clonePrefab, leftOffset, transform.rotation);
            GameObject rightCloneObj = Instantiate(_clonePrefab, rightOffset, transform.rotation);

            NinjaClone leftClone = leftCloneObj.GetComponent<NinjaClone>();
            NinjaClone rightClone = rightCloneObj.GetComponent<NinjaClone>();

            leftClone.SetLifetime(_clonesDuration);
            leftClone.StartAttacking();
            
            rightClone.SetLifetime(_clonesDuration);
            rightClone.StartAttacking();

            _activeClones.Add(leftClone);
            _activeClones.Add(rightClone);
            
            // Start cooldown when skill executes
            _clonesCD = _originalClonesCD;
            _clonesSpellIcon.StartCooldown(_originalClonesCD);
            _currentSkillBeingCast = 0;
        }

        /// *** Huuma Shuriken *** ///

        // Used in Skill_Huuma animation: throws the shuriken toward the mouse
        private void SkillHuuma()
        {
            LookAtMouse();
            Vector3 origin = transform.position + Vector3.up * 1.2f + transform.forward * 0.8f;
            _activeHuuma = Instantiate(_huumaPrefab);
            _activeHuuma.Begin(origin, transform.forward, _huumaRange, _huumaSpeed, _huumaLifetime, transform, OnHuumaStuck, OnHuumaEnded);
            _currentSkillBeingCast = 0;
        }

        // The key lights up while the shuriken waits on the ground
        private void OnHuumaStuck() => _huumaSpellIcon.SetHighlighted(true);

        // The cooldown starts when the shuriken is gone, whether it ran out of time or was used
        private void OnHuumaEnded()
        {
            _activeHuuma = null;
            _huumaSpellIcon.SetHighlighted(false);
            _huumaCD = _originalHuumaCD;
            _huumaSpellIcon.StartCooldown(_originalHuumaCD);
        }

        // The player goes where the shuriken is and the shuriken is used up
        private void SwapWithHuuma()
        {
            Vector3 from = transform.position;
            Vector3 to = NavMesh.SamplePosition(_activeHuuma.transform.position, out NavMeshHit navHit, 5f, NavMesh.AllAreas) ? navHit.position : from;

            ResetPlayerState(false);
            NavMeshAgent agent = GetComponent<NavMeshAgent>();
            agent.Warp(to);
            agent.ResetPath();

            if (_huumaSwapEffectPrefab)
            {
                Destroy(Instantiate(_huumaSwapEffectPrefab, from + Vector3.up, Quaternion.identity), 2f);
                Destroy(Instantiate(_huumaSwapEffectPrefab, to + Vector3.up, Quaternion.identity), 2f);
            }

            _activeHuuma.Consume();
        }

        /// *** Kick *** ///

        private IEnumerator SkillKick()
        {
            // May be overkill here, but it is needed to reset some animator booleans 
            ResetPlayerState(false);
            LookAtMouse();
            SetKickFlipAvailable(true);
            isAnimationLocked = true;
            anim.SetTrigger(Skill_Kick);
            yield return new WaitUntil(() => _isKickWindowActive);
            while (_isKickWindowActive)
            {
                Collider[] hitColliders = Physics.OverlapSphere(_kickHitPosition.position, _kickHitArea);
                foreach (Collider hitCollider in hitColliders)
                {
                    if (!hitCollider.CompareTag("Counterable")) 
                        continue;
                    
                    // Check if it's any counterable object (boss, stone, etc.)
                    ICounterable counterable = hitCollider.GetComponentInParent<ICounterable>();
                    if (counterable != null)
                        counterable.TriggerCounter();
                }
                yield return null;
            }
        }
        
        private IEnumerator StartSkillFlipKick()
        {
            if (!_isAbleToKickFlip)
                yield break;
            
            anim.SetTrigger(Skill_FlipKick);
            yield return new WaitUntil(() => _isKickFlipping);
            while (_isKickFlipping)
            {
                var playerTransform = transform;
                playerTransform.position += playerTransform.forward * (Time.deltaTime * _flipKickDistance);
                yield return null;
            }
        }

        // The kick icon is highlighted while the flying kick (pressing E again) is available, so players learn about it
        private void SetKickFlipAvailable(bool available)
        {
            _isAbleToKickFlip = available;
            if (_kickSpellIcon)
                _kickSpellIcon.SetHighlighted(available);
        }

        // Used in FlipKick animation
        private void StartFlip() => _isKickFlipping = true;
        
        // Used in Kick and FlipKick animation
        private void StartKickHit()
        {
            _isKickFlipping = false;
            SetKickFlipAvailable(false);
            _isKickWindowActive = true;
        }

        // Used in Kick and FlipKick animation
        private void StopKickHit()
        {
            isAnimationLocked = false;
            _isKickWindowActive = false;
        }

        // Debug for kick counter collider
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(_kickHitPosition.position, _kickHitArea);
        }
    }
}