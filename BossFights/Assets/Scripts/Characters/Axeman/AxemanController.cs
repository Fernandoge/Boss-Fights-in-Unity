using System;
using System.Collections.Generic;
using Cameras;
using Characters.Ninja;
using Interfaces;
using Shared;
using UI;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Serialization;

namespace Characters.Axeman
{
    // A fast melee fighter built on cancels. Every attack and skill is an AxeMove: it strikes early, and from its Cancel Time the next input (an attack, a skill, the dash or a move click) cuts the rest of the animation short.
    // Moves started by cancelling the previous one build a chain that adds damage. Inputs pressed a moment too early are remembered and fire the instant the cancel opens.
    // The Mixamo axe clips turn the body (up to a full spin), so the clip's root rotation is played and each move is aimed so that the strike lands where the cursor is.
    public class AxemanController : PlayerController
    {
        [Header("")]
        [Header("Axeman")]
        [SerializeField] private float _inputBuffer = 0.35f;
        [SerializeField] private float _crossFadeTime = 0.08f;
        [SerializeField] private float _hitStopTime = 0.05f;
        [SerializeField] private float _hurtLockTime = 0.45f;
        [SerializeField] private float _lungeStopDistance = 1.3f;
        [SerializeField] private TrailRenderer _bladeTrail;
        [SerializeField] private TrailRenderer _bladeTrailLeft;
        [SerializeField] private Canvas _skillBar;
        [SerializeField] private GameObject _impactEffect;
        [SerializeField] private float _impactEffectScale = 1f;
        [Header("Chain")]
        [SerializeField] private float _chainGraceTime = 0.4f;
        [SerializeField] private float _chainDamageBonus = 0f;
        [SerializeField] private int _chainMaxSteps = 5;
        [Header("Basic Attack (stands still)")]
        [SerializeField] private AxeMove _basicHit1 = new AxeMove { Name = "Basic 1 (horizontal sweep)", State = "Axe Slash Horizontal", ClipStart = 0.35f, Speed = 1.6f, HitTime = 1.01f, CancelTime = 1.2f, EndTime = 1.5f, YawToHit = -174f, Damage = 2, Radius = 3.2f, Arc = 160f, HitForwardOffset = 0.8f, NoImpactEffect = true, TrailStartTime = 0.78f, TrailEndTime = 1.2f };
        [SerializeField] private AxeMove _basicHit2 = new AxeMove { Name = "Basic 2 (backhand sweep)", State = "Axe Combo 3", ClipStart = 1.45f, Speed = 1.6f, HitTime = 1.75f, CancelTime = 1.95f, EndTime = 2.15f, YawToHit = 146f, Damage = 2, Radius = 3.2f, Arc = 160f, HitForwardOffset = 0.8f, NoImpactEffect = true, TrailStartTime = 1.58f, TrailEndTime = 1.95f };
        [SerializeField] private AxeMove _basicHit3 = new AxeMove { Name = "Basic 3 (overhead chop)", State = "Axe Slash Downward", ClipStart = 0.3f, Speed = 1.6f, HitTime = 0.93f, CancelTime = 1.05f, EndTime = 1.4f, YawToHit = -127f, Damage = 2, Radius = 3f, Arc = 120f, HitForwardOffset = 1.2f, CameraPunch = 1.04f, NoImpactEffect = true, TrailStartTime = 0.7f, TrailEndTime = 1.05f };
        [SerializeField] private float _basicComboResetTime = 0.7f;
        [SerializeField] private bool _skillsPrimeFinisher = true;
        [Header("Skill Q - Second Wind (mash the key)")]
        [SerializeField] private int _healPresses = 10;
        [SerializeField] private float _healTimeLimit = 4f;
        [SerializeField] private int _healAmount = 1;
        [SerializeField] private float _healCD = 8f;
        // The channel loops only the gather of the battlecry (the bend in and the rise, before the head goes back); every press feeds it power (a faster loop and a pump), and the last press plays the whole scream out
        [SerializeField] private string _healChannelState = "Axe Taunt Battlecry";
        [SerializeField] private float _healLoopStart = 0.05f;
        [SerializeField] private float _healLoopEnd = 0.45f;
        [SerializeField] private float _healChannelSpeed = 1f;
        [SerializeField] private float _healMaxChannelSpeed = 2.2f;
        [SerializeField] private float _healPumpBoost = 1.5f;
        [SerializeField] private float _healPumpDecay = 6f;
        [SerializeField] private AxeMove _healRise = new AxeMove { Name = "Second Wind (scream)", State = "Axe Taunt Battlecry", ClipStart = 0.45f, Speed = 1.1f, HitTime = 1.55f, CancelTime = 2.2f, EndTime = 2.7f, TurnsToMouse = false };
        [SerializeField] private SpellIcon _healIcon;
        [SerializeField] private SmashPrompt _smashPrompt;
        [SerializeField] private GameObject _healEffect;
        [Header("Skill W - Rushing Cleave")]
        [SerializeField] private AxeMove _rushCast1 = new AxeMove { Name = "Rush 1", State = "Axe Slash Horizontal", ClipStart = 0.35f, Speed = 2.3f, HitTime = 1.01f, CancelTime = 1.12f, EndTime = 1.4f, YawToHit = -174f, Damage = 2, Radius = 3.2f, Arc = 160f, HitForwardOffset = 0.8f, LungeDistance = 5.5f, LungeEndTime = 1.01f, TrailStartTime = 0.78f, TrailEndTime = 1.2f };
        [SerializeField] private AxeMove _rushCast2 = new AxeMove { Name = "Rush 2", State = "Axe Combo 3", ClipStart = 1.45f, Speed = 1.75f, HitTime = 1.75f, CancelTime = 1.9f, EndTime = 2.1f, YawToHit = 146f, Damage = 2, Radius = 3.2f, Arc = 160f, HitForwardOffset = 0.8f, LungeDistance = 5.5f, LungeEndTime = 1.75f, TrailStartTime = 1.58f, TrailEndTime = 1.95f };
        [SerializeField] private AxeMove _rushCast3 = new AxeMove { Name = "Rush 3 (spin)", State = "Axe Spin High", ClipStart = 0.55f, Speed = 2f, HitTime = 1.05f, CancelTime = 1.3f, EndTime = 1.95f, YawToHit = 213f, Damage = 4, Radius = 4.2f, Arc = 360f, CameraPunch = 1.06f, LungeDistance = 6f, LungeEndTime = 1.05f, TrailStartTime = 0.85f, TrailEndTime = 1.45f };
        [SerializeField] private float _rushWindow = 3f;
        [SerializeField] private float _rushCD = 5f;
        [SerializeField] private SpellIcon _rushIcon;
        [Header("Skill F - Hammer Kick")]
        [SerializeField] private AxeMove _kick = new AxeMove { Name = "Hammer Kick", State = "Axe Kick 1", ClipStart = 0.2f, Speed = 1.5f, HitTime = 0.57f, CancelTime = 0.75f, EndTime = 1.05f, YawToHit = -21f, Damage = 2, Radius = 2.8f, Arc = 100f, HitForwardOffset = 1.2f, CountersBoss = true, LungeDistance = 2.2f, LungeEndTime = 0.57f, InvulnerableTime = 0.2f };
        [SerializeField] private float _kickCD = 6f;
        [SerializeField] private SpellIcon _kickIcon;
        [Header("Skill E - Shield Dash")]
        [SerializeField] private AxeMove _shieldDash = new AxeMove { Name = "Shield Roll Forward", State = "Shield Charge", ClipStart = 0.05f, Speed = 2.2f, HitTime = 0f, CancelTime = 1f, EndTime = 1.3f, LungeDistance = 5.2f, LungeEndTime = 1.25f, InvulnerableTime = 0.55f, UsesClipMotion = true, ClipTravel = 4.59f };
        [SerializeField] private AxeMove _shieldDashLeft = new AxeMove { Name = "Shield Dodge Left", State = "Shield Dodge Left", ClipStart = 0.1f, Speed = 1.7f, HitTime = 0f, CancelTime = 0.75f, EndTime = 1f, TurnsToMouse = false, KeepsFacing = true, LungeDistance = 5.2f, LungeEndTime = 0.92f, InvulnerableTime = 0.5f, UsesClipMotion = true, ClipTravel = 2.6f };
        [SerializeField] private AxeMove _shieldDashRight = new AxeMove { Name = "Shield Dodge Right", State = "Shield Dodge Right", ClipStart = 0.1f, Speed = 1.7f, HitTime = 0f, CancelTime = 0.75f, EndTime = 1f, TurnsToMouse = false, KeepsFacing = true, LungeDistance = 5.2f, LungeEndTime = 0.92f, InvulnerableTime = 0.5f, UsesClipMotion = true, ClipTravel = 2.6f };
        [SerializeField] private AxeMove _shieldDashBack = new AxeMove { Name = "Shield Dodge Back", State = "Shield Dodge Back", ClipStart = 0.1f, Speed = 1.7f, HitTime = 0f, CancelTime = 0.75f, EndTime = 1f, TurnsToMouse = false, KeepsFacing = true, LungeDistance = 5.2f, LungeEndTime = 0.92f, InvulnerableTime = 0.5f, UsesClipMotion = true, ClipTravel = 2.94f };
        [SerializeField] private float _shieldDashCD = 7f;
        [SerializeField] private DodgeBubble _shieldBubblePrefab;
        [SerializeField] private SpellIcon _shieldIcon;
        [Header("Skill R - Execution Leap")]
        [SerializeField] private AxeMove _executionLeap = new AxeMove { Name = "Execution leap", State = "Axe Leap Slam", ClipStart = 0.4f, Speed = 1.7f, HitTime = 1.68f, CancelTime = 1.85f, EndTime = 2.4f, YawToHit = -7f, Damage = 4, Radius = 4.5f, Arc = 360f, CameraPunch = 1.1f, LungeDistance = 11.44f, LungeToMouse = true, LungeMinDistance = 3f, LungeEndTime = 1.68f, StopsAtEnemy = false, UsesClipMotion = true, ClipTravel = 4.8f, ArcHeightFactor = 0.6f, SplashTime = 0.45f };
        [FormerlySerializedAs("_battlecryCD")] [SerializeField] private float _leapCD = 12f;
        [FormerlySerializedAs("_battlecryIcon")] [SerializeField] private SpellIcon _leapIcon;
        [Header("Skill D - Axe Throw")]
        [SerializeField] private AxeMove _axeThrow = new AxeMove { Name = "Axe throw", State = "Axe Spin Low", ClipStart = 0.4f, Speed = 2f, HitTime = 0.94f, CancelTime = 1.15f, EndTime = 1.85f, YawToHit = 187f, TrailStartTime = 0.75f, TrailEndTime = 0.94f };
        [SerializeField] private ThrownAxe _thrownAxePrefab;
        [SerializeField] private SkinnedMeshRenderer _heldAxe;
        [SerializeField] private MeshRenderer _heldAxeLeft;
        [SerializeField] private int _axeThrowDamage = 4;
        [SerializeField] private float _axeThrowRange = 12.6f;
        [SerializeField] private float _axeThrowCurve = 0.5f;
        [SerializeField] private float _axeThrowSpread = 0.3f;
        [SerializeField] private float _axeThrowDuration = 1.26f;
        [FormerlySerializedAs("_shockwaveCD")] [SerializeField] private float _axeThrowCD = 8f;
        [FormerlySerializedAs("_shockwaveIcon")] [SerializeField] private SpellIcon _axeThrowIcon;
        [Header("Dash")]
        [SerializeField] private AxeMove _dashMove = new AxeMove { Name = "Dash (escape jump)", State = "Unarmed Jump Forward", ClipStart = 0.15f, Speed = 1.9f, HitTime = 0f, CancelTime = 1.15f, EndTime = 1.4f, LungeDistance = 6f, LungeEndTime = 1.2f, StopsAtEnemy = false, UsesClipMotion = true, ClipTravel = 3.2f, ArcHeightScale = 0.85f };

