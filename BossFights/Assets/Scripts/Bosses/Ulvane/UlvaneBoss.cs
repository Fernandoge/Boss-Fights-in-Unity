using System;
using System.Collections;
using Shared;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Bosses.Ulvane
{
    // A longbow hunter who keeps his distance and calls animals; every animal has exactly one skill
    public class UlvaneBoss : BossController
    {
        [Serializable]
        private class BeastEntry
        {
            public UlvaneAttack attack;
            public UlvaneBeast prefab;
        }

        [Header("Kiting")]
        [SerializeField] private float _walkSpeed = 3.2f;
        [SerializeField] private float _minPlayerDistance = 8f;
        [SerializeField] private float _maxPlayerDistance = 13f;
        [SerializeField, Range(0.1f, 1f)] private float _strafeSpeedScale = 0.6f;
        [SerializeField] private Vector2 _strafeSwitchTime = new Vector2(2f, 4f);
        [SerializeField] private float _blockedStrafeTime = 1f;
        [SerializeField] private float _moveBlendTime = 0.1f;

        [Header("Shared")]
        [SerializeField] private SkillIndicator _indicatorPrefab;
        [SerializeField] private GameObject _impactEffectPrefab;

        [Header("Arrow Shot")]
        [SerializeField] private UlvaneProjectile _arrowPrefab;
        [SerializeField] private float _drawTime = 1f;
        [SerializeField] private float _aimTime = 1.1f;
        [SerializeField] private float _releaseDelay = 0.12f;
        [SerializeField] private float _afterShotTime = 0.5f;
        [SerializeField] private float _arrowSpeed = 30f;
        [SerializeField] private float _arrowLaneWidth = 0.9f;
        [SerializeField] private float _arrowMaxRange = 45f;
        [SerializeField] private float _arrowHeight = 1.3f;
        [SerializeField] private float _arrowLeadTime = 0.3f;
        [SerializeField] private int _arrowDamage = 2;

        [Header("Beasts")]
        [SerializeField] private BeastEntry[] _beasts;
        [SerializeField] private float _commandDelay = 0.6f;
        [SerializeField] private float _beastSideOffset = 2f;
        [SerializeField] private float _afterBeastTime = 0.6f;
        [SerializeField] private float _beastTimeout = 20f;

        [Header("Phase 2")]
        [SerializeField] private float _roarTime = 2.2f;
        [SerializeField] private float _phaseTwoTimeBetweenAttacks = 3f;
        [SerializeField] private int _phaseTwoArrowCount = 3;
        [SerializeField] private float _phaseTwoArrowSpread = 14f;

        private UlvaneAttack? _lastAttack;
        private float _strafeSign = 1f;
        private float _strafeTimer;
        private float _blockedTimer;

        private static readonly int Draw = Animator.StringToHash("Draw");
        private static readonly int Release = Animator.StringToHash("Release");
        private static readonly int Command = Animator.StringToHash("Command");
        private static readonly int Move_X = Animator.StringToHash("MoveX");
        private static readonly int Move_Z = Animator.StringToHash("MoveZ");
        private static readonly int Roar = Animator.StringToHash("EnterSecondPhase");

        public UlvaneAttack? DebugOnlyAttack { get; set; }

        public SkillIndicator IndicatorPrefab => _indicatorPrefab;

        public GameObject ImpactEffectPrefab => _impactEffectPrefab;

        // Where the animals aim: the player as the boss last saw them, never the live position
        public Vector3 PlayerAimPoint => PerceivedPlayerPosition;

        public Vector3 PlayerAimVelocity => PerceivedPlayerVelocity;

        // Beside the boss, on a random side
        public Vector3 BeastSpawnPoint => transform.position + transform.right * (_beastSideOffset * (Random.value < 0.5f ? -1f : 1f)) + transform.forward * 0.8f;

        private int ArrowCount => hasEnteredSecondPhase ? _phaseTwoArrowCount : 1;

        private Vector3 ArrowOrigin => transform.position + transform.forward * 0.8f + Vector3.up * _arrowHeight;

        protected override void Start()
        {
            base.Start();
            navMeshAgent.updateRotation = false;
            navMeshAgent.speed = _walkSpeed;
            anim.applyRootMotion = false;
            _strafeSign = Random.value < 0.5f ? -1f : 1f;
            _strafeTimer = Random.Range(_strafeSwitchTime.x, _strafeSwitchTime.y);
        }

        /// *** Debug *** ///

        public void DebugForceAttack(UlvaneAttack attack)
        {
            if (DebugIsBusy)
                return;

            base.PerformAttack();
            StartAttack(attack);
        }

        /// *** Boss Overrides *** ///

        // Keeps a distance from the player: backs away when they are close, walks closer when they are far, and circles around in between
        protected override void IdleMovement()
        {
            Vector3 toPlayer = player.position - transform.position;
            toPlayer.y = 0f;
            float distance = toPlayer.magnitude;
            Vector3 forward = distance > 0.01f ? toPlayer / distance : transform.forward;
            FacePlayer(Time.deltaTime);

            _strafeTimer -= Time.deltaTime;
            if (_strafeTimer <= 0f)
            {
                _strafeSign = -_strafeSign;
                _strafeTimer = Random.Range(_strafeSwitchTime.x, _strafeSwitchTime.y);
            }

            _blockedTimer -= Time.deltaTime;
            Vector3 strafe = Vector3.Cross(Vector3.up, forward) * _strafeSign;
            Vector3 move;
            if (_blockedTimer > 0f || (distance >= _minPlayerDistance && distance <= _maxPlayerDistance))
                move = strafe * _strafeSpeedScale;
            else
                move = distance < _minPlayerDistance ? -forward : forward;

            Vector3 before = transform.position;
            Vector3 delta = move * (_walkSpeed * Time.deltaTime);
            navMeshAgent.Move(delta);

            // Cornered against trees while backing away: slide sideways for a moment instead
            bool isBlocked = delta.sqrMagnitude > 0f && (transform.position - before).sqrMagnitude < delta.sqrMagnitude * 0.09f;
            if (isBlocked && _blockedTimer <= 0f && distance < _minPlayerDistance)
            {
                _blockedTimer = _blockedStrafeTime;
                _strafeSign = -_strafeSign;
            }

            Vector3 localMove = transform.InverseTransformDirection(move);
            anim.SetBool(Walking, move.sqrMagnitude > 0.0001f);
            anim.SetFloat(Move_X, localMove.x, _moveBlendTime, Time.deltaTime);
            anim.SetFloat(Move_Z, localMove.z, _moveBlendTime, Time.deltaTime);
        }

        protected override void PerformAttack()
        {
            base.PerformAttack();
            StartAttack(ChooseAttack());
        }

        protected override void OnDefeated()
        {
            foreach (UlvaneBeast beast in FindObjectsByType<UlvaneBeast>(FindObjectsSortMode.None))
                Destroy(beast.gameObject);

            foreach (GroundBlast blast in FindObjectsByType<GroundBlast>(FindObjectsSortMode.None))
                Destroy(blast.gameObject);

            foreach (UlvaneProjectile projectile in FindObjectsByType<UlvaneProjectile>(FindObjectsSortMode.None))
                Destroy(projectile.gameObject);

            foreach (SkillIndicator indicator in FindObjectsByType<SkillIndicator>(FindObjectsSortMode.None))
                Destroy(indicator.gameObject);
        }

        // Placeholder phase 2: a roar, then faster attacks and a volley of three arrows
        protected override IEnumerator EnterSecondPhase()
        {
            if (hasEnteredSecondPhase)
                yield break;

            hasEnteredSecondPhase = true;
            yield return new WaitUntil(() => !isPerformingAttack && !isPerformingAction);
            base.PerformAttack();
            StandStill();
            auxTimeBetweenAttacks = _phaseTwoTimeBetweenAttacks;
            timeBetweenAttacks = auxTimeBetweenAttacks;
            anim.SetTrigger(Roar);
            yield return WaitFacingPlayer(_roarTime);
            FinishAttack();
            EndSecondPhaseTransition();
        }

        /// *** Attack Selection *** ///

        // A random attack, but never the same one twice in a row
        private UlvaneAttack ChooseAttack()
        {
            if (DebugOnlyAttack.HasValue)
                return DebugOnlyAttack.Value;

            UlvaneAttack[] attacks = (UlvaneAttack[])Enum.GetValues(typeof(UlvaneAttack));
            UlvaneAttack attack;
            do
                attack = attacks[Random.Range(0, attacks.Length)];
            while (attacks.Length > 1 && _lastAttack.HasValue && attack == _lastAttack.Value);

            return attack;
        }

        private void StartAttack(UlvaneAttack attack)
        {
            _lastAttack = attack;
            StandStill();
            if (attack == UlvaneAttack.ArrowShot)
                StartCoroutine(ArrowShotSequence());
            else
                StartCoroutine(BeastSequence(attack));
        }

        private void FinishAttack() => isPerformingAttack = false;

        // The walk animation would keep playing on the spot while the boss waits for a skill to end
        private void StandStill()
        {
            anim.SetBool(Walking, false);
            anim.SetFloat(Move_X, 0f);
            anim.SetFloat(Move_Z, 0f);
        }

        /// *** Arrow Shot *** ///

        // Draws the bow, locks the aim with a lane on the floor, then looses the arrow down the lane
        private IEnumerator ArrowShotSequence()
        {
            ShowDangerIcon(_arrowDamage);
            anim.SetTrigger(Draw);
            yield return WaitFacingPlayer(_drawTime);

            Vector3 aimPoint = PerceivedPlayerPosition + PerceivedPlayerVelocity * _arrowLeadTime;
            Vector3 baseDirection = aimPoint - transform.position;
            baseDirection.y = 0f;
            baseDirection = baseDirection.sqrMagnitude > 0.0001f ? baseDirection.normalized : transform.forward;

            int count = ArrowCount;
            Vector3[] directions = new Vector3[count];
            float[] lengths = new float[count];
            for (int i = 0; i < count; i++)
            {
                float angle = count > 1 ? Mathf.Lerp(-_phaseTwoArrowSpread, _phaseTwoArrowSpread, i / (float)(count - 1)) : 0f;
                directions[i] = Quaternion.Euler(0f, angle, 0f) * baseDirection;
                lengths[i] = NavMeshUtility.DistanceToEdge(transform.position, directions[i], _arrowMaxRange);
                Instantiate(_indicatorPrefab).ShowLine(transform.position, directions[i], lengths[i], _arrowLaneWidth, _aimTime);
            }

            yield return WaitFacingDirection(baseDirection, _aimTime);
            anim.SetTrigger(Release);
            yield return new WaitForSeconds(_releaseDelay);

            for (int i = 0; i < count; i++)
            {
                UlvaneProjectile arrow = Instantiate(_arrowPrefab, ArrowOrigin, Quaternion.identity);
                arrow.Launch(directions[i], _arrowSpeed, lengths[i] / _arrowSpeed + 0.2f, _arrowDamage);
            }

            yield return WaitFacingPlayer(_afterShotTime);
            FinishAttack();
        }

        /// *** Beasts *** ///

        // Ulvane gestures, the animal appears beside him and does its skill, and he waits until it has left
        private IEnumerator BeastSequence(UlvaneAttack attack)
        {
            UlvaneBeast prefab = FindBeastPrefab(attack);
            if (!prefab)
            {
                FinishAttack();
                yield break;
            }

            ShowDangerIcon(prefab.HitDamage);
            anim.SetTrigger(Command);
            yield return WaitFacingPlayer(_commandDelay);

            UlvaneBeast beast = Instantiate(prefab);
            beast.Summon(this);
            for (float elapsed = 0f; beast && !beast.IsFinished && elapsed < _beastTimeout; elapsed += Time.deltaTime)
            {
                FacePlayer(Time.deltaTime);
                yield return null;
            }

            yield return WaitFacingPlayer(_afterBeastTime);
            FinishAttack();
        }

        private UlvaneBeast FindBeastPrefab(UlvaneAttack attack)
        {
            foreach (BeastEntry entry in _beasts)
                if (entry.attack == attack)
                    return entry.prefab;

            return null;
        }

        // A walkable point about this far from the player, in a random direction, for animals that charge in from the edge of the fight
        public Vector3 GetFlankPoint(float distance)
        {
            Vector3 point = PerceivedPlayerPosition;
            for (int i = 0; i < 12; i++)
            {
                Vector2 circle = Random.insideUnitCircle.normalized;
                point = NavMeshUtility.Snap(PerceivedPlayerPosition + new Vector3(circle.x, 0f, circle.y) * distance, 8f);
                Vector3 offset = point - PerceivedPlayerPosition;
                offset.y = 0f;
                if (offset.magnitude >= distance * 0.7f)
                    return point;
            }

            return point;
        }

        public void SpawnPuff(Vector3 position)
        {
            if (_impactEffectPrefab)
                Destroy(Instantiate(_impactEffectPrefab, position, Quaternion.identity), 3f);
        }

        /// *** Facing *** ///

        private IEnumerator WaitFacingPlayer(float duration)
        {
            for (float elapsed = 0f; elapsed < duration; elapsed += Time.deltaTime)
            {
                FacePlayer(Time.deltaTime);
                yield return null;
            }
        }

        private IEnumerator WaitFacingDirection(Vector3 direction, float duration)
        {
            for (float elapsed = 0f; elapsed < duration; elapsed += Time.deltaTime)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), Mathf.Clamp01(Time.deltaTime * rotationSpeed));
                yield return null;
            }
        }

        private void FacePlayer(float deltaTime)
        {
            Vector3 direction = PerceivedPlayerPosition - transform.position;
            direction.y = 0f;
            if (direction == Vector3.zero)
                return;

            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), Mathf.Clamp01(deltaTime * rotationSpeed));
        }
    }
}
