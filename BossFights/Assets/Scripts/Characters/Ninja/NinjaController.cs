using System.Collections;
using System.Collections.Generic;
using Bosses;
using Cameras;
using Interfaces;
using Shared;
using TMPro;
using UI;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

namespace Characters.Ninja
{
    public class NinjaController : PlayerController
    {
        [Header("")]
        [Header("Ninja")]
        [SerializeField] private QTEPrompt _qtePrompt;
        [SerializeField] private TextMeshProUGUI _QTEAlert;
        [SerializeField] private float _qteTimeLimit = 2f;
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
        [SerializeField] private int _huumaDamage = 10;
        [SerializeField] private float _huumaRange = 26f;
        [SerializeField] private float _huumaSpeed = 25f;
        [SerializeField] private float _huumaTurnSpeed = 1080f;
        [SerializeField] private float _huumaLifetime = 2.5f;
        [SerializeField] private float _huumaCD = 10f;
        [SerializeField] private SpellIcon _huumaSpellIcon;
        [Header("Skill Kick")]
        [SerializeField] private Transform _kickHitPosition;
        [SerializeField] private float _kickHitArea;
        [SerializeField] private float _flipKickDistance;
        [SerializeField] private float _kickCD = 8f;
        [SerializeField] private SpellIcon _kickSpellIcon;
        [Header("Skill Dodge")]
        [SerializeField] private DodgeBubble _dodgeBubblePrefab;
        [SerializeField] private int _castInputsDodge = 3;
        [SerializeField] private float _dodgeDuration = 1f;
        [SerializeField] private float _dodgeDistance = 3.5f;
        [SerializeField] private float _dodgeCD = 12f;
        [SerializeField] private SpellIcon _dodgeSpellIcon;
        [Header("Skill Sonic Blow")]
        [FormerlySerializedAs("_whirlSlashPrefab")] [SerializeField] private GameObject _sonicBlowSlashPrefab;
        [FormerlySerializedAs("_whirlDuration")] [SerializeField] private float _sonicBlowDuration = 1.4f;
        [FormerlySerializedAs("_whirlHits")] [SerializeField] private int _sonicBlowHits = 6;
        [FormerlySerializedAs("_whirlDamage")] [SerializeField] private int _sonicBlowDamage = 2;
        [SerializeField] private int _sonicBlowFinisherDamage = 5;
        [SerializeField] private float _sonicBlowFinisherPause = 0.5f;
        [SerializeField] private float _sonicBlowFinisherReachScale = 1.4f;
        [SerializeField] private float _sonicBlowFinisherSlashScale = 2.2f;
        [SerializeField] private float _sonicBlowFinisherZoom = 1.3f;
        [SerializeField] private float _sonicBlowChargeZoom = 1.12f;
        [SerializeField] private float _sonicBlowLength = 6f;
        [SerializeField] private float _sonicBlowWidth = 3f;
        [SerializeField] private float _sonicBlowTurnSpeed = 720f;
        [SerializeField] private GameObject _sonicBlowKunaiPrefab;
        [SerializeField] private Vector3 _sonicBlowKunaiPosition;
        [SerializeField] private Vector3 _sonicBlowKunaiEuler;
        [SerializeField] private Vector3 _sonicBlowHandEuler;
        [SerializeField] private float _sonicBlowSwingReach = 1f;
        [SerializeField] private float _sonicBlowSwingWidth = 0.5f;
        [SerializeField] private float _sonicBlowSwingLow = 0.8f;
        [SerializeField] private float _sonicBlowSwingHigh = 1.6f;
        [FormerlySerializedAs("_whirlCD")] [SerializeField] private float _sonicBlowCD = 12f;
        [FormerlySerializedAs("_whirlSpellIcon")] [SerializeField] private SpellIcon _sonicBlowSpellIcon;
        [Header("Skill Leap")]
        [SerializeField] private GroundShuriken _leapShurikenPrefab;
        [SerializeField] private int _leapShurikenCount = 5;
        [SerializeField] private int _leapShurikenDamage = 2;
        [SerializeField] private float _leapShurikenRadius = 0.7f;
        [SerializeField] private float _leapShurikenSpacing = 1.3f;
        [SerializeField] private float _leapThrowRange = 30f;
        [SerializeField] private float _leapDistance = 5f;
        [SerializeField] private float _leapHeight = 0.9f;
        [SerializeField] private float _leapRiseTime = 0.4f;
        [SerializeField, Range(0.3f, 0.9f)] private float _leapFlipEnd = 0.64f;
        [SerializeField] private float _leapHangPitch = 20f;
        [SerializeField] private float _leapHangEndHeight = 0.8f;
        [SerializeField] private float _leapTuckThigh = 20f;
        [SerializeField] private float _leapTuckKnee = 20f;
        [SerializeField] private float _leapFallTime = 0.3f;
        [SerializeField] private float _leapQteTime = 1f;
        [SerializeField] private int _leapKunaiCount = 3;
        [SerializeField] private int _leapKunaiDamage = 2;
        [SerializeField, Range(0.05f, 1f)] private float _leapSlowMotion = 0.25f;
        [SerializeField] private float _leapCD = 14f;
        [SerializeField] private SpellIcon _leapSpellIcon;