        private AxeMove _move;
        private float _clipTime;
        private float _moveSpeed = 1f;
        private bool _isHitDone;
        private bool _isCancelOpen;
        private Action _onHit;
        private Vector3 _aimDirection = Vector3.forward;
        private float _warpTotal;
        private float _warpDone;
        private Vector3 _settleDirection;
        private float _settleLeft;
        private float _lungeTotal;
        private float _lungeDone;
        private float _clipMotionScale;
        private float _clipHeightScale = 1f;
        private float _leanAngle;
        private float _leanTarget;
        private float _leanVelocity;
        private float _hitStopLeft;
        private float _hurtLockLeft;
        private float _lastMoveEndTime;
        private int _chain;
        private RequestKind _request;
        private float _requestLeft;
        private int _basicStage;
        private float _basicStageExpire;
        private bool _isHealing;
        private int _healPressCount;
        private float _healTimeLeft;
        private float _healClipTime;
        private float _healPump;
        private float _healCDLeft;
        private int _rushStage;
        private float _rushWindowLeft;
        private float _rushCDLeft;
        private float _axeThrowCDLeft;
        private bool _isAxeAway;
        private int _axesAway;
        private float _kickCDLeft;
        private float _shieldDashCDLeft;
        private float _leapCDLeft;
        private NavMeshAgent _agent;
        private float _agentBaseOffset;
        private CameraZoomPunch _cameraPunch;
        private readonly Collider[] _hitBuffer = new Collider[48];
        private readonly Collider[] _lungeBuffer = new Collider[8];
        private readonly HashSet<IDamageableByPlayer> _hitTargets = new HashSet<IDamageableByPlayer>();

