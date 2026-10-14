using System;
using System.Collections;
using System.Collections.Generic;
using Manager.GameManager;
using Shared;
using UI;
using UnityEngine;
using UnityEngine.AI;
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
        [SerializeField] private float _arrowHeight = 2.6f;
        [SerializeField] private float _arrowForwardOffset = 1.6f;
        [SerializeField] private float _arrowLeadTime = 0.3f;
        [SerializeField] private int _arrowDamage = 1;

        [Header("Beasts")]
        [SerializeField] private BeastEntry[] _beasts;
        [SerializeField] private float _commandDelay = 0.6f;
        [SerializeField] private float _minSpawnDistanceFromPlayer = 10f;
        [SerializeField] private Vector3 _arenaCenter = Vector3.zero;
        [SerializeField] private float _centerPatrolRadius = 14f;
        [SerializeField] private float _outerPatrolMinRadius = 18f;
        [SerializeField] private float _outerPatrolStep = 14f;
        [SerializeField] private float _afterBeastTime = 0.6f;
        [SerializeField] private float _beastTimeout = 20f;

        [Header("Beast Limits")]
        [SerializeField] private int _maxBeastsAtOnce = 2;
        [SerializeField] private int _phaseTwoMaxBeastsAtOnce = 2;
        [SerializeField] private float _firstBeastDelay = 2f;
        [SerializeField] private float _timeBetweenBeastStarts = 1.5f;
        [SerializeField] private float _lastStandTimeBetweenBeastStarts = 0.9f;

        [Header("Snow Tornado Combo")]
        [SerializeField] private float _tornadoPartnerDistance = 5f;
        [SerializeField] private float _tornadoFeedInterval = 0.6f;
        [SerializeField] private float _tornadoTimeout = 25f;
        [SerializeField] private float _tornadoScreamTime = 2f;
        [SerializeField] private float _tornadoFirstDelay = 5f;
        [SerializeField] private Vector2 _tornadoCooldown = new Vector2(16f, 24f);

        [Header("Phase 2")]
        [SerializeField] private float _roarTime = 2.2f;
        [SerializeField] private float _phaseTwoTimeBetweenAttacks = 3f;
        [SerializeField] private int _phaseTwoArrowCount = 3;
        [SerializeField] private float _phaseTwoArrowSpread = 14f;

        [Header("Phase 3")]
        [SerializeField] private float _phaseThreeDuration = 20f;
        [SerializeField] private float _mournRadius = 4.5f;
        [SerializeField] private float _mournArriveTimeout = 25f;
        [SerializeField] private float _mournTime = 3f;

        private bool _isRoaring;
        private readonly List<UlvaneAttack> _turnOrder = new List<UlvaneAttack>();
        private UlvaneAttack? _lastBeastAttack;
        private bool _isComboRunning;
        private float _nextTornadoTime = float.MaxValue;
        private bool _isLastStand;
        private bool _isMourning;
        private float _strafeSign = 1f;
        private float _strafeTimer;
        private float _blockedTimer;
        private NavMeshTriangulation _navMesh;
        private float[] _cumulativeTriangleAreas;
        private UlvanePatrolZone[] _patrolZones;
        private readonly Dictionary<UlvaneAttack, UlvaneBeast> _activeBeasts = new Dictionary<UlvaneAttack, UlvaneBeast>();

        private static readonly int Draw = Animator.StringToHash("Draw");
        private static readonly int Release = Animator.StringToHash("Release");
        private static readonly int Command = Animator.StringToHash("Command");
        private static readonly int Move_X = Animator.StringToHash("MoveX");
        private static readonly int Move_Z = Animator.StringToHash("MoveZ");
        private static readonly int Roar = Animator.StringToHash("EnterSecondPhase");
        private static readonly int Fall = Animator.StringToHash("Die");

        public UlvaneAttack? DebugOnlyAttack { get; set; }

        public SkillIndicator IndicatorPrefab => _indicatorPrefab;

        public GameObject ImpactEffectPrefab => _impactEffectPrefab;

        // Where the animals aim: the player as the boss last saw them, never the live position
        public Vector3 PlayerAimPoint => PerceivedPlayerPosition;

        public Vector3 PlayerAimVelocity => PerceivedPlayerVelocity;

        // The animals hold back while the boss is dead, roaring into phase 2, or the player is dead (or the harness switched auto attacks off)
        public bool CanBeastsAct => !IsDead && !_isRoaring && !_isMourning && !DebugAutoAttacksDisabled && !GameManager.Instance.player.IsDead;

        private int MaxBeastsAtOnce => _isLastStand ? int.MaxValue : hasEnteredSecondPhase ? _phaseTwoMaxBeastsAtOnce : _maxBeastsAtOnce;

        private int ArrowCount => hasEnteredSecondPhase ? _phaseTwoArrowCount : 1;

        private Vector3 ArrowOrigin => transform.position + transform.forward * _arrowForwardOffset + Vector3.up * _arrowHeight;

        protected override void Start()
        {
            base.Start();
            navMeshAgent.updateRotation = false;
            navMeshAgent.speed = _walkSpeed;
            anim.applyRootMotion = false;
            _strafeSign = Random.value < 0.5f ? -1f : 1f;
            _strafeTimer = Random.Range(_strafeSwitchTime.x, _strafeSwitchTime.y);
            SpawnBeasts();
            StartCoroutine(BeastTurns(_firstBeastDelay));
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

        // He falls instead of dying: phase 3 starts, see LastStandSequence
        protected override bool OnHealthDepleted()
        {
            if (_isLastStand)
                return false;

            _isLastStand = true;
            isImmuneToDamage = true;
            StopAllCoroutines();
            StartCoroutine(LastStandSequence());
            return true;
        }

        protected override void OnDefeated()
        {
            // The animals that mourned him stay on the field under the victory screen
            if (!_isLastStand)
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
            _isRoaring = true;
            yield return new WaitUntil(() => !isPerformingAttack && !isPerformingAction);
            base.PerformAttack();
            StandStill();
            _nextTornadoTime = Time.time + _tornadoFirstDelay;
            auxTimeBetweenAttacks = _phaseTwoTimeBetweenAttacks;
            timeBetweenAttacks = auxTimeBetweenAttacks;
            anim.SetTrigger(Roar);
            yield return WaitFacingPlayer(_roarTime);
            FinishAttack();
            _isRoaring = false;
            EndSecondPhaseTransition();
        }

        /// *** Phase 3 *** ///

        // Ulvane falls and cannot be hurt, the animals go enraged and the player has to survive for a while; then the animals run to his body, circle it and howl, and only then does the fight end
        private IEnumerator LastStandSequence()
        {
            // The attack loop stops for good: the boss is neither attacking nor walking, and no new attack can start
            isPerformingAttack = true;
            timeBetweenAttacks = float.MaxValue;
            navMeshAgent.isStopped = true;
            HideDangerIcon();
            StandStill();
            anim.SetTrigger(Fall);

            UlvaneBeast[] beasts = FindObjectsByType<UlvaneBeast>(FindObjectsSortMode.None);
            foreach (UlvaneBeast beast in beasts)
                beast.Enrage();

            StartCoroutine(BeastTurns(0.5f));

            SurviveBanner banner = SurviveBanner.Show();
            for (float elapsed = 0f; elapsed < _phaseThreeDuration; elapsed += Time.deltaTime)
            {
                if (GameManager.Instance.player.IsDead)
                {
                    banner.Hide();
                    yield break;
                }

                yield return null;
            }

            banner.Hide();

            _isMourning = true;
            Vector3 center = transform.position;
            float startAngle = Random.Range(0f, 360f);
            for (int i = 0; i < beasts.Length; i++)
                beasts[i].Mourn(center, _mournRadius, startAngle + 360f * i / beasts.Length);

            for (float elapsed = 0f; elapsed < _mournArriveTimeout && !AllArrivedAtMourning(beasts); elapsed += Time.deltaTime)
                yield return null;

            yield return new WaitForSeconds(_mournTime);
            Defeat(false);
        }

        private bool AllArrivedAtMourning(UlvaneBeast[] beasts)
        {
            foreach (UlvaneBeast beast in beasts)
                if (beast && !beast.HasArrivedAtMourning)
                    return false;

            return true;
        }

        /// *** Attack Selection *** ///

        // Ulvane only shoots on his own for now: the animals act by themselves, and his special calls will go here
        // Phase 2 only: now and then he screams for a combined command instead of shooting
        private UlvaneAttack ChooseAttack() => DebugOnlyAttack ?? (CanStartTornado() ? UlvaneAttack.SnowTornado : UlvaneAttack.ArrowShot);

        private bool CanStartTornado() => hasEnteredSecondPhase && !_isLastStand && Time.time >= _nextTornadoTime && TryGetTornadoPartners(out DogBeast _, out PenguinBeast _);

        private bool TryGetTornadoPartners(out DogBeast dog, out PenguinBeast penguin)
        {
            _activeBeasts.TryGetValue(UlvaneAttack.DogWhirlwind, out UlvaneBeast dogBeast);
            _activeBeasts.TryGetValue(UlvaneAttack.PenguinSnowballs, out UlvaneBeast penguinBeast);
            dog = dogBeast as DogBeast;
            penguin = penguinBeast as PenguinBeast;
            return !_isComboRunning && dog && penguin && dog.IsAvailable && penguin.IsAvailable;
        }

        private void StartAttack(UlvaneAttack attack)
        {
            StandStill();
            if (attack == UlvaneAttack.ArrowShot)
                StartCoroutine(ArrowShotSequence());
            else if (attack == UlvaneAttack.SnowTornado)
                StartCoroutine(SnowTornadoSequence());
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

            // The boss stands slightly lower than the floor decoration, so the lanes start at the player's height to draw above the dirt patches
            Vector3 laneOrigin = new Vector3(transform.position.x, GameManager.Instance.player.transform.position.y, transform.position.z);
            int count = ArrowCount;
            Vector3[] directions = new Vector3[count];
            float[] lengths = new float[count];
            for (int i = 0; i < count; i++)
            {
                float angle = count > 1 ? Mathf.Lerp(-_phaseTwoArrowSpread, _phaseTwoArrowSpread, i / (float)(count - 1)) : 0f;
                directions[i] = Quaternion.Euler(0f, angle, 0f) * baseDirection;
                lengths[i] = NavMeshUtility.DistanceToEdge(transform.position, directions[i], _arrowMaxRange);
                Instantiate(_indicatorPrefab).ShowLine(laneOrigin, directions[i], lengths[i], _arrowLaneWidth, _aimTime);
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

        // The animals take turns: every time fewer than the allowed number are working and enough time passed since the last one started, the next animal in a shuffled order is sent. Each animal gets one turn before any gets a second. In the last stand all of them may work together, one new start every fraction of a second.
        private IEnumerator BeastTurns(float firstDelay)
        {
            yield return new WaitForSeconds(firstDelay);
            float nextStartTime = Time.time;
            while (true)
            {
                if (CanBeastsAct && Time.time >= nextStartTime && CountWorkingBeasts() < MaxBeastsAtOnce && TrySendNextBeast())
                    nextStartTime = Time.time + (_isLastStand ? _lastStandTimeBetweenBeastStarts : _timeBetweenBeastStarts);

                yield return null;
            }
        }

        private int CountWorkingBeasts()
        {
            int count = 0;
            foreach (UlvaneBeast beast in _activeBeasts.Values)
                if (beast && !beast.IsFinished)
                    count++;

            return count;
        }

        private bool TrySendNextBeast()
        {
            if (_turnOrder.Count == 0)
                RefillTurnOrder();

            for (int i = 0; i < _turnOrder.Count; i++)
            {
                UlvaneBeast beast = _activeBeasts[_turnOrder[i]];
                if (!beast || !beast.IsAvailable)
                    continue;

                _lastBeastAttack = _turnOrder[i];
                _turnOrder.RemoveAt(i);
                beast.Command();
                return true;
            }

            return false;
        }

        private void RefillTurnOrder()
        {
            _turnOrder.AddRange(_activeBeasts.Keys);
            for (int i = _turnOrder.Count - 1; i > 0; i--)
            {
                int swap = Random.Range(0, i + 1);
                (_turnOrder[i], _turnOrder[swap]) = (_turnOrder[swap], _turnOrder[i]);
            }

            // The animal that just went does not go first again
            if (_turnOrder.Count > 1 && _turnOrder[0] == _lastBeastAttack)
                (_turnOrder[0], _turnOrder[_turnOrder.Count - 1]) = (_turnOrder[_turnOrder.Count - 1], _turnOrder[0]);
        }

        // A special call: Ulvane gestures and the animal drops what it is doing and runs its skill, and he waits until it is done. Nothing picks this yet; the debug harness uses it.
        private IEnumerator BeastSequence(UlvaneAttack attack)
        {
            if (!_activeBeasts.TryGetValue(attack, out UlvaneBeast beast) || !beast || !beast.IsAvailable)
            {
                FinishAttack();
                yield break;
            }

            ShowDangerIcon(beast.HitDamage);
            anim.SetTrigger(Command);
            yield return WaitFacingPlayer(_commandDelay);

            beast.Command();
            for (float elapsed = 0f; beast && !beast.IsFinished && elapsed < _beastTimeout; elapsed += Time.deltaTime)
            {
                FacePlayer(Time.deltaTime);
                yield return null;
            }

            yield return WaitFacingPlayer(_afterBeastTime);
            FinishAttack();
        }

        // Combined command: Ulvane only screams (and goes back to shooting), the dog and the penguin do the rest by themselves, see SnowTornadoCombo
        private IEnumerator SnowTornadoSequence()
        {
            if (!TryGetTornadoPartners(out DogBeast dog, out PenguinBeast penguin))
            {
                FinishAttack();
                yield break;
            }

            _isComboRunning = true;
            StartCoroutine(SnowTornadoCombo(dog, penguin));
            anim.SetTrigger(Roar);
            yield return WaitFacingPlayer(_tornadoScreamTime);
            FinishAttack();
        }

        // The dog runs to the penguin's left and spins up a still whirlwind, the penguin feeds it snowballs, and when the dog holds a full stack it fires them all at the player at more than twice a normal snowball's speed. Both animals wear the danger icon. It runs beside Ulvane's own attacks and the other animals' skills.
        private IEnumerator SnowTornadoCombo(DogBeast dog, PenguinBeast penguin)
        {
            dog.BeginCombo();
            penguin.BeginCombo();
            dog.ShowDanger();
            penguin.ShowDanger();

            Vector3 penguinPosition = penguin.transform.position;
            Vector3 toPlayer = PlayerAimPoint - penguinPosition;
            toPlayer.y = 0f;
            toPlayer = toPlayer.sqrMagnitude > 0.0001f ? toPlayer.normalized : penguin.transform.forward;

            // Left of the penguin as it looks at the player; the other side if that one is outside the arena
            Vector3 left = Vector3.Cross(toPlayer, Vector3.up);
            Vector3 dogSpot = NavMeshUtility.Snap(penguinPosition + left * _tornadoPartnerDistance);
            if ((dogSpot - (penguinPosition + left * _tornadoPartnerDistance)).sqrMagnitude > 1f)
                dogSpot = NavMeshUtility.Snap(penguinPosition - left * _tornadoPartnerDistance);

            float deadline = Time.time + _tornadoTimeout;
            yield return dog.StormStart(dogSpot);

            for (int i = 0; i < dog.StackSize && Time.time < deadline; i++)
            {
                yield return penguin.ComboFeed(dog.transform, dog.AddSnowball);
                yield return new WaitForSeconds(_tornadoFeedInterval);
            }

            while (dog.StackCount < dog.StackSize && Time.time < deadline)
                yield return null;

            yield return dog.ThrowStack();

            dog.EndCombo();
            penguin.EndCombo();
            _nextTornadoTime = Time.time + Random.Range(_tornadoCooldown.x, _tornadoCooldown.y);
            _isComboRunning = false;
        }

        // Every animal is in the arena from the start of the fight, patrolling
        private void SpawnBeasts()
        {
            _navMesh = NavMesh.CalculateTriangulation();
            CacheTriangleAreas();
            _patrolZones = FindObjectsByType<UlvanePatrolZone>(FindObjectsSortMode.None);
            foreach (BeastEntry entry in _beasts)
            {
                if (!entry.prefab)
                    continue;

                UlvanePatrolZone zone = FindZone(entry.attack);
                Vector3 point = GetPatrolPoint(zone, entry.prefab.PatrolArea, _minSpawnDistanceFromPlayer);
                UlvaneBeast beast = Instantiate(entry.prefab);
                beast.Spawn(this, point, zone);
                _activeBeasts[entry.attack] = beast;
            }
        }

        private UlvanePatrolZone FindZone(UlvaneAttack attack)
        {
            foreach (UlvanePatrolZone zone in _patrolZones)
                if (zone.Beast == attack)
                    return zone;

            return null;
        }

        // Where a patrolling animal walks next: inside its own zone if it has one, otherwise anywhere on the floor except the other animals' zones
        public Vector3 GetPatrolPoint(UlvanePatrolZone zone, UlvanePatrolArea area, float minPlayerDistance = 0f, Vector3? from = null)
        {
            if (zone)
                return NavMeshUtility.Snap(zone.RandomPoint());

            Vector3 point = Vector3.zero;
            for (int i = 0; i < 60; i++)
            {
                point = RandomNavMeshPoint();
                Vector3 fromPlayer = point - player.position;
                fromPlayer.y = 0f;
                if (!IsInsideAnyZone(point) && IsInPatrolArea(point, area) && fromPlayer.sqrMagnitude >= minPlayerDistance * minPlayerDistance && IsWithinOuterStep(point, area, from, i))
                    return point;
            }

            return point;
        }

        // Outer animals stroll in short steps instead of cutting across the middle; they give up the limit when no such point turns up
        private bool IsWithinOuterStep(Vector3 point, UlvanePatrolArea area, Vector3? from, int attempt)
        {
            if (area != UlvanePatrolArea.NearWalls || !from.HasValue || attempt >= 40)
                return true;

            Vector3 step = point - from.Value;
            step.y = 0f;
            return step.sqrMagnitude <= _outerPatrolStep * _outerPatrolStep;
        }

        private bool IsInPatrolArea(Vector3 point, UlvanePatrolArea area)
        {
            if (area == UlvanePatrolArea.Anywhere)
                return true;

            Vector3 offset = point - _arenaCenter;
            offset.y = 0f;
            float sqrDistance = offset.sqrMagnitude;
            return area == UlvanePatrolArea.NearWalls
                ? sqrDistance >= _outerPatrolMinRadius * _outerPatrolMinRadius
                : sqrDistance <= _centerPatrolRadius * _centerPatrolRadius;
        }

        private bool IsInsideAnyZone(Vector3 point)
        {
            foreach (UlvanePatrolZone zone in _patrolZones)
                if (zone.Contains(point, 1f))
                    return true;

            return false;
        }

        // Triangles are picked by area, so the points spread evenly over the floor
        private void CacheTriangleAreas()
        {
            int count = _navMesh.indices.Length / 3;
            _cumulativeTriangleAreas = new float[count];
            float total = 0f;
            for (int i = 0; i < count; i++)
            {
                Vector3 a = _navMesh.vertices[_navMesh.indices[i * 3]];
                Vector3 b = _navMesh.vertices[_navMesh.indices[i * 3 + 1]];
                Vector3 c = _navMesh.vertices[_navMesh.indices[i * 3 + 2]];
                total += Vector3.Cross(b - a, c - a).magnitude * 0.5f;
                _cumulativeTriangleAreas[i] = total;
            }
        }

        private Vector3 RandomNavMeshPoint()
        {
            float pick = Random.value * _cumulativeTriangleAreas[_cumulativeTriangleAreas.Length - 1];
            int triangle = Array.BinarySearch(_cumulativeTriangleAreas, pick);
            if (triangle < 0)
                triangle = ~triangle;

            triangle = Mathf.Min(triangle, _cumulativeTriangleAreas.Length - 1) * 3;
            Vector3 a = _navMesh.vertices[_navMesh.indices[triangle]];
            Vector3 b = _navMesh.vertices[_navMesh.indices[triangle + 1]];
            Vector3 c = _navMesh.vertices[_navMesh.indices[triangle + 2]];
            float u = Random.value;
            float v = Random.value;
            if (u + v > 1f)
            {
                u = 1f - u;
                v = 1f - v;
            }

            return a + (b - a) * u + (c - a) * v;
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