        private bool _isKickWindowActive;
        private bool _isKickFlipping;
        private bool _isAbleToKickFlip;
        private float _originalHealCD;
        private float _originalClonesCD;
        private float _originalHuumaCD;
        private float _originalKickCD;
        private float _originalDodgeCD;
        private float _dodgeTimeLeft;
        private float _originalSonicBlowCD;
        private float _sonicBlowTimeLeft;
        private bool _isSonicBlowing;
        private bool _isSonicBlowCharged;
        private bool _hadRootMotion;
        private float _sonicBlowElapsed;
        private float _sonicBlowIkWeight;
        private GameObject _sonicBlowKunai;
        private int _sonicBlowHitsDone;
        private readonly List<GameObject> _sonicBlowSlashes = new List<GameObject>();
        private readonly Collider[] _sonicBlowColliders = new Collider[48];
        private readonly HashSet<IDamageableByPlayer> _sonicBlowHitTargets = new HashSet<IDamageableByPlayer>();
        private NavMeshAgent _agent;
        private LeapPhase _leapPhase;
        private float _leapPhaseTime;
        private float _leapQteTimeLeft;
        private float _leapPreviousTimeScale;
        private float _agentBaseOffset;
        private float _leapYaw;
        private float _leapPitch;
        private float _leapFallStartHeight;
        private Transform _hipsBone;
        private Transform _leftUpperLeg;
        private Transform _rightUpperLeg;
        private Transform _leftLowerLeg;
        private Transform _rightLowerLeg;
        private float _leapTuck;
        private readonly List<float> _leapPendingKunai = new List<float>();
        private int _throwArmLayer;
        private float _throwArmTimer;
        private float _throwReachWeight;
        private float _throwReachLength = 0.62f;
        private float _originalLeapCD;
        private bool _isLeapSlowed;
        private bool _leapHadRootMotion;
        private Vector3 _leapDirection;
        private List<KeyCode> _leapKeys;
        private HuumaShuriken _activeHuuma;
        private List<NinjaClone> _activeClones;
        private Vector3 _storedShootTargetPoint;
        private bool _wasShootingLastFrame;
        private bool _isAimingHuuma;
        private bool _hasSwappedWithHuuma;
        private int _currentSkillBeingCast;
        
        private static readonly int Skill_Heal = Animator.StringToHash("Skill_Heal");
        private static readonly int Skill_Clones = Animator.StringToHash("Skill_Clones");
        private static readonly int Skill_Huuma = Animator.StringToHash("Skill_Huuma");
        private static readonly int Skill_Kick = Animator.StringToHash("Skill_Kick");
        private static readonly int Skill_FlipKick = Animator.StringToHash("Skill_FlipKick");
        private static readonly int Skill_Dodge = Animator.StringToHash("Skill_Dodge");
        private static readonly int Skill_Whirlwind = Animator.StringToHash("Skill_Whirlwind");
        private static readonly int Whirlwind = Animator.StringToHash("Whirlwind");
        private static readonly int Skill_Leap = Animator.StringToHash("Skill_Leap");
        private static readonly int Leaping = Animator.StringToHash("Leaping");
        private static readonly int Throw_Kunai = Animator.StringToHash("Throw_Kunai");
        private static readonly int Leap_Speed = Animator.StringToHash("LeapSpeed");
        private static readonly int Leap_Hang = Animator.StringToHash("Leap_Hang");
        private static readonly int CastInput = Animator.StringToHash("CastInput");
        private static readonly int Shooting = Animator.StringToHash("Shooting");
        private const KeyCode HealKey = KeyCode.Q;
        private const KeyCode ClonesKey = KeyCode.W;
        private const KeyCode KickKey = KeyCode.E;
        private const KeyCode HuumaKey = KeyCode.A;
        private const KeyCode DodgeKey = KeyCode.S;
        private const KeyCode SonicBlowKey = KeyCode.F;
        private const KeyCode LeapKey = KeyCode.R;
        private const float LeapHangDrift = 0.4f;
        private const float LeapClipLength = 2.15f;
        private const float LeapPitchSpeed = 180f;
        private const float LeapTuckSpeed = 5f;
        private const float LeapKunaiReleaseDelay = 0.17f;
        private const float ThrowArmDuration = 0.36f;
        private const float LeapKunaiMaxPitch = 20f;
        private const float ThrowReachRise = 0.12f;
        private const float ThrowReachFall = 0.14f;
        private const float ThrowArmFade = 0.1f;
        private static readonly KeyCode[] QteKeyCodes = { KeyCode.Q, KeyCode.W, KeyCode.E, KeyCode.R, KeyCode.A, KeyCode.S, KeyCode.D, KeyCode.F };
        private const float SonicBlowWindUp = 0.1f;
        private const float SonicBlowStrikeStart = 0.2f;
        private const float SonicBlowStrikeFade = 0.05f;
        private const float SonicBlowEffectDistance = 2.2f;
        private const float SonicBlowEffectLifetime = 0.4f;
        private const float SonicBlowSlashTilt = 35f;
        private const float SonicBlowIkBlendSpeed = 12f;
        private const float SonicBlowHalfHeight = 1.5f;
        private const float FinisherChargeTurns = 1.375f;
        private const float FinisherChargeRadiusStart = 0.45f;
        private const float FinisherChargeRadiusEnd = 0.7f;
        private const float FinisherChargeShake = 0.035f;
        private const float FinisherThrustTime = 0.1f;

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

            // Initialize kick cooldown
            _originalKickCD = _kickCD;
            _kickCD = 0;

            // Initialize dodge cooldown
            _originalDodgeCD = _dodgeCD;
            _dodgeCD = 0;
            _agent = GetComponent<NavMeshAgent>();
            _agentBaseOffset = _agent.baseOffset;
            _hipsBone = anim.GetBoneTransform(HumanBodyBones.Hips);
            _throwArmLayer = anim.GetLayerIndex("Throw Upper Body");