        private static readonly int Running = Animator.StringToHash("Running");
        private static readonly int Idle_State = Animator.StringToHash("Idle");
        private static readonly int Run_State = Animator.StringToHash("Run");
        public Vector3 CatchPoint => transform.position + Vector3.up * 1.2f;

        private const KeyCode HealKey = KeyCode.Q;
        private const KeyCode RushKey = KeyCode.W;
        private const KeyCode KickKey = CounterKey;
        private const KeyCode ShieldDashKey = KeyCode.E;
        private const KeyCode LeapKey = KeyCode.R;
        private const KeyCode AxeThrowKey = KeyCode.D;
        private const float LandingClearance = 0.9f;
        private const float LandingStep = 0.3f;
        private const float LandingMinDistance = 1f;
        private const float SettleTime = 0.3f;
        private const float LeanInSmooth = 0.05f;
        private const float LeanOutSmooth = 0.2f;
        private const float LeanReleaseShare = 0.75f;
        private const float LeanSnapAngle = 0.4f;
        private const float ShieldDashForwardAngle = 50f;
        private const float ShieldDashBackAngle = 130f;
        private const float SettleTurnSpeed = 900f;
        private const float IdleBlendTime = 0.12f;
        private const float HealChannelBlendTime = 0.15f;
        private const float HealLoopBlendTime = 0.2f;
        private const float EffectLifetime = 3f;

        /// *** Unity Events *** ///

        protected override void Start()
        {
            base.Start();

            _agent = GetComponent<NavMeshAgent>();
            _agentBaseOffset = _agent.baseOffset;
            // The code applies the clips' root motion itself (OnAnimatorMove): the body turns of a move play, the movement only where a move asks for it
            anim.applyRootMotion = true;

            Camera mainCamera = Camera.main;
            if (mainCamera)
            {
                _cameraPunch = mainCamera.GetComponent<CameraZoomPunch>();
                if (!_cameraPunch)
                    _cameraPunch = mainCamera.gameObject.AddComponent<CameraZoomPunch>();

                _skillBar.worldCamera = mainCamera;
            }

            // A camera canvas sizes itself as if its parent had scale 1, so it cannot stay under the scaled character
            _skillBar.transform.SetParent(null, false);
            _healIcon.SetKeyLabel(HealKey);
            _rushIcon.SetKeyLabel(RushKey);
            _kickIcon.SetKeyLabel(KickKey);
            _shieldIcon.SetKeyLabel(ShieldDashKey);
            _leapIcon.SetKeyLabel(LeapKey);
            _axeThrowIcon.SetKeyLabel(AxeThrowKey);
            if (_healEffect)
                _healEffect.SetActive(false);
            StopTrails();
        }

        private void OnAnimatorMove()
        {
            // Locomotion and reactions stay in place (the agent moves the character); only a move plays its clip's turning and, for the leap and the dash, its travel
            if (_move == null)
                return;

            if (!_move.KeepsFacing)
                transform.rotation = anim.deltaRotation * transform.rotation;

            if (_move.UsesClipMotion && _clipTime <= _move.LungeEndTime)
                ApplyClipMotion(anim.deltaPosition);
        }

        private void OnDisable()
        {
            if (_heldAxe)
                _heldAxe.enabled = true;

            if (_heldAxeLeft)
                _heldAxeLeft.enabled = true;
        }

        private void OnDestroy()
        {
            if (_skillBar)
                Destroy(_skillBar.gameObject);
        }

        private void OnDrawGizmosSelected()
        {
            if (_move == null)
                return;

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position + _aimDirection * _move.HitForwardOffset, _move.Radius);
        }

        /// *** Public Methods *** ///

        // Each axe shows up in its own hand when caught; the attacks that need the axes unlock when both are back
        public void OnAxeCaught(bool isLeft)
        {
            if (isLeft)
                _heldAxeLeft.enabled = true;
            else
                _heldAxe.enabled = true;

            _axesAway--;
            if (_axesAway <= 0)
            {
                _axesAway = 0;
                _isAxeAway = false;
            }
        }

        public void PlayImpact(Collider hitCollider, Vector3 from)
        {
            if (!_impactEffect)
                return;

            MeshCollider meshCollider = hitCollider as MeshCollider;
            Vector3 point = meshCollider && !meshCollider.convex ? hitCollider.bounds.ClosestPoint(from) : hitCollider.ClosestPoint(from);
            Vector3 toSource = from - point;
            GameObject impact = Instantiate(_impactEffect, point, toSource.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(toSource) : Quaternion.identity);
            impact.transform.localScale *= _impactEffectScale;
            PlayOnce(impact);
            Destroy(impact, EffectLifetime);
        }

