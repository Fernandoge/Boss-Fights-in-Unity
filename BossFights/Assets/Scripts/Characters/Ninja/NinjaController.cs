using System.Collections;
using System.Collections.Generic;
using Interfaces;
using Shared;
using TMPro;
using UI;
using UnityEngine;
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
        [Header("Skill Wall")] 
        [SerializeField] private GameObject _wallPrefab;
        [SerializeField] private int _castInputsWall;
        [Header("Skill Clones")]
        [SerializeField] private GameObject _clonePrefab;
        [SerializeField] private int _castInputsClones;
        [SerializeField] private float _clonesDuration;
        [SerializeField] private float _clonesCD;
        [SerializeField] private SpellIcon _clonesSpellIcon;
        [Header("Skill Kick")] 
        [SerializeField] private Transform _kickHitPosition;
        [SerializeField] private float _kickHitArea;
        [SerializeField] private float _flipKickDistance;
        [Header("Skill Katon")] 
        [SerializeField] private GameObject _katonPrefab;
        [SerializeField] private Transform _fireballSpawnPoint;
        [SerializeField] private float _fireballSpeed;
        [SerializeField] private int[] _castInputsKaton;
        [SerializeField] private int[] _katonDamage;

        private GameObject _wallParticles;
        private bool _isKickWindowActive;
        private bool _isKickFlipping;
        private bool _isAbleToKickFlip;
        private float _originalHealCD;
        private float _originalClonesCD;
        private List<NinjaClone> _activeClones;
        private Vector3 _storedShootTargetPoint;
        private bool _wasShootingLastFrame;
        private int _currentSkillBeingCast;
        
        private static readonly int Skill_Heal = Animator.StringToHash("Skill_Heal");
        private static readonly int Skill_Wall = Animator.StringToHash("Skill_Wall");
        private static readonly int Skill_Clones = Animator.StringToHash("Skill_Clones");
        private static readonly int Skill_Kick = Animator.StringToHash("Skill_Kick");
        private static readonly int Skill_Katon = Animator.StringToHash("Skill_Katon");
        private static readonly int Skill_FlipKick = Animator.StringToHash("Skill_FlipKick");
        private static readonly int CastInput = Animator.StringToHash("CastInput");
        private static readonly int Shooting = Animator.StringToHash("Shooting");

        /// *** Unity Events *** ///
        
        protected override void Start()
        {
            base.Start();
            _wallParticles = _wallPrefab.GetComponentInChildren<ParticleSystem>().gameObject;
            
            // Initialize heal cooldown
            _originalHealCD = _healCD;
            _healCD = 0;
            
            // Initialize clones cooldown
            _originalClonesCD = _clonesCD;
            _clonesCD = 0;
            _activeClones = new List<NinjaClone>();
        }
        
        /// *** Base Methods *** ///

        protected override void PlayerCooldowns()
        {
            base.PlayerCooldowns();

            if (_healCD > 0)
                _healCD -= Time.deltaTime;
            if (_clonesCD > 0)
                _clonesCD -= Time.deltaTime;

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
            anim.ResetTrigger(Skill_Wall);
            anim.ResetTrigger(Skill_Clones);
            _isKickFlipping = false;
            _isKickWindowActive = false;
            _isAbleToKickFlip = false;
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
            
            _currentSkillBeingCast = 0;
        }
        
        /// *** Inputs *** ///
        
        protected override void SkillsInput()
        {
            base.SkillsInput();
            
            if (Input.GetKeyDown(KeyCode.E) && anim.GetCurrentAnimatorStateInfo(0).IsName("Kick"))
                StartCoroutine(StartSkillFlipKick());
            
            if (isAnimationLocked || anim.GetCurrentAnimatorStateInfo(0).IsTag("AnimationLock"))
                return;
            
            if (Input.GetKeyDown(KeyCode.Q) && _healCD <= 0)
                StartCoroutine(CastingSkill(Skill_Heal, _castInputsHeal, false, KeyCode.Q));
            else if (Input.GetKeyDown(KeyCode.W) && _clonesCD <= 0)
                StartCoroutine(CastingSkill(Skill_Clones, _castInputsClones, false, KeyCode.W));
            else if (Input.GetKeyDown(KeyCode.D))
                StartCoroutine(CastingSkill(Skill_Wall, _castInputsWall, true, KeyCode.D));
            else if (Input.GetKeyDown(KeyCode.E))
                StartCoroutine(SkillKick());
            else if (Input.GetKeyDown(KeyCode.A))
                StartCoroutine(CastingSkill(Skill_Katon, _castInputsKaton[0], true, KeyCode.A, 
                    _castInputsKaton.Length));
        }
        
        /// *** Skill Casting *** ///
        
        private IEnumerator CastingSkill(int skillToTrigger, int skillCastInputs, bool targetedSkill, 
            KeyCode keycodeToRemove, int chargesRemaining = 0)
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

            if (skillToTrigger == Skill_Katon && chargesRemaining > 1)
            {
                var katonQTE= StartCoroutine(CastingSkill(skillToTrigger, _castInputsKaton[_castInputsKaton.Length - chargesRemaining + 1], 
                    targetedSkill, keycodeToRemove, chargesRemaining - 1));
                StartCoroutine(ChargedKatonQTE(_castInputsKaton.Length - chargesRemaining + 1, katonQTE));
            }
            else
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

        /// *** Wall *** ///
        
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

        /// *** Kick *** ///

        private IEnumerator SkillKick()
        {
            // May be overkill here, but it is needed to reset some animator booleans 
            ResetPlayerState(false);
            LookAtMouse();
            _isAbleToKickFlip = true;
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
            isAnimationLocked = false;
            _isKickWindowActive = false;
        }

        // Debug for kick counter collider
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(_kickHitPosition.position, _kickHitArea);
        }

        /// *** Katon *** ///
        
        private IEnumerator ChargedKatonQTE(int chargeCount, Coroutine QTEInProgress = null)
        {
            _QTEAlert.gameObject.SetActive(true);
            _QTEAlert.text = chargeCount switch
            {
                1 => "Click! (1/3)",
                2 => "Click! (2/3)",
                _ => _QTEAlert.text
            };
            yield return new WaitUntil(() => Input.GetMouseButton(0));
            
            _QTEText.transform.parent.gameObject.SetActive(false);
            LookAtMouse();
            _QTEAlert.gameObject.SetActive(false);
            anim.SetBool(Casting, false);
            isAnimationLocked = false;
            if (QTEInProgress != null)
                StopCoroutine(QTEInProgress);
            
            //Debug
            print("click completed Katon coroutine");
        }
        
        // Used in Skill_Katon animation
        // TODO: Make different sizes of fireball depending on the charge
        // TODO: Add damage collision and different damage values depending on the charge
        private void SkillKaton()
        {
            GameObject fireball = Instantiate(_katonPrefab, _fireballSpawnPoint.position, _fireballSpawnPoint.rotation);
            Vector3 fireballPosition = fireball.transform.position;
            Vector3 fireballDirection = _fireballSpawnPoint.forward;
            var fireballScript = fireball.GetComponentInChildren<Projectile>();
            fireballScript.Shoot(_fireballSpeed, fireballPosition, fireballDirection);
        }
    }
}