            // The reach is the length of the arm, measured on the rig
            Transform upperArm = anim.GetBoneTransform(HumanBodyBones.RightUpperArm);
            Transform lowerArm = anim.GetBoneTransform(HumanBodyBones.RightLowerArm);
            Transform handBone = anim.GetBoneTransform(HumanBodyBones.RightHand);
            _throwReachLength = (Vector3.Distance(upperArm.position, lowerArm.position) + Vector3.Distance(lowerArm.position, handBone.position)) * 0.95f;
            _leftUpperLeg = anim.GetBoneTransform(HumanBodyBones.LeftUpperLeg);
            _rightUpperLeg = anim.GetBoneTransform(HumanBodyBones.RightUpperLeg);
            _leftLowerLeg = anim.GetBoneTransform(HumanBodyBones.LeftLowerLeg);
            _rightLowerLeg = anim.GetBoneTransform(HumanBodyBones.RightLowerLeg);

            // Initialize leap cooldown
            _originalLeapCD = _leapCD;
            _leapCD = 0;

            // Initialize sonic blow cooldown
            _originalSonicBlowCD = _sonicBlowCD;
            _sonicBlowCD = 0;

            _healSpellIcon.SetKeyLabel(HealKey);
            _clonesSpellIcon.SetKeyLabel(ClonesKey);
            _kickSpellIcon.SetKeyLabel(KickKey);
            _dodgeSpellIcon.SetKeyLabel(DodgeKey);
            _sonicBlowSpellIcon.SetKeyLabel(SonicBlowKey);
            _leapSpellIcon.SetKeyLabel(LeapKey);
            _huumaSpellIcon.SetKeyLabel(HuumaKey);
        }
        
        // The hand swings between a low-left and a high-right point, one strike at each end, so the strikes read as a zigzag of slashes
        private void OnAnimatorIK(int layerIndex)
        {
            ApplyThrowReachIk();

            _sonicBlowIkWeight = Mathf.MoveTowards(_sonicBlowIkWeight, _isSonicBlowing ? 1f : 0f, SonicBlowIkBlendSpeed * Time.deltaTime);
            if (_sonicBlowIkWeight <= 0f)
                return;

            float interval = GetSonicBlowInterval();
            float swing = Mathf.SmoothStep(0f, 1f, Mathf.PingPong(Mathf.Max(0f, _sonicBlowElapsed - SonicBlowWindUp) / interval, 1f));
            Vector3 low = transform.position + transform.forward * _sonicBlowSwingReach - transform.right * _sonicBlowSwingWidth + Vector3.up * _sonicBlowSwingLow;
            Vector3 high = transform.position + transform.forward * _sonicBlowSwingReach + transform.right * _sonicBlowSwingWidth + Vector3.up * _sonicBlowSwingHigh;
            Vector3 handTarget = Vector3.Lerp(low, high, swing);

            // Before the finisher the arm charges: it winds up in a growing, accelerating circle overhead and behind, trembling more and more, and then thrusts far forward
            float finisherTime = GetSonicBlowStrikeTime(_sonicBlowHits - 1);
            float chargeStart = GetSonicBlowChargeStart();
            if (_sonicBlowHits > 1 && _sonicBlowElapsed >= chargeStart)
            {
                float charge = Mathf.InverseLerp(chargeStart, finisherTime, _sonicBlowElapsed);
                float angle = Mathf.Pow(charge, 1.6f) * FinisherChargeTurns * 2f * Mathf.PI + 0.75f * Mathf.PI * charge;
                float radius = Mathf.Lerp(FinisherChargeRadiusStart, FinisherChargeRadiusEnd, charge);
                Vector3 center = transform.position + transform.right * 0.6f + Vector3.up * 1.3f;
                Vector3 ring = center + transform.forward * (Mathf.Cos(angle) * radius) + Vector3.up * (Mathf.Sin(angle) * radius);
                ring += new Vector3(Mathf.Sin(Time.time * 70f), Mathf.Sin(Time.time * 83f), Mathf.Sin(Time.time * 61f)) * (FinisherChargeShake * charge);
                handTarget = Vector3.Lerp(handTarget, ring, Mathf.Clamp01(charge * 8f));

                Vector3 thrust = transform.position + transform.forward * (_sonicBlowSwingReach * _sonicBlowFinisherReachScale + 0.5f) + Vector3.up * 1.4f;
                float thrustProgress = Mathf.InverseLerp(finisherTime - 0.02f, finisherTime + FinisherThrustTime, _sonicBlowElapsed);
                handTarget = Vector3.Lerp(handTarget, thrust, Mathf.SmoothStep(0f, 1f, thrustProgress));
            }

            anim.SetIKPositionWeight(AvatarIKGoal.RightHand, _sonicBlowIkWeight);
            anim.SetIKPosition(AvatarIKGoal.RightHand, handTarget);
            anim.SetIKRotationWeight(AvatarIKGoal.RightHand, _sonicBlowIkWeight);
            anim.SetIKRotation(AvatarIKGoal.RightHand, transform.rotation * Quaternion.Euler(_sonicBlowHandEuler));
        }

        // The lean and the knee tuck are applied to the bones after the animation, never to the root: a tilted root makes the NavMeshAgent shift the ninja sideways on every frame
        private void LateUpdate()
        {
            if (_leapPitch != 0f && _hipsBone)
                _hipsBone.rotation = Quaternion.AngleAxis(_leapPitch, transform.right) * _hipsBone.rotation;

            if (_leapTuck > 0f && _leftUpperLeg)
            {
                Vector3 axis = transform.right;
                _leftUpperLeg.rotation = Quaternion.AngleAxis(-_leapTuckThigh * _leapTuck, axis) * _leftUpperLeg.rotation;
                _rightUpperLeg.rotation = Quaternion.AngleAxis(-_leapTuckThigh * _leapTuck, axis) * _rightUpperLeg.rotation;
                _leftLowerLeg.rotation = Quaternion.AngleAxis(_leapTuckKnee * _leapTuck, axis) * _leftLowerLeg.rotation;
                _rightLowerLeg.rotation = Quaternion.AngleAxis(_leapTuckKnee * _leapTuck, axis) * _rightLowerLeg.rotation;
            }
        }