        // Pack hit effects loop, which would repeat the burst until the object is destroyed
        private static void PlayOnce(GameObject effect)
        {
            foreach (ParticleSystem particles in effect.GetComponentsInChildren<ParticleSystem>())
            {
                ParticleSystem.MainModule main = particles.main;
                main.loop = false;
            }
        }

        /// *** Base Methods *** ///

        protected override void PlayerCooldowns()
        {
            base.PlayerCooldowns();

            float deltaTime = Time.deltaTime;
            TickCooldowns(deltaTime);
            TickMove(deltaTime);
            TickSettle(deltaTime);
            TickLean(deltaTime);
        }

        protected override void ResetPlayerState(bool includeCoroutines)
        {
            base.ResetPlayerState(includeCoroutines);

            EndMove(false);
            if (_isHealing)
                EndHealChannel(false, true);

            _request = RequestKind.None;
            _hitStopLeft = 0f;
            anim.speed = 1f;
        }

        protected override void OnDamaged()
        {
            _hurtLockLeft = _hurtLockTime;
            _chain = 0;
            _basicStage = 0;
        }

        protected override void BasicAttackInput()
        {
            // The left click is handled with the skills, since an attack can be remembered and cancelled into
        }

        /// *** Inputs *** ///

        protected override void SkillsInput()
        {
            if (Input.GetKeyDown(DashKey))
                SkillDash();

            // While mashing, the cast key is the only input (besides the dash, which escapes the cast)
            if (_isHealing)
            {
                if (Input.GetKeyDown(HealKey))
                    AddHealPress();

                return;
            }

            if (Input.GetKeyDown(HealKey))
                Request(RequestKind.Heal);
            else if (Input.GetKeyDown(RushKey))
                Request(RequestKind.Rush);
            else if (Input.GetKeyDown(KickKey))
                Request(RequestKind.Kick);
            else if (Input.GetKeyDown(ShieldDashKey))
                Request(RequestKind.ShieldDash);
            else if (Input.GetKeyDown(LeapKey))
                Request(RequestKind.Leap);
            else if (Input.GetKeyDown(AxeThrowKey))
                Request(RequestKind.AxeThrow);
            else if (Input.GetMouseButton(0) && _request == RequestKind.None)
                Request(RequestKind.Basic);

            TryStartRequest();
        }

        private void Request(RequestKind kind)
        {
            _request = kind;
            _requestLeft = _inputBuffer;
        }

        private void TryStartRequest()
        {
            if (_request == RequestKind.None)
                return;

            _requestLeft -= Time.deltaTime;
            if (_requestLeft <= 0f)
            {
                _request = RequestKind.None;
                return;
            }

            // A move that cannot be cancelled yet, or a hurt character, keeps the request waiting in the buffer
            if (isAnimationLocked)
                return;

            bool isCancel = _move != null;
            RequestKind kind = _request;
            _request = RequestKind.None;
            // Without the axe in hand only the heal, the kick, the shield dash and the dash work
            if (_isAxeAway && RequiresAxe(kind))
                return;

            switch (kind)
            {
                case RequestKind.Basic:
                    StartBasic(isCancel);
                    break;
                case RequestKind.Heal:
                    StartHeal();
                    break;
                case RequestKind.Rush:
                    StartRush(isCancel);
                    break;
                case RequestKind.Kick:
                    StartKick(isCancel);
                    break;
                case RequestKind.ShieldDash:
                    StartShieldDash(isCancel);
                    break;
                case RequestKind.Leap:
                    StartLeap(isCancel);
                    break;
                case RequestKind.AxeThrow:
                    StartAxeThrow(isCancel);
                    break;
            }
        }

        /// *** Moves *** ///

        private void BeginMove(AxeMove move, bool isCancel, Action onHit = null)
        {
            // Moves that follow each other quickly make a chain, the first one of a chain starts it
            bool isChained = isCancel || Time.time - _lastMoveEndTime <= _chainGraceTime;
            _chain = isChained ? _chain + 1 : 0;

            _move = move;
            _onHit = onHit;
            _clipTime = move.ClipStart;
            _isHitDone = false;
            _isCancelOpen = false;
            _hitTargets.Clear();
            _settleLeft = 0f;
            isAnimationLocked = true;
            _hurtLockLeft = 0f;
            _agent.isStopped = true;
            anim.SetBool(Running, false);
            // A new move ends any hit stop of the one before
            _hitStopLeft = 0f;
            _moveSpeed = move.Speed;
            anim.speed = _moveSpeed;

            // The agent turns the character to its movement direction and keeps it upright, which would flatten the pitch of a leaning move
            _agent.updateRotation = move.LeanAngle <= 0f && !move.KeepsFacing;
            _agent.updateUpAxis = move.LeanAngle <= 0f;
            // A new move starts upright: AimMove overwrites the rotation, so a lean still easing out is taken off first
            if (!Mathf.Approximately(_leanAngle, 0f))
                transform.Rotate(-_leanAngle, 0f, 0f, Space.Self);

            _leanAngle = 0f;
            _leanTarget = 0f;
            _leanVelocity = 0f;
            AimMove(move);
            StartMovement(move);
            if (move.InvulnerableTime > 0f)
                SetInvulnerable(move.InvulnerableTime);

            // The offset of a cross fade is in real seconds, so it is divided by the animation speed to start at ClipStart seconds of the clip
            anim.CrossFadeInFixedTime(move.StateHash, _crossFadeTime, 0, move.ClipStart / _moveSpeed);
        }

        // The clip turns the body by YawToHit before the strike; the difference to the cursor direction is spread over the wind-up, so the strike lands where the cursor is
        private void AimMove(AxeMove move)
        {
            _aimDirection = transform.forward;
            _warpTotal = 0f;
            _warpDone = 0f;
            if ((move.TurnsToMouse || move.KeepsFacing) && TryGetAimPoint(out Vector3 aimPoint))
            {
                Vector3 toAim = aimPoint - transform.position;
                toAim.y = 0f;
                if (toAim.sqrMagnitude > 0.04f)
                    _aimDirection = toAim.normalized;
            }

            _aimDirection.y = 0f;
            _aimDirection.Normalize();
            if (!move.TurnsToMouse)
                return;

            float aimYaw = Mathf.Atan2(_aimDirection.x, _aimDirection.z) * Mathf.Rad2Deg;
            float warp = Mathf.DeltaAngle(transform.eulerAngles.y + move.YawToHit, aimYaw);
            // A move that strikes at once (the dash) turns to the cursor immediately
            if (move.HitTime <= move.ClipStart + 0.001f)
                transform.rotation = Quaternion.Euler(0f, transform.eulerAngles.y + warp, 0f);
            else
                _warpTotal = warp;
        }