        // The throw clip only moves the arm back (its forward reach comes from the pelvis, which the upper body mask leaves out), so the hand is pushed out toward the aim point with IK around the moment the kunai leaves
        private void ApplyThrowReachIk()
        {
            if (_leapPhase == LeapPhase.None || _throwArmTimer <= 0f)
            {
                if (_leapPhase != LeapPhase.None || _throwReachWeight > 0f)
                {
                    _throwReachWeight = 0f;
                    anim.SetIKPositionWeight(AvatarIKGoal.RightHand, 0f);
                }
                return;
            }

            float elapsed = ThrowArmDuration - _throwArmTimer;
            _throwReachWeight = Mathf.Clamp01(elapsed / ThrowReachRise) * Mathf.Clamp01((ThrowArmDuration - elapsed) / ThrowReachFall);
            Transform shoulder = anim.GetBoneTransform(HumanBodyBones.RightUpperArm);
            Vector3 direction = transform.forward;
            anim.SetIKPositionWeight(AvatarIKGoal.RightHand, _throwReachWeight);
            anim.SetIKPosition(AvatarIKGoal.RightHand, shoulder.position + direction * _throwReachLength);
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
            if (_kickCD > 0)
                _kickCD -= Time.deltaTime;
            if (_dodgeCD > 0)
                _dodgeCD -= Time.deltaTime;
            if (_sonicBlowCD > 0)
                _sonicBlowCD -= Time.deltaTime;
            if (_leapCD > 0)
                _leapCD -= Time.deltaTime;
            if (_leapPhase != LeapPhase.None)
                UpdateLeap();
            if (_throwArmTimer > 0f)
                UpdateThrowArm();
            if (_dodgeTimeLeft > 0)
                DodgeMove();
            if (_sonicBlowTimeLeft > 0)
                UpdateSonicBlow();

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

            // After the QTE the ninja turns toward the mouse while the throw animation plays
            if (_isAimingHuuma)
                TurnTowardsMouse(_huumaTurnSpeed);
        }

        protected override void ResetPlayerState(bool includeCoroutines)
        {
            base.ResetPlayerState(includeCoroutines);
            
            anim.ResetTrigger(Skill_Heal);
            anim.ResetTrigger(Skill_Clones);
            anim.ResetTrigger(Skill_Huuma);
            anim.ResetTrigger(Skill_Dodge);
            anim.ResetTrigger(Skill_Whirlwind);
            StopSonicBlow();
            StopLeap();
            anim.ResetTrigger(Skill_Leap);
            anim.ResetTrigger(Throw_Kunai);
            anim.ResetTrigger(Leap_Hang);
            _isAimingHuuma = false;
            _dodgeTimeLeft = 0f;
            _isKickFlipping = false;
            _isKickWindowActive = false;
            SetKickFlipAvailable(false);
            _QTEAlert.gameObject.SetActive(false);
            _qtePrompt.Hide();
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
            else if (_currentSkillBeingCast == Skill_Dodge)
            {
                _dodgeCD = _originalDodgeCD;
                _dodgeSpellIcon.StartCooldown(_originalDodgeCD);
            }
            
            _currentSkillBeingCast = 0;
        }
        
        /// *** Inputs *** ///
        
        protected override void SkillsInput()
        {
            base.SkillsInput();
            
            if (Input.GetKeyDown(KickKey) && anim.GetCurrentAnimatorStateInfo(0).IsName("Kick"))
                StartCoroutine(StartSkillFlipKick());
            
            // The swap works as soon as the shuriken is thrown, even while it is still in the air or the throw animation is finishing; a QTE in progress comes first, since its keys can include the swap key
            if (Input.GetKeyDown(HuumaKey) && _activeHuuma != null && !_hasSwappedWithHuuma && !anim.GetBool(Casting))
            {
                SwapWithHuuma();
                return;
            }

            if (isAnimationLocked || anim.GetCurrentAnimatorStateInfo(0).IsTag("AnimationLock"))
                return;

            if (Input.GetKeyDown(HealKey) && _healCD <= 0)
                StartCoroutine(CastingSkill(Skill_Heal, _castInputsHeal, false, HealKey));
            else if (Input.GetKeyDown(ClonesKey) && _clonesCD <= 0)
                StartCoroutine(CastingSkill(Skill_Clones, _castInputsClones, false, ClonesKey));
            else if (Input.GetKeyDown(KickKey) && _kickCD <= 0)
                StartCoroutine(SkillKick());
            else if (Input.GetKeyDown(HuumaKey) && _huumaCD <= 0 && _activeHuuma == null)
                StartCoroutine(CastingSkill(Skill_Huuma, _castInputsHuuma, false, HuumaKey));
            else if (Input.GetKeyDown(DodgeKey) && _dodgeCD <= 0)
                StartCoroutine(CastingSkill(Skill_Dodge, _castInputsDodge, false, DodgeKey));
            else if (Input.GetKeyDown(SonicBlowKey) && _sonicBlowCD <= 0)
                StartSonicBlow();
            else if (Input.GetKeyDown(LeapKey) && _leapCD <= 0)
                StartLeap();
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
            KeyCode[] totalKeyCodes = QteKeyCodes;
            var keycodesToCast = GenerateKeycodesToCast(totalKeyCodes, keycodeToRemove, skillCastInputs);
            
            // Show QTE Keycodes in Screen
            _qtePrompt.Show(keycodesToCast);
            
            // This is so the KeyCode to trigger the Cast doesn't enter the loop
            yield return new WaitUntil(() => !Input.GetKeyDown(keycodeToRemove));
            
            // Start QTE: the whole sequence has to be typed before the timer bar runs out
            float timeLeft = _qteTimeLimit;
            while (keycodesToCast.Count > 0)
            {
                timeLeft -= Time.deltaTime;
                _qtePrompt.SetTimeLeft(timeLeft / _qteTimeLimit);
                if (timeLeft <= 0f)
                {
                    FailedQTE();
                    yield break;
                }

                if (Input.GetKeyDown(keycodesToCast[0]))
                {
                    anim.SetTrigger(CastInput);
                    keycodesToCast.RemoveAt(0);
                    _qtePrompt.Advance();
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
            _qtePrompt.Fail();
            _QTEAlert.gameObject.SetActive(false);
        }

        private IEnumerator CompletedQTE(bool targetedSkill)
        {
            _qtePrompt.Complete();
            _isAimingHuuma = _currentSkillBeingCast == Skill_Huuma;
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

            if (_currentSkillBeingCast == Skill_Dodge)
                SkillDodge();
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
            _isAimingHuuma = false;
            LookAtMouse();
            Vector3 origin = transform.position + Vector3.up * 1.2f + transform.forward * 0.8f;
            _activeHuuma = Instantiate(_huumaPrefab);
            _activeHuuma.Begin(origin, transform.forward, _huumaDamage, _huumaRange, _huumaSpeed, _huumaLifetime, transform, OnHuumaEnded, OnHuumaLanded);
            _huumaSpellIcon.SetHighlighted(true);
            _currentSkillBeingCast = 0;
        }

        private void TurnTowardsMouse(float degreesPerSecond)
        {
            if (!GetMouseWorldPoint(out Vector3 mousePoint))
                return;

            Vector3 direction = mousePoint - transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.1f)
                return;

            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(direction), degreesPerSecond * Time.deltaTime);
        }