        private void StartMovement(AxeMove move)
        {
            _lungeDone = 0f;
            _lungeTotal = GetLungeDistance(move);
            _clipMotionScale = move.UsesClipMotion && move.ClipTravel > 0.01f ? _lungeTotal / move.ClipTravel : 0f;
            // A longer leap also rises higher, so it stays a jump instead of a glide; a shorter one rises less
            _clipHeightScale = move.ArcHeightScale * (1f + move.ArcHeightFactor * (_clipMotionScale - 1f));
        }

        private float GetLungeDistance(AxeMove move)
        {
            if (move.LungeDistance <= 0f)
                return 0f;

            float distance = move.LungeDistance;
            if (move.LungeToMouse && TryGetAimPoint(out Vector3 aimPoint))
            {
                Vector3 toAim = aimPoint - transform.position;
                toAim.y = 0f;
                distance = Mathf.Clamp(toAim.magnitude, move.LungeMinDistance, move.LungeDistance);
            }

            return move.UsesClipMotion ? ClampLanding(distance) : distance;
        }

        // A jump or a dash that would end inside a boss ends in front of it instead
        private float ClampLanding(float distance)
        {
            while (distance > LandingMinDistance && IsEnemyNear(transform.position + _aimDirection * distance, LandingClearance))
                distance -= LandingStep;

            return Mathf.Max(distance, 0f);
        }

        private bool IsEnemyNear(Vector3 point, float radius)
        {
            int count = Physics.OverlapSphereNonAlloc(point + Vector3.up, radius, _lungeBuffer, ~0, QueryTriggerInteraction.Collide);
            for (int i = 0; i < count; i++)
                if (_lungeBuffer[i].GetComponentInParent<IDamageableByPlayer>() != null)
                    return true;

            return false;
        }

        private bool TryGetAimPoint(out Vector3 aimPoint)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (DebugAimPoint.HasValue)
            {
                aimPoint = DebugAimPoint.Value;
                return true;
            }
#endif
            return GetMouseWorldPoint(out aimPoint);
        }

        private void TickMove(float deltaTime)
        {
            if (_hitStopLeft > 0f)
            {
                _hitStopLeft -= deltaTime;
                if (_hitStopLeft <= 0f)
                    anim.speed = _move != null ? _moveSpeed : 1f;
                return;
            }

            if (_move == null)
                return;

            _clipTime += deltaTime * _moveSpeed;
            ApplyWarp();
            if (_move.LeanAngle > 0f)
                _leanTarget = Mathf.InverseLerp(_move.ClipStart, _move.EndTime, _clipTime) < LeanReleaseShare ? _move.LeanAngle : 0f;
            if (!_move.UsesClipMotion && _lungeTotal > 0f && _clipTime <= _move.LungeEndTime)
                Lunge();

            UpdateTrail();
            if (!_isHitDone && _clipTime >= _move.HitTime)
            {
                _isHitDone = true;
                DoHit(_move);
            }

            if (!_isCancelOpen && _clipTime >= _move.CancelTime)
            {
                _isCancelOpen = true;
                isAnimationLocked = false;
            }

            // Clicking to move during the recovery cuts it, like any other input
            if (_isCancelOpen && anim.GetBool(Running))
            {
                EndMove(true);
                return;
            }

            if (_clipTime >= _move.EndTime)
                EndMove(true);
        }

        private void ApplyWarp()
        {
            if (Mathf.Approximately(_warpTotal, 0f))
                return;

            float progress = Mathf.InverseLerp(_move.ClipStart, _move.HitTime, _clipTime);
            float target = _warpTotal * progress;
            transform.Rotate(0f, target - _warpDone, 0f, Space.World);
            _warpDone = target;
        }

        // The pitch eases toward its target and keeps easing after the move is over, so the body straightens up smoothly instead of snapping back to the stance
        private void TickLean(float deltaTime)
        {
            if (Mathf.Approximately(_leanAngle, 0f) && Mathf.Approximately(_leanTarget, 0f))
                return;

            float next = Mathf.SmoothDamp(_leanAngle, _leanTarget, ref _leanVelocity, _leanTarget > _leanAngle ? LeanInSmooth : LeanOutSmooth, Mathf.Infinity, deltaTime);
            if (Mathf.Approximately(_leanTarget, 0f) && next < LeanSnapAngle)
            {
                next = 0f;
                _leanVelocity = 0f;
            }

            transform.Rotate(next - _leanAngle, 0f, 0f, Space.Self);
            _leanAngle = next;

            // The agent keeps the character upright and turns it to its movement, so it only takes over again once the body stands straight
            if (_move == null && Mathf.Approximately(_leanAngle, 0f))
            {
                _agent.updateRotation = true;
                _agent.updateUpAxis = true;
            }
        }

        private void TickSettle(float deltaTime)
        {
            if (_settleLeft <= 0f || _move != null)
                return;

            _settleLeft -= deltaTime;
            if (anim.GetBool(Running))
            {
                _settleLeft = 0f;
                return;
            }

            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(_settleDirection), SettleTurnSpeed * deltaTime);
        }

        // The lunge slows down as it arrives, so the character stops where the strike lands
        private void Lunge()
        {
            float progress = Mathf.InverseLerp(_move.ClipStart, _move.LungeEndTime, _clipTime);
            float eased = 1f - (1f - progress) * (1f - progress);
            float step = _lungeTotal * eased - _lungeDone;
            if (step <= 0f)
                return;

            if (_move.StopsAtEnemy && IsEnemyAhead())
            {
                _lungeTotal = 0f;
                return;
            }

            _agent.Move(_aimDirection * step);
            _lungeDone += step;
        }

        // The clip's travel is scaled to the wanted distance and applied along the cursor direction; the clip's height becomes the agent's height
        private void ApplyClipMotion(Vector3 clipDelta)
        {
            Vector3 horizontal = new Vector3(clipDelta.x, 0f, clipDelta.z);
            float step = horizontal.magnitude * _clipMotionScale;
            if (step > 0f && !(_move.StopsAtEnemy && IsEnemyAhead()))
                _agent.Move(_aimDirection * step);

            _agent.baseOffset = Mathf.Max(_agentBaseOffset, _agent.baseOffset + clipDelta.y * _clipHeightScale);
        }

        // The movement stops short of a boss instead of running through it
        private bool IsEnemyAhead()
        {
            Vector3 probe = transform.position + _aimDirection * _lungeStopDistance + Vector3.up;
            int count = Physics.OverlapSphereNonAlloc(probe, 0.4f, _lungeBuffer, ~0, QueryTriggerInteraction.Collide);
            for (int i = 0; i < count; i++)
                if (_lungeBuffer[i].GetComponentInParent<IDamageableByPlayer>() != null)
                    return true;

            return false;
        }

        private void UpdateTrail()
        {
            bool isTrailing = _move.TrailEndTime > 0f && _clipTime >= _move.TrailStartTime && _clipTime <= _move.TrailEndTime;
            SetTrail(_bladeTrail, isTrailing && !_move.UsesLeftAxe);
            SetTrail(_bladeTrailLeft, isTrailing && _move.UsesLeftAxe);
        }

        private static void SetTrail(TrailRenderer trail, bool isTrailing)
        {
            if (!trail || isTrailing == trail.emitting)
                return;

            if (isTrailing)
                trail.Clear();

            trail.emitting = isTrailing;
        }

        private void StopTrails()
        {
            SetTrail(_bladeTrail, false);
            SetTrail(_bladeTrailLeft, false);
        }

        private void EndMove(bool playLocomotion)
        {
            if (_move == null)
                return;

            // A move that ends mid-turn (the spins) leaves the character facing the cursor
            // The settle turns to a flat rotation, which would wipe a lean that is still easing out
            if (playLocomotion && _move.TurnsToMouse && Mathf.Approximately(_leanAngle, 0f))
            {
                _settleDirection = _aimDirection;
                _settleLeft = SettleTime;
            }

            // The lean is released here and eases out in TickLean, which gives the agent its rotation back when the body is straight again
            _leanTarget = 0f;
            if (Mathf.Approximately(_leanAngle, 0f))
            {
                _agent.updateRotation = true;
                _agent.updateUpAxis = true;
            }

            _move = null;
            _onHit = null;
            _warpTotal = 0f;
            _lungeTotal = 0f;
            _lastMoveEndTime = Time.time;
            _agent.baseOffset = _agentBaseOffset;
            isAnimationLocked = false;
            StopTrails();

            if (_hitStopLeft <= 0f)
                anim.speed = 1f;

            if (playLocomotion)
                anim.CrossFadeInFixedTime(anim.GetBool(Running) ? Run_State : Idle_State, IdleBlendTime);
        }

        /// *** Strike *** ///

        private void DoHit(AxeMove move)
        {
            // A leap or a dash is on the ground again when it strikes
            if (move.UsesClipMotion)
                _agent.baseOffset = _agentBaseOffset;

            int landed = 0;
            if (move.Damage > 0 || move.CountersBoss)
                landed = StrikeTargets(move);

            if (move.EffectPrefab)
                SpawnEffect(move);

            // The splash is centred where the strike is measured from, so the ring covers exactly what the move can hit
            if (move.SplashTime > 0f)
                GroundSplash.Show(transform.position + _aimDirection * move.HitForwardOffset, move.Radius, move.SplashColor, move.SplashTime);

            if (landed > 0)
            {
                _hitStopLeft = _hitStopTime;
                anim.speed = 0f;
                if (move.CameraPunch > 1f && _cameraPunch)
                    _cameraPunch.Punch(move.CameraPunch, 0.05f, 0.04f, 0.2f);
            }

            if (_onHit != null)
                _onHit();
        }

        private int StrikeTargets(AxeMove move)
        {
            Vector3 center = transform.position + _aimDirection * move.HitForwardOffset;
            int count = Physics.OverlapSphereNonAlloc(center, move.Radius, _hitBuffer, ~0, QueryTriggerInteraction.Collide);
            float multiplier = 1f + _chainDamageBonus * Mathf.Min(_chain, _chainMaxSteps);
            int landed = 0;
            for (int i = 0; i < count; i++)
            {
                Collider hitCollider = _hitBuffer[i];
                if (!IsInsideArc(hitCollider, move.Arc))
                    continue;

                if (move.CountersBoss && hitCollider.CompareTag("Counterable"))
                {
                    ICounterable counterable = hitCollider.GetComponentInParent<ICounterable>();
                    if (counterable != null)
                        counterable.TriggerCounter();
                }

                IDamageableByPlayer target = hitCollider.GetComponentInParent<IDamageableByPlayer>();
                if (target == null || !_hitTargets.Add(target))
                    continue;

                int damage = DamageRoll.Roll(Mathf.Max(1, Mathf.RoundToInt(move.Damage * multiplier)), out bool isCrit);
                target.TakeDamage(damage, isCrit);
                if (!move.NoImpactEffect)
                    PlayImpact(hitCollider, transform.position + Vector3.up * 1.2f);

                landed++;
            }

            return landed;
        }

        private bool IsInsideArc(Collider hitCollider, float arc)
        {
            if (arc >= 359f)
                return true;

            MeshCollider meshCollider = hitCollider as MeshCollider;
            Vector3 closest = meshCollider && !meshCollider.convex ? hitCollider.bounds.ClosestPoint(transform.position) : hitCollider.ClosestPoint(transform.position);
            Vector3 toTarget = closest - transform.position;
            toTarget.y = 0f;
            // Standing inside the target counts as in front of it
            if (toTarget.sqrMagnitude < 0.25f)
                return true;

            return Vector3.Angle(_aimDirection, toTarget) <= arc * 0.5f;
        }

        private void SpawnEffect(AxeMove move)
        {
            Quaternion facing = Quaternion.LookRotation(_aimDirection);
            GameObject effect = Instantiate(move.EffectPrefab, transform.position + facing * move.EffectOffset, facing * Quaternion.Euler(0f, 0f, move.EffectRoll));
            effect.transform.localScale *= move.EffectScale;
            PlayOnce(effect);
            Destroy(effect, EffectLifetime);
        }

        /// *** Basic Attack *** ///

        private void StartBasic(bool isCancel)
        {
            if (Time.time > _basicStageExpire)
                _basicStage = 0;

            AxeMove move = _basicStage == 0 ? _basicHit1 : _basicStage == 1 ? _basicHit2 : _basicHit3;
            _basicStage = (_basicStage + 1) % 3;
            BeginMove(move, isCancel);
            _basicStageExpire = Time.time + move.GetRealTime(move.EndTime, _moveSpeed) + _basicComboResetTime;
        }

        // A skill leaves the next attack as the heavy finisher, so skills weave into the attack chain
        private void PrimeFinisher()
        {
            if (!_skillsPrimeFinisher)
                return;

            _basicStage = 2;
            _basicStageExpire = Time.time + 3f;
        }

        /// *** Skill Q: Second Wind *** ///

        // The Axeman braces and has to mash the cast key to finish the cast; the heart comes back when he stands up. Taking a hit, running out of time or dashing away cancels it, and the cooldown starts either way
        private void StartHeal()
        {
            if (_healCDLeft > 0f)
                return;

            EndMove(false);
            _isHealing = true;
            _healPressCount = 0;
            _healTimeLeft = _healTimeLimit;
            _healClipTime = 0f;
            _healPump = 0f;
            isAnimationLocked = true;
            _agent.isStopped = true;
            anim.SetBool(Running, false);
            anim.speed = 1f;
            anim.CrossFadeInFixedTime(Animator.StringToHash(_healChannelState), HealChannelBlendTime);
            _smashPrompt.Show(HealKey, _healPresses);
        }

        private void AddHealPress()
        {
            _healPressCount++;
            _smashPrompt.SetProgress(_healPressCount, _healPresses);
            if (_healPressCount >= _healPresses)
                CompleteHeal();
            else
                _healPump = _healPumpBoost;
        }

        // Plays the wind-up once, then keeps the roar hold looping; the more presses, the faster it pulses
        private void TickHealChannel(float deltaTime)
        {
            _healPump = Mathf.Max(0f, _healPump - _healPumpDecay * deltaTime);
            float power = (float)_healPressCount / _healPresses;
            float speed = Mathf.Lerp(_healChannelSpeed, _healMaxChannelSpeed, power) + _healPump;
            anim.speed = speed;
            _healClipTime += speed * deltaTime;
            if (_healClipTime < _healLoopEnd)
                return;

            _healClipTime = _healLoopStart;
            // The offset of a cross fade is in real seconds, so it is divided by the animation speed
            anim.CrossFadeInFixedTime(Animator.StringToHash(_healChannelState), HealLoopBlendTime, 0, _healLoopStart / speed);
        }

        private void CompleteHeal()
        {
            _isHealing = false;
            anim.speed = 1f;
            _smashPrompt.Complete();
            StartHealCooldown();
            BeginMove(_healRise, false, ApplyHeal);
        }

        private void ApplyHeal()
        {
            SetHealth(_healAmount);
            if (_healEffect)
            {
                _healEffect.SetActive(false);
                _healEffect.SetActive(true);
            }
        }

        private void EndHealChannel(bool playIdle, bool hasFailed)
        {
            _isHealing = false;
            anim.speed = 1f;
            if (hasFailed)
                _smashPrompt.Fail();
            else
                _smashPrompt.Hide();

            StartHealCooldown();
            isAnimationLocked = false;
            if (playIdle)
                anim.CrossFadeInFixedTime(Idle_State, IdleBlendTime);
        }

        private void StartHealCooldown()
        {
            _healCDLeft = _healCD;
            _healIcon.StartCooldown(_healCD);
        }

        /// *** Skill W: Rushing Cleave *** ///

        // Three casts, each a lunge and a swing; the third is a wide spin. Waiting too long between casts ends the chain, and the cooldown starts when it ends
        private void StartRush(bool isCancel)
        {
            if (_rushStage == 0 && _rushCDLeft > 0f)
                return;

            AxeMove move = _rushStage == 0 ? _rushCast1 : _rushStage == 1 ? _rushCast2 : _rushCast3;
            BeginMove(move, isCancel);
            PrimeFinisher();
            _rushStage++;
            _rushWindowLeft = _rushWindow;
            if (_rushStage >= 3)
                FinishRush();
            else
                _rushIcon.SetHighlighted(true);
        }

        private void FinishRush()
        {
            _rushStage = 0;
            _rushCDLeft = _rushCD;
            _rushIcon.SetHighlighted(false);
            _rushIcon.StartCooldown(_rushCD);
        }

        // The other skills keep the Rushing Cleave chain alive
        private void RefreshRushWindow()
        {
            if (_rushStage > 0)
                _rushWindowLeft = _rushWindow;
        }

        /// *** Skill F: Hammer Kick *** ///

        private void StartKick(bool isCancel)
        {
            if (_kickCDLeft > 0f)
                return;

            _kickCDLeft = _kickCD;
            _kickIcon.StartCooldown(_kickCD);
            BeginMove(_kick, isCancel);
            PrimeFinisher();
            RefreshRushWindow();
        }

        /// *** Skill E: Shield Dash *** ///

        // A short shoulder charge, like a rugby tackle (a run clip with the body pitched forward), that covers the Axeman with a shield for half a second (he cannot be hurt while it lasts); it stops in front of an enemy and can be cancelled early into an attack or another skill
        private void StartShieldDash(bool isCancel)
        {
            if (_shieldDashCDLeft > 0f)
                return;

            _shieldDashCDLeft = _shieldDashCD;
            _shieldIcon.StartCooldown(_shieldDashCD);
            AxeMove dodge = ChooseShieldDash();
            BeginMove(dodge, isCancel);
            if (_shieldBubblePrefab)
                Instantiate(_shieldBubblePrefab, transform).Begin(dodge.InvulnerableTime);

            PrimeFinisher();
            RefreshRushWindow();
        }

        // The dodge follows where the cursor is relative to the way he faces: ahead turns to it and runs in, to the sides and behind he keeps his facing and slides toward it
        private AxeMove ChooseShieldDash()
        {
            if (!TryGetAimPoint(out Vector3 aimPoint))
                return _shieldDash;

            Vector3 toAim = aimPoint - transform.position;
            toAim.y = 0f;
            if (toAim.sqrMagnitude < 0.04f)
                return _shieldDash;

            Vector3 forward = transform.forward;
            forward.y = 0f;
            float angle = Vector3.SignedAngle(forward, toAim, Vector3.up);
            if (Mathf.Abs(angle) <= ShieldDashForwardAngle)
                return _shieldDash;

            if (Mathf.Abs(angle) >= ShieldDashBackAngle)
                return _shieldDashBack;

            return angle > 0f ? _shieldDashRight : _shieldDashLeft;
        }

        /// *** Skill R: Execution Leap *** ///

        // A leap onto the cursor with a huge slam; the cooldown starts when the leap does
        private void StartLeap(bool isCancel)
        {
            if (_leapCDLeft > 0f)
                return;

            _leapCDLeft = _leapCD;
            _leapIcon.StartCooldown(_leapCD);
            BeginMove(_executionLeap, isCancel);
            RefreshRushWindow();
        }

        /// *** Skill D: Axe Throw *** ///

        // Both axes leave the hands at the strike of the spin, fly a fixed distance toward the cursor on arcs bowing to opposite sides and come back along the same arcs (ThrownAxe); until both are caught every attack that needs the axes is locked
        private void StartAxeThrow(bool isCancel)
        {
            if (_axeThrowCDLeft > 0f || _isAxeAway)
                return;

            _axeThrowCDLeft = _axeThrowCD;
            _axeThrowIcon.StartCooldown(_axeThrowCD);
            BeginMove(_axeThrow, isCancel, ReleaseAxe);
        }

        private void ReleaseAxe()
        {
            _isAxeAway = true;
            _axesAway = 2;
            _heldAxe.enabled = false;
            _heldAxeLeft.enabled = false;
            LaunchAxe(HumanBodyBones.RightHand, false);
            LaunchAxe(HumanBodyBones.LeftHand, true);
        }

        private void LaunchAxe(HumanBodyBones handBone, bool isLeft)
        {
            Vector3 hand = anim.GetBoneTransform(handBone).position;
            ThrownAxe thrown = Instantiate(_thrownAxePrefab, hand, Quaternion.identity);
            thrown.Launch(this, isLeft, hand, _aimDirection, _axeThrowRange, _axeThrowCurve, _axeThrowSpread, _axeThrowDuration, _axeThrowDamage);
        }

        private bool RequiresAxe(RequestKind kind) => kind == RequestKind.Basic || kind == RequestKind.Rush || kind == RequestKind.AxeThrow || kind == RequestKind.Leap;

        /// *** Debug *** ///

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        // For the CLI and tests, which cannot press keys or move the mouse: while set, every aim uses this point instead of the mouse
        public Vector3? DebugAimPoint { get; set; }

        public void DebugPress(string input)
        {
            switch (input)
            {
                case "Basic":
                    Request(RequestKind.Basic);
                    break;
                case "Q":
                    Request(RequestKind.Heal);
                    break;
                case "W":
                    Request(RequestKind.Rush);
                    break;
                case "F":
                    Request(RequestKind.Kick);
                    break;
                case "R":
                    Request(RequestKind.Leap);
                    break;
                case "E":
                    Request(RequestKind.ShieldDash);
                    break;
                case "D":
                    Request(RequestKind.AxeThrow);
                    break;
                case "Dash":
                    SkillDash();
                    break;
                case "Smash":
                    if (_isHealing)
                        AddHealPress();
                    break;
            }
        }
#endif

        /// *** Dash *** ///

        protected override void SkillDash()
        {
            if (!IsDashReady || IsDead)
                return;

            // Dashing away escapes a cast in progress
            if (_isHealing)
                EndHealChannel(false, false);

            StartDashCooldown();
            _request = RequestKind.None;
            if (DashSmokePrefab)
                Instantiate(DashSmokePrefab, transform.position + Vector3.up * 0.9f, Quaternion.identity);

            BeginMove(_dashMove, true);
        }

        /// *** Timers *** ///

        private void TickCooldowns(float deltaTime)
        {
            if (_healCDLeft > 0f)
                _healCDLeft -= deltaTime;
            if (_rushCDLeft > 0f)
                _rushCDLeft -= deltaTime;
            if (_axeThrowCDLeft > 0f)
                _axeThrowCDLeft -= deltaTime;
            if (_kickCDLeft > 0f)
                _kickCDLeft -= deltaTime;
            if (_shieldDashCDLeft > 0f)
                _shieldDashCDLeft -= deltaTime;
            if (_leapCDLeft > 0f)
                _leapCDLeft -= deltaTime;

            if (_isHealing)
            {
                _healTimeLeft -= deltaTime;
                _smashPrompt.SetTimeLeft(_healTimeLeft / _healTimeLimit);
                TickHealChannel(deltaTime);

                if (_healTimeLeft <= 0f)
                    EndHealChannel(true, true);
            }

            if (_rushStage > 0)
            {
                _rushWindowLeft -= deltaTime;
                if (_rushWindowLeft <= 0f)
                    FinishRush();
            }

            if (_hurtLockLeft > 0f)
            {
                _hurtLockLeft -= deltaTime;
                if (_hurtLockLeft <= 0f)
                    DamageAnimStopped();
            }

            // A chain ends when the Axeman stops fighting for a moment
            if (_chain > 0 && _move == null && Time.time - _lastMoveEndTime > _chainGraceTime)
            {
                _chain = 0;
                }
        }

        /// *** Nested Types *** ///

        private enum RequestKind
        {
            None,
            Basic,
            Heal,
            Rush,
            Kick,
            ShieldDash,
            Leap,
            AxeThrow
        }
    }
}