        // From the moment it lands the shuriken waits for a limited time, so the icon counts it down
        private void OnHuumaLanded(float waitTime) => _huumaSpellIcon.StartActiveTimer(waitTime);

        // The cooldown starts when the shuriken is gone, whether it ran out of time or was used
        private void OnHuumaEnded()
        {
            _activeHuuma = null;
            _huumaSpellIcon.SetHighlighted(false);

            // A swap already started the cooldown, so the shuriken fading away does not restart it
            if (!_hasSwappedWithHuuma)
                StartHuumaCooldown();

            _hasSwappedWithHuuma = false;
        }

        private void StartHuumaCooldown()
        {
            _huumaCD = _originalHuumaCD;
            _huumaSpellIcon.StartCooldown(_originalHuumaCD);
        }

        // The player goes where the shuriken is and the shuriken takes the player's place, with a smoke puff at both spots (one swap per throw)
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

            // The swap is used up: the cooldown starts now and the shuriken just waits where the player was until it fades
            _hasSwappedWithHuuma = true;
            _huumaSpellIcon.SetHighlighted(false);
            StartHuumaCooldown();
            _activeHuuma.RelocateTo(from);
        }

        /// *** Dodge *** ///

        // It starts right after the last QTE key, so the immunity is there for the moment the player was reacting to
        private void SkillDodge()
        {
            SetInvulnerable(_dodgeDuration);
            _dodgeTimeLeft = _dodgeDuration;
            if (_dodgeBubblePrefab)
                Instantiate(_dodgeBubblePrefab, transform).Begin(_dodgeDuration);

            _dodgeCD = _originalDodgeCD;
            _dodgeSpellIcon.StartCooldown(_originalDodgeCD);
            _currentSkillBeingCast = 0;
        }

        // The ninja hops back while the dodge lasts, fast at first and then slowing down; the agent keeps it on the NavMesh
        private void DodgeMove()
        {
            float speed = _dodgeDistance / _dodgeDuration * 2f * (_dodgeTimeLeft / _dodgeDuration);
            _dodgeTimeLeft -= Time.deltaTime;
            _agent.Move(-transform.forward * (speed * Time.deltaTime));
        }

        /// *** Sonic Blow *** ///

        // The ninja turns to the mouse and unleashes _sonicBlowHits fast strikes in a box in front of it over _sonicBlowDuration, standing in place. It gives no immunity, and getting hit interrupts it
        private void StartSonicBlow()
        {
            ResetPlayerState(false);
            LookAtMouse();
            isAnimationLocked = true;
            _isSonicBlowing = true;
            // The punch clip turns its root, which would spin the ninja away from the mouse direction
            _hadRootMotion = anim.applyRootMotion;
            anim.applyRootMotion = false;
            _sonicBlowTimeLeft = _sonicBlowDuration;
            _sonicBlowElapsed = 0f;
            _sonicBlowHitsDone = 0;
            _isSonicBlowCharged = false;
            AttachSonicBlowKunai();
            anim.SetBool(Whirlwind, true);
            anim.SetTrigger(Skill_Whirlwind);

            _sonicBlowCD = _originalSonicBlowCD;
            _sonicBlowSpellIcon.StartCooldown(_originalSonicBlowCD);
        }

        private float GetSonicBlowInterval() => (_sonicBlowDuration - SonicBlowWindUp - _sonicBlowFinisherPause) / Mathf.Max(1, _sonicBlowHits);

        // The charge starts half an interval after the last normal strike and lasts until the finisher
        private float GetSonicBlowChargeStart() => GetSonicBlowStrikeTime(Mathf.Max(0, _sonicBlowHits - 2)) + GetSonicBlowInterval() * 0.5f;

        // The strikes are evenly spaced after the wind-up, and the last one (the finisher) comes after an extra pause
        private float GetSonicBlowStrikeTime(int index) => SonicBlowWindUp + index * GetSonicBlowInterval() + (index == _sonicBlowHits - 1 ? _sonicBlowFinisherPause : 0f);

        private void UpdateSonicBlow()
        {
            _sonicBlowTimeLeft -= Time.deltaTime;
            float elapsed = _sonicBlowDuration - _sonicBlowTimeLeft;
            _sonicBlowElapsed = elapsed;

            // The ninja keeps following the mouse while it strikes
            TurnTowardsMouse(_sonicBlowTurnSpeed);

            // The camera starts to close in slowly while the arm charges, so the finisher is the end of a build-up
            if (!_isSonicBlowCharged && _sonicBlowHits > 1 && elapsed >= GetSonicBlowChargeStart())
            {
                _isSonicBlowCharged = true;
                ZoomCamera(_sonicBlowChargeZoom, GetSonicBlowStrikeTime(_sonicBlowHits - 1) - elapsed, 0.4f, 0.35f);
            }

            while (_sonicBlowHitsDone < _sonicBlowHits && elapsed >= GetSonicBlowStrikeTime(_sonicBlowHitsDone))
            {
                SonicBlowStrike();
                _sonicBlowHitsDone++;
            }

            if (_sonicBlowTimeLeft <= 0f)
            {
                StopSonicBlow();
                isAnimationLocked = false;
            }
        }

        private void SonicBlowStrike()
        {
            // Every strike blends into a fresh punch past its wind-up, so the jabs pile up into a flurry without popping
            anim.CrossFadeInFixedTime(Skill_Whirlwind, SonicBlowStrikeFade, 0, SonicBlowStrikeStart);

            // The last strike is the finisher: a longer, wider reach, a bigger slash and a camera zoom-in
            bool isFinisher = _sonicBlowHitsDone == _sonicBlowHits - 1;
            float reach = isFinisher ? _sonicBlowFinisherReachScale : 1f;
            int damage = isFinisher ? _sonicBlowFinisherDamage : _sonicBlowDamage;

            Vector3 forward = transform.forward;
            Vector3 center = transform.position + forward * (_sonicBlowLength * reach * 0.5f);
            Vector3 effectPosition = transform.position + forward * SonicBlowEffectDistance * reach + Vector3.up;
            Vector3 halfExtents = new Vector3(_sonicBlowWidth * reach * 0.5f, SonicBlowHalfHeight, _sonicBlowLength * reach * 0.5f);

            _sonicBlowHitTargets.Clear();
            int count = Physics.OverlapBoxNonAlloc(center + Vector3.up, halfExtents, _sonicBlowColliders, transform.rotation);
            for (int i = 0; i < count; i++)
            {
                IDamageableByPlayer damageable = _sonicBlowColliders[i].GetComponentInParent<IDamageableByPlayer>();
                if (damageable != null && _sonicBlowHitTargets.Add(damageable))
                    damageable.TakeDamage(DamageRoll.Roll(damage, out bool isCrit), isCrit);
            }

            if (_sonicBlowSlashPrefab)
            {
                // The slashes alternate their tilt with the swing, so they cross each other; the finisher is a big straight one
                float tilt = isFinisher ? 0f : _sonicBlowHitsDone % 2 == 0 ? SonicBlowSlashTilt : -SonicBlowSlashTilt;
                Quaternion rotation = Quaternion.LookRotation(forward) * Quaternion.Euler(0f, 0f, tilt);
                GameObject slash = Instantiate(_sonicBlowSlashPrefab, effectPosition, rotation);
                if (isFinisher)
                    slash.transform.localScale *= _sonicBlowFinisherSlashScale;
                Destroy(slash, SonicBlowEffectLifetime);
                _sonicBlowSlashes.Add(slash);
            }

            if (isFinisher)
                ZoomCamera(_sonicBlowFinisherZoom, 0.07f, 0.15f, 0.35f);
        }

        private void ZoomCamera(float factor, float inTime, float holdTime, float outTime)
        {
            Camera mainCamera = Camera.main;
            if (!mainCamera)
                return;

            CameraZoomPunch zoom = mainCamera.GetComponent<CameraZoomPunch>();
            if (!zoom)
                zoom = mainCamera.gameObject.AddComponent<CameraZoomPunch>();

            zoom.Punch(factor, inTime, holdTime, outTime);
        }

        private void AttachSonicBlowKunai()
        {
            if (!_sonicBlowKunaiPrefab || _sonicBlowKunai)
                return;

            Transform hand = anim.GetBoneTransform(HumanBodyBones.RightHand);
            _sonicBlowKunai = Instantiate(_sonicBlowKunaiPrefab, hand);
            _sonicBlowKunai.transform.SetLocalPositionAndRotation(_sonicBlowKunaiPosition, Quaternion.Euler(_sonicBlowKunaiEuler));
        }

        private void StopSonicBlow()
        {
            if (!_isSonicBlowing)
                return;

            _isSonicBlowing = false;
            anim.applyRootMotion = _hadRootMotion;

            // The effects end with the skill instead of fading out on their own
            foreach (GameObject slash in _sonicBlowSlashes)
            {
                if (slash)
                    Destroy(slash);
            }
            _sonicBlowSlashes.Clear();
            if (_sonicBlowKunai)
                Destroy(_sonicBlowKunai);

            _sonicBlowTimeLeft = 0f;
            anim.SetBool(Whirlwind, false);
        }

        /// *** Leap *** ///

        // Throws shurikens at the mouse point and jumps away from it: a quick rise, a slow-motion hang with a short QTE that throws a kunai at the mouse per key, then the fall. Missing a key or running out of time only ends the shooting
        private void StartLeap()
        {
            ResetPlayerState(false);
            LookAtMouse();
            isAnimationLocked = true;
            _leapHadRootMotion = anim.applyRootMotion;
            anim.applyRootMotion = false;
            anim.SetBool(Leaping, true);

            // The backflip is played fast during the rise, so it is over when the slow motion starts; the speed comes from the animator parameter
            anim.SetFloat(Leap_Speed, _leapFlipEnd * LeapClipLength / _leapRiseTime);
            anim.SetTrigger(Skill_Leap);

            _leapYaw = transform.eulerAngles.y;
            _leapPitch = 0f;
            _leapPhase = LeapPhase.Rise;
            _leapPhaseTime = 0f;
            _leapDirection = -transform.forward;
            ThrowLeapShurikens();

            _leapCD = _originalLeapCD;
            _leapSpellIcon.StartCooldown(_originalLeapCD);
        }

        private void ThrowLeapShurikens()
        {
            if (!_leapShurikenPrefab)
                return;

            Vector3 aim = transform.forward;
            float distance = _leapThrowRange;
            if (GetMouseAimPoint(out Vector3 mousePoint))
            {
                Vector3 toMouse = mousePoint - transform.position;
                toMouse.y = 0f;
                if (toMouse.sqrMagnitude > 0.01f)
                {
                    aim = toMouse.normalized;
                    distance = Mathf.Min(toMouse.magnitude, _leapThrowRange);
                }
            }

            Vector3 origin = transform.position + Vector3.up;
            Vector3 center = transform.position + aim * distance;
            Vector3 side = Vector3.Cross(Vector3.up, aim);
            for (int i = 0; i < _leapShurikenCount; i++)
            {
                // A shallow arc around the mouse point, curving back toward the ninja
                float lateral = (i - (_leapShurikenCount - 1) * 0.5f) * _leapShurikenSpacing;
                Vector3 spot = center + side * lateral - aim * (Mathf.Abs(lateral) * 0.3f);
                spot = NavMesh.SamplePosition(spot, out NavMeshHit navHit, 3f, NavMesh.AllAreas) ? navHit.position : new Vector3(spot.x, transform.position.y, spot.z);
                Instantiate(_leapShurikenPrefab).Begin(origin, spot, _leapShurikenDamage, _leapShurikenRadius);
            }
        }

        private void UpdateLeap()
        {
            _leapTuck = Mathf.MoveTowards(_leapTuck, _leapPhase == LeapPhase.Hang ? 1f : 0f, LeapTuckSpeed * Time.unscaledDeltaTime);
            UpdatePendingKunai();

            switch (_leapPhase)
            {
                case LeapPhase.Rise:
                    _leapPhaseTime += Time.deltaTime;
                    float rise = Mathf.Clamp01(_leapPhaseTime / _leapRiseTime);
                    MoveLeap(_leapDirection * (_leapDistance / _leapRiseTime * Time.deltaTime), _leapHeight * Mathf.Sin(rise * Mathf.PI * 0.5f));
                    if (rise >= 1f)
                        StartLeapQte();
                    break;
                case LeapPhase.Hang:
                    TurnLeap(-_leapHangPitch);

                    // The ninja sinks slowly while the QTE runs, so the hang looks like a slow fall and not a frozen pose
                    float sink = 1f - Mathf.Clamp01(_leapQteTimeLeft / _leapQteTime);
                    MoveLeap(_leapDirection * (LeapHangDrift * Time.deltaTime), Mathf.Lerp(_leapHeight, _leapHangEndHeight, sink));
                    UpdateLeapQte();
                    break;
                case LeapPhase.Fall:
                    _leapPhaseTime += Time.deltaTime;
                    float fall = Mathf.Clamp01(_leapPhaseTime / _leapFallTime);
                    TurnLeap(0f);
                    MoveLeap(Vector3.zero, _leapFallStartHeight * (1f - fall * fall));
                    if (fall >= 1f)
                    {
                        StopLeap();
                        isAnimationLocked = false;
                    }
                    break;
            }
        }

        // The ninja holds its direction and leans back while it hangs, as if the throws pushed it away; it only turns when a kunai is thrown
        private void TurnLeap(float targetPitch)
        {
            _leapPitch = Mathf.MoveTowards(_leapPitch, targetPitch, LeapPitchSpeed * Time.unscaledDeltaTime);
            transform.rotation = Quaternion.Euler(0f, _leapYaw, 0f);
        }

        // The agent keeps the ninja on the NavMesh while the base offset lifts it into the air
        private void MoveLeap(Vector3 delta, float height)
        {
            _agent.Move(delta);
            _agent.baseOffset = _agentBaseOffset + height;
        }

        private void StartLeapQte()
        {
            _leapPhase = LeapPhase.Hang;
            anim.SetTrigger(Leap_Hang);
            _leapKeys = GenerateKeycodesToCast(QteKeyCodes, LeapKey, _leapKunaiCount);
            _leapQteTimeLeft = _leapQteTime;
            _qtePrompt.Show(_leapKeys);

            _leapPreviousTimeScale = Time.timeScale;
            Time.timeScale = _leapSlowMotion;
            _isLeapSlowed = true;

            // The ninja moves at normal speed inside the slow motion, so her skill feels fast
            anim.updateMode = AnimatorUpdateMode.UnscaledTime;
        }

        // The QTE timer runs on real time, so the slow motion does not stretch it
        private void UpdateLeapQte()
        {
            _leapQteTimeLeft -= Time.unscaledDeltaTime;
            _qtePrompt.SetTimeLeft(_leapQteTimeLeft / _leapQteTime);
            if (_leapQteTimeLeft <= 0f)
            {
                EndLeapQte(false);
                return;
            }

            if (Input.GetKeyDown(_leapKeys[0]))
            {
                ThrowLeapKunai();
                _leapKeys.RemoveAt(0);
                _qtePrompt.Advance();
                if (_leapKeys.Count == 0)
                    EndLeapQte(true);
                return;
            }

            foreach (KeyCode key in QteKeyCodes)
            {
                if (key != LeapKey && Input.GetKeyDown(key))
                {
                    EndLeapQte(false);
                    return;
                }
            }
        }

        private void EndLeapQte(bool completed)
        {
            if (completed)
                _qtePrompt.Complete();
            else
                _qtePrompt.Fail();

            RestoreLeapTime();
            _leapFallStartHeight = _agent.baseOffset - _agentBaseOffset;
            _leapPhase = LeapPhase.Fall;
            _leapPhaseTime = 0f;
        }

        // The throw animation starts at the key press and the kunai leaves when the arm is stretched, like the basic attack; the animation runs on real time in the hang, so it stays fast in the slow motion
        private void ThrowLeapKunai()
        {
            FaceMouseForLeapThrow();
            anim.SetTrigger(Throw_Kunai);
            anim.SetLayerWeight(_throwArmLayer, 1f);
            _throwArmTimer = ThrowArmDuration;
            _leapPendingKunai.Add(LeapKunaiReleaseDelay);
        }

        // The throw plays on a layer that only has the upper body, so the legs keep the tucked fall pose; the layer only has weight while a throw plays (an idle layer left at full weight holds the last throw pose)
        private void UpdateThrowArm()
        {
            if (_throwArmTimer <= 0f)
                return;

            _throwArmTimer -= Time.unscaledDeltaTime;
            anim.SetLayerWeight(_throwArmLayer, Mathf.Clamp01(_throwArmTimer / ThrowArmFade));
        }

        private void ResetThrowArm()
        {
            _throwArmTimer = 0f;
            if (_throwArmLayer > 0)
                anim.SetLayerWeight(_throwArmLayer, 0f);
        }

        private void UpdatePendingKunai()
        {
            for (int i = _leapPendingKunai.Count - 1; i >= 0; i--)
            {
                _leapPendingKunai[i] -= Time.unscaledDeltaTime;
                if (_leapPendingKunai[i] > 0f)
                    continue;

                _leapPendingKunai.RemoveAt(i);
                ReleaseLeapKunai();
            }
        }

        // Like the basic attack, the ninja turns to the mouse only when it shoots (a flat turn, instant)
        private void FaceMouseForLeapThrow()
        {
            if (!GetMouseAimPoint(out Vector3 mousePoint))
                return;

            Vector3 direction = mousePoint - transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.1f)
                return;

            _leapYaw = Quaternion.LookRotation(direction).eulerAngles.y;
            transform.rotation = Quaternion.Euler(0f, _leapYaw, 0f);
        }

        // The kunai fly straight ahead, level. The hand is higher than a boss' body, so when the mouse is over something that can be hurt the kunai tilts down just enough to reach that point (never more than LeapKunaiMaxPitch); aimed at the ground it stays level and never dives into the floor
        private Vector3 GetLeapKunaiDirection(Vector3 origin)
        {
            Vector3 forward = transform.forward;
            if (!GetMouseAimHit(out RaycastHit hit) || hit.collider.GetComponentInParent<IDamageableByPlayer>() == null)
                return forward;

            Vector3 toHit = hit.point - origin;
            toHit.y = 0f;
            float flatDistance = Mathf.Max(toHit.magnitude, 0.5f);
            float pitch = Mathf.Clamp(Mathf.Atan2(hit.point.y - origin.y, flatDistance) * Mathf.Rad2Deg, -LeapKunaiMaxPitch, 0f);
            return Quaternion.AngleAxis(-pitch, transform.right) * forward;
        }

        private void ReleaseLeapKunai()
        {
            // It leaves from the hand that throws it, like the basic attack leaves from the shooting hand
            Vector3 origin = anim.GetBoneTransform(HumanBodyBones.RightHand).position;
            ShootBasicAttack(origin, GetLeapKunaiDirection(origin), _leapKunaiDamage);
        }

        private void RestoreLeapTime()
        {
            if (!_isLeapSlowed)
                return;

            _isLeapSlowed = false;
            Time.timeScale = _leapPreviousTimeScale;
            anim.updateMode = AnimatorUpdateMode.Normal;
        }

        // Also the way out when something interrupts the leap in the air: it puts the ninja back on the ground and the time back to normal
        private void StopLeap()
        {
            if (_leapPhase == LeapPhase.None)
                return;

            _leapPhase = LeapPhase.None;
            RestoreLeapTime();
            _agent.baseOffset = _agentBaseOffset;
            anim.SetBool(Leaping, false);
            anim.applyRootMotion = _leapHadRootMotion;
            _leapPitch = 0f;
            _leapTuck = 0f;
            _leapPendingKunai.Clear();
            ResetThrowArm();
            transform.rotation = Quaternion.Euler(0f, _leapYaw, 0f);
        }

        /// *** Kick *** ///

        private IEnumerator SkillKick()
        {
            // May be overkill here, but it is needed to reset some animator booleans 
            ResetPlayerState(false);
            LookAtMouse();
            SetKickFlipAvailable(true);
            _kickCD = _originalKickCD;
            _kickSpellIcon.StartCooldown(_originalKickCD);
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

        private enum LeapPhase
        {
            None,
            Rise,
            Hang,
            Fall
        }
    }
}