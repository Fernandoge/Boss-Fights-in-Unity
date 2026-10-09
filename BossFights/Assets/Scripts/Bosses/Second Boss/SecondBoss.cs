using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

namespace Bosses.Second_Boss
{
    public class SecondBoss : BossController
    {
        [Header("Spell Circles")]
        [SerializeField] private SpellCircle _spellCirclePrefab;
        [SerializeField] private int _circleCount = 7;
        [SerializeField] private float _circleRadius = 1.2f;
        [SerializeField] private float _castReleaseDelay = 0.28f;
        [SerializeField] private float _circleInterval = 0.42f;
        [SerializeField] private float _circleTelegraphTime = 0.575f;
        [SerializeField] private float _afterCastTime = 0.4f;
        [SerializeField] private int _circleDamage = 1;
        [SerializeField, Range(0f, 1f)] private float _leadPlayerChance = 0.5f;
        [SerializeField] private float _leadOffsetDistance = 0.8f;
        [SerializeField] private Vector2 _randomOffsetDistanceRange = new Vector2(0.5f, 2.5f);

        [Header("Diagonal Lines")]
        [SerializeField] private DiagonalLineBlast _lineBlastPrefab;
        [SerializeField] private float _lineWindUpTime = 0.45f;
        [SerializeField] private float _lineTeleportDelay = 0.3f;
        [SerializeField] private float _lineAfterBlastTime = 0.2f;
        [SerializeField] private LineBlastSettings _lineBlast = new LineBlastSettings();
        [SerializeField] private float _arenaEdgePadding = 0.6f;

        [Header("Orb Barrage")]
        [SerializeField] private BouncingOrb _orbPrefab;
        [SerializeField] private int _orbCount = 3;
        [SerializeField] private float _orbSpeed = 7f;
        [SerializeField] private float _orbLifetime = 9f;
        [SerializeField] private int _orbDamage = 1;
        [SerializeField] private float _orbReleaseDelay = 0.28f;
        [SerializeField] private float _orbThrowInterval = 0.7f;
        [SerializeField] private float _orbAfterThrowTime = 0.5f;
        [SerializeField] private float _orbAimSpread = 25f;
        [SerializeField] private float _orbSpawnDistance = 1.5f;
        [SerializeField] private float _orbWallInset = 0.2f;

        [Header("Starfall")]
        [SerializeField] private StarfallStar _starPrefab;
        [SerializeField] private int _starCount = 4;
        [SerializeField] private float _starCastDelay = 0.5f;
        [SerializeField] private float _starInterval = 0.4f;
        [SerializeField] private float _starAfterTime = 0.5f;
        [SerializeField, Range(0f, 1f)] private float _starNearPlayerChance = 0.4f;
        [SerializeField] private float _starMinSpacing = 7f;
        [SerializeField] private float _starMaxPlayerDistance = 15f;
        [SerializeField] private int _starPlacementAttempts = 25;
        [SerializeField] private float _starEdgeMargin = 3f;
        [SerializeField] private StarfallSettings _starfall = new StarfallSettings();

        [Header("Timed Explosions")]
        [SerializeField] private TimedExplosionZone _zonePrefab;
        [SerializeField] private int _zoneCount = 3;
        [SerializeField] private float _zoneCastDelay = 0.5f;
        [SerializeField] private float _zoneLightTime = 0.5f;
        [SerializeField] private float _zoneLightGap = 0.2f;
        [SerializeField] private float _zonePauseTime = 1.2f;
        [SerializeField] private float _zoneExplosionInterval = 1.3f;
        [SerializeField] private float _zoneTeleportDelay = 0.3f;
        [SerializeField] private int _zoneDamage = 1;
        [SerializeField, Range(0f, 1f)] private float _zoneVerticalChance = 0.5f;

        [Header("Phase 2")]
        [SerializeField] private int _phaseTwoOrbCount = 4;
        [SerializeField] private int _phaseTwoZoneSteps = 6;
        [SerializeField] private int _phaseTwoZoneCooldown = 2;
        [SerializeField] private float _phaseTwoLineSpeed = 1.3f;
        [SerializeField] private int _phaseTwoCircleSteps = 4;
        [SerializeField] private int _phaseTwoCirclesPerStep = 2;
        [SerializeField] private float _phaseTwoCircleDistance = 2.4f;
        [SerializeField] private float _powerUpTime = 3.5f;

        [Header("Color Intermission")]
        [SerializeField] private ColorSquareBoard _colorBoardPrefab;
        [SerializeField] private ClockNumberPopup _colorIconPrefab;
        [SerializeField] private Color[] _intermissionColors = { new Color(0.95f, 0.25f, 0.25f), new Color(0.3f, 0.55f, 1f), new Color(0.3f, 0.9f, 0.4f), new Color(1f, 0.85f, 0.2f) };
        [SerializeField] private IntermissionCharge _colorChargePrefab;
        [SerializeField] private float _colorPreludeTime = 5f;
        [SerializeField] private int _colorRounds = 6;
        [SerializeField] private int _colorResetAfterRound = 4;
        [SerializeField] private float _colorShuffleTime = 1.5f;
        [SerializeField] private float _colorShowTime = 5f;
        [SerializeField] private float _colorFadeTime = 0.5f;
        [SerializeField] private float _colorPauseTime = 1f;
        [SerializeField] private float _colorRunTime = 3f;
        [SerializeField] private float _colorAfterExplosionTime = 1.5f;
        [SerializeField] private float _colorRevealTime = 1f;
        [SerializeField] private int _colorDamage = 1;

        [Header("Clock Mechanic")]
        [SerializeField] private ClockNumberPopup _clockNumberPrefab;
        [SerializeField] private ClockNumberPopup _clockIconPrefab;
        [SerializeField, Range(0f, 1f)] private float _clockStartChance = 0.35f;
        [SerializeField] private int _clockMinAttacksBetween = 2;
        [SerializeField] private float _clockStartCastTime = 1.6f;
        [SerializeField] private float _clockIconDelay = 0.5f;
        [SerializeField] private float _clockStartTeleportDelay = 0.3f;
        [SerializeField] private float _clockCastStrikeTime = 0.82f;
        [SerializeField] private ClockWaveBlast _clockWavePrefab;
        [SerializeField] private Vector3 _clockNumberOffset = new Vector3(0f, 5.2f, 0f);
        [SerializeField] private float _clockIntroTime = 1f;
        [SerializeField] private float _clockWaveInterval = 2.4f;
        [SerializeField] private float _clockWaveTelegraphTime = 1.4f;
        [SerializeField] private float _clockEndTime = 1f;
        [SerializeField] private int _clockWaveDamage = 2;
        [SerializeField] private float _clockUpDownSpokeWidth = 3f;
        [SerializeField] private float _clockSideSpokeWidth = 6f;
        [SerializeField] private float _clockSpokeStartDistance = 1f;

        [Header("Teleport")]
        [SerializeField] private float _teleportMinPlayerDistance = 10f;
        [SerializeField] private float _teleportMaxPlayerDistance = 22f;
        [SerializeField] private float _teleportMinMoveDistance = 8f;
        [SerializeField] private float _teleportWallClearance = 3f;
        [SerializeField, Range(0f, 1f)] private float _teleportHighChance = 0.2f;
        [SerializeField] private float _teleportSkinnyWidthScale = 0.02f;
        [SerializeField] private float _teleportStretchHeightScale = 1.3f;
        [SerializeField] private float _teleportVanishTime = 0.25f;
        [SerializeField] private float _teleportAppearTime = 0.25f;
        [SerializeField] private int _teleportAttempts = 30;

        private Vector3 _originalScale;
        private Camera _camera;
        private Vector4 _arenaRect;
        private SecondBossAttack? _lastAttack;
        private int _attacksSinceZones = int.MaxValue / 2;
        private readonly List<int> _clockNumbers = new List<int>();
        private int _clockNumbersShown;
        private ClockNumberPopup _activeClockNumber;
        private ClockNumberPopup _activeClockIcon;
        private int _attacksSinceClock;
        private bool _clockActive;

        private static readonly int Cast = Animator.StringToHash("Cast");
        private static readonly int Cast_Area = Animator.StringToHash("CastArea");
        private static readonly int Cast_Area_State = Animator.StringToHash("Cast Area");
        private static readonly int Block_Start_State = Animator.StringToHash("Block Start");
        private static readonly int Power_Up_State = Animator.StringToHash("Power Up");
        private static readonly int Idle = Animator.StringToHash("Idle");
        private static readonly int Empty = Animator.StringToHash("Empty");
        private static readonly int[] ClockPositions = { 3, 6, 9, 12 };

        private const int CastHandLayer = 1;

        public SecondBossAttack? DebugOnlyAttack { get; set; }

        private int OrbCount => hasEnteredSecondPhase ? _phaseTwoOrbCount : _orbCount;

        private float LineSpeed => hasEnteredSecondPhase ? _phaseTwoLineSpeed : 1f;

        protected override void Start()
        {
            base.Start();
            _originalScale = transform.localScale;
            _arenaRect = GetArenaRect();
            _camera = Camera.main;
        }

        /// *** Debug *** ///

        public void DebugForceAttack(SecondBossAttack attack)
        {
            if (DebugIsBusy)
                return;

            base.PerformAttack();
            StartAttack(attack);
        }

        public void DebugForceIntermission()
        {
            if (DebugIsBusy)
                return;

            base.PerformAttack();
            StartCoroutine(ClockIntermissionSequence());
        }

        public void DebugForceClockStart()
        {
            if (DebugIsBusy)
                return;

            base.PerformAttack();
            StartCoroutine(ClockStartSequence());
        }

        public void DebugForceSpellCircles() => DebugForceAttack(SecondBossAttack.SpellCircles);

        public void DebugForceDiagonalLines() => DebugForceAttack(SecondBossAttack.DiagonalLines);

        public void DebugForceOrbBarrage() => DebugForceAttack(SecondBossAttack.OrbBarrage);

        public void DebugForceColorIntermission()
        {
            if (DebugIsBusy)
                return;

            base.PerformAttack();
            StartCoroutine(ColorIntermissionSequence());
        }

        public void DebugForceStarfall() => DebugForceAttack(SecondBossAttack.Starfall);

        public void DebugForceTimedExplosions() => DebugForceAttack(SecondBossAttack.TimedExplosions);

        public void DebugTeleport()
        {
            if (DebugIsBusy)
                return;

            isPerformingAttack = true;
            navMeshAgent.isStopped = true;
            StartCoroutine(TeleportThenFinishAttack());
        }

        /// *** Boss Overrides *** ///

        // The boss never chases: it only turns to face the player
        protected override void IdleMovement()
        {
            navMeshAgent.isStopped = true;
            FacePlayer(Time.deltaTime);
        }

        protected override void PerformAttack()
        {
            base.PerformAttack();

            // The clock mechanic is not running all the time: sometimes the boss casts the clock, then the next four attacks show a number each, then the intermission comes (not while one attack is being tested on its own)
            bool clockMechanicEnabled = !DebugOnlyAttack.HasValue && !hasEnteredSecondPhase;
            if (clockMechanicEnabled && _clockActive && _clockNumbersShown >= ClockPositions.Length)
            {
                StartCoroutine(ClockIntermissionSequence());
                return;
            }

            if (clockMechanicEnabled && !_clockActive && _attacksSinceClock >= _clockMinAttacksBetween && Random.value < _clockStartChance)
            {
                StartCoroutine(ClockStartSequence());
                return;
            }

            SecondBossAttack attack = ChooseAttack();
            if (clockMechanicEnabled && _clockActive)
                ShowNextClockNumber();
            else if (clockMechanicEnabled)
                _attacksSinceClock++;

            StartAttack(attack);
        }

        // At half health the boss goes to the middle for the color intermission; there is no second phase after it yet, so it simply goes back to attacking
        protected override void OnDefeated()
        {
            ClearLingeringHazards();
            ResetClockMechanic();

            foreach (SpellCircle circle in FindObjectsByType<SpellCircle>(FindObjectsSortMode.None))
                Destroy(circle.gameObject);

            foreach (DiagonalLineBlast blast in FindObjectsByType<DiagonalLineBlast>(FindObjectsSortMode.None))
                Destroy(blast.gameObject);

            foreach (ClockWaveBlast wave in FindObjectsByType<ClockWaveBlast>(FindObjectsSortMode.None))
                Destroy(wave.gameObject);

            foreach (ColorSquareBoard board in FindObjectsByType<ColorSquareBoard>(FindObjectsSortMode.None))
                Destroy(board.gameObject);

            foreach (ClockNumberPopup popup in FindObjectsByType<ClockNumberPopup>(FindObjectsSortMode.None))
                Destroy(popup.gameObject);

            foreach (IntermissionCharge charge in FindObjectsByType<IntermissionCharge>(FindObjectsSortMode.None))
                Destroy(charge.gameObject);

            // The hands layer would keep casting over the death animation
            anim.ResetTrigger(Cast);
            anim.SetLayerWeight(CastHandLayer, 0f);
        }

        protected override IEnumerator EnterSecondPhase()
        {
            if (hasEnteredSecondPhase)
                yield break;

            hasEnteredSecondPhase = true;
            yield return new WaitUntil(() => !isPerformingAttack && !isPerformingAction);
            MarkBusy();
            yield return ColorIntermissionSequence();
        }

        /// *** Attack Selection *** ///

        // A random attack, but never the same one as the last (with two attacks they simply alternate)
        private SecondBossAttack ChooseAttack()
        {
            if (DebugOnlyAttack.HasValue)
                return DebugOnlyAttack.Value;

            SecondBossAttack[] attacks = (SecondBossAttack[])Enum.GetValues(typeof(SecondBossAttack));
            SecondBossAttack attack;
            do
                attack = attacks[Random.Range(0, attacks.Length)];
            while (attacks.Length > 1 && ((_lastAttack.HasValue && attack == _lastAttack.Value) || (hasEnteredSecondPhase && attack == SecondBossAttack.TimedExplosions && IsExplosionZonesOnCooldown())));

            return attack;
        }

        // Phase 2: the explosion zones can be cast again only after at least 2 other attacks and only when the zones of the last cast are gone, so two memory games never overlap
        private bool IsExplosionZonesOnCooldown() => _attacksSinceZones < _phaseTwoZoneCooldown || FindFirstObjectByType<TimedExplosionZone>() != null;

        private void StartAttack(SecondBossAttack attack)
        {
            _lastAttack = attack;
            _attacksSinceZones = attack == SecondBossAttack.TimedExplosions ? 0 : _attacksSinceZones + 1;
            switch (attack)
            {
                case SecondBossAttack.DiagonalLines:
                    StartCoroutine(DiagonalLinesSequence());
                    break;
                case SecondBossAttack.OrbBarrage:
                    StartCoroutine(OrbBarrageSequence());
                    break;
                case SecondBossAttack.Starfall:
                    StartCoroutine(StarfallSequence());
                    break;
                case SecondBossAttack.TimedExplosions:
                    StartCoroutine(TimedExplosionsSequence());
                    break;
                default:
                    StartCoroutine(SpellCirclesSequence());
                    break;
            }
        }

        /// *** Spell Circles *** ///

        private IEnumerator SpellCirclesSequence()
        {
            // Every circle restarts the cast animation, so the boss looks like it is spamming the spell
            int steps = hasEnteredSecondPhase ? _phaseTwoCircleSteps : _circleCount;
            for (int i = 0; i < steps; i++)
            {
                anim.SetTrigger(Cast);
                yield return WaitFacingPlayer(_castReleaseDelay);
                SpawnSpellCircle();
                yield return WaitFacingPlayer(Mathf.Max(0f, _circleInterval - _castReleaseDelay));
            }

            // Let the last circles land before vanishing
            yield return WaitFacingPlayer(Mathf.Max(0f, _circleTelegraphTime - _circleInterval) + _afterCastTime);
            yield return TeleportThenFinishAttack();
        }

        private void SpawnSpellCircle()
        {
            Vector3 center = GetSpellCircleCenter();
            if (!hasEnteredSecondPhase)
            {
                Instantiate(_spellCirclePrefab).Begin(center, _circleRadius, _circleTelegraphTime, _circleDamage);
                return;
            }

            // Phase 2: two circles on opposite sides of the aimed spot, far enough apart to leave a safe place in the middle, so the player either stands in the middle or runs away
            float startAngle = Random.value * 360f;
            for (int i = 0; i < _phaseTwoCirclesPerStep; i++)
            {
                Vector3 direction = Quaternion.Euler(0f, startAngle + i * 360f / _phaseTwoCirclesPerStep, 0f) * Vector3.forward;
                Vector3 circleCenter = center + direction * _phaseTwoCircleDistance;
                circleCenter.x = Mathf.Clamp(circleCenter.x, _arenaRect.x + _circleRadius, _arenaRect.z - _circleRadius);
                circleCenter.z = Mathf.Clamp(circleCenter.z, _arenaRect.y + _circleRadius, _arenaRect.w - _circleRadius);
                Instantiate(_spellCirclePrefab).Begin(circleCenter, _circleRadius, _circleTelegraphTime, _circleDamage);
            }
        }

        // Either aims where the player is heading or lands at a random offset from where they stand, so players cannot rely on one dodge habit
        private Vector3 GetSpellCircleCenter()
        {
            Vector3 center = PerceivedPlayerPosition;
            if (Random.value < _leadPlayerChance)
            {
                // Right after a fast move the boss does not know where the player is heading
                center += PerceivedPlayerVelocity * _circleTelegraphTime + GetRandomFlatOffset(0f, _leadOffsetDistance);
            }
            else
                center += GetRandomFlatOffset(_randomOffsetDistanceRange.x, _randomOffsetDistanceRange.y);

            return NavMesh.SamplePosition(center, out NavMeshHit hit, 3f, NavMesh.AllAreas) ? hit.position : PerceivedPlayerPosition;
        }

        private static Vector3 GetRandomFlatOffset(float minDistance, float maxDistance)
        {
            Vector2 direction = Random.insideUnitCircle.normalized;
            float distance = Random.Range(minDistance, maxDistance);
            return new Vector3(direction.x, 0f, direction.y) * distance;
        }

        /// *** Orb Barrage *** ///

        // Throws the orbs one after the other, each at a different angle around the player; they keep bouncing after the boss has teleported away
        private IEnumerator OrbBarrageSequence()
        {
            int count = OrbCount;
            List<float> angles = GetOrbAimAngles(count);
            for (int i = 0; i < count; i++)
            {
                anim.SetTrigger(Cast);
                yield return WaitFacingPlayer(_orbReleaseDelay);
                SpawnOrb(angles[i]);
                yield return WaitFacingPlayer(Mathf.Max(0f, _orbThrowInterval - _orbReleaseDelay));
            }

            yield return WaitFacingPlayer(_orbAfterThrowTime);
            yield return TeleportThenFinishAttack();
        }

        // The throw angles are spread evenly across the aim cone in a random order, so one dodge does not beat all of them
        private List<float> GetOrbAimAngles(int count)
        {
            List<float> angles = new List<float>();
            for (int i = 0; i < count; i++)
                angles.Add(count > 1 ? Mathf.Lerp(-_orbAimSpread, _orbAimSpread, i / (float)(count - 1)) : 0f);

            for (int i = angles.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (angles[i], angles[j]) = (angles[j], angles[i]);
            }

            return angles;
        }

        private void SpawnOrb(float aimAngle)
        {
            Vector3 toPlayer = PerceivedPlayerPosition - transform.position;
            toPlayer.y = 0f;
            Vector3 direction = Quaternion.Euler(0f, aimAngle, 0f) * (toPlayer == Vector3.zero ? transform.forward : toPlayer.normalized);

            BouncingOrb orb = Instantiate(_orbPrefab);
            orb.Begin(transform.position + direction * _orbSpawnDistance, direction, _orbSpeed, GetOrbBounds(), _orbLifetime, _orbDamage);
        }

        // Where the center of an orb may go: the walkable area, pulled in so the orb touches the wall instead of sinking into it
        private Vector4 GetOrbBounds()
        {
            float inset = _arenaEdgePadding + _orbWallInset;
            return new Vector4(_arenaRect.x + inset, _arenaRect.y + inset, _arenaRect.z - inset, _arenaRect.w - inset);
        }

        /// *** Starfall *** ///

        // Raises both hands and calls the stars down one after the other, each aimed near the player like the spell circles; they keep falling after the boss has teleported away
        private IEnumerator StarfallSequence()
        {
            PlayCastArea();
            yield return WaitFacingPlayer(_starCastDelay);

            List<Vector3> centers = new List<Vector3>();
            float lastLandingTime = 0f;
            for (int i = 0; i < _starCount; i++)
            {
                Vector3 center = GetStarCenter(centers);
                centers.Add(center);
                Instantiate(_starPrefab).Begin(center, _starfall, _arenaRect);
                lastLandingTime = Time.time + _starfall.telegraphTime;
                yield return WaitFacingPlayer(_starInterval);
            }

            yield return WaitFacingPlayer(_starAfterTime);
            yield return TeleportRoutine();

            // The next attack starts casting as the last star lands, so the player is not asked to dodge a new one while the stars are still falling
            yield return WaitFacingPlayer(Mathf.Max(0f, lastLandingTime - timeBetweenAttacks - Time.time));
            FinishAttack();
        }

        // Each star either aims near the player like a spell circle or lands at a random spot not too far from them, and never close to a star already placed, so the stars spread over the arena; they also stay away from the edges so the whole star on the floor is inside the map
        private Vector3 GetStarCenter(List<Vector3> placed)
        {
            Vector3 best = KeepAwayFromEdges(GetSpellCircleCenter());
            float bestSpacing = -1f;
            for (int attempt = 0; attempt < _starPlacementAttempts; attempt++)
            {
                Vector3 candidate = KeepAwayFromEdges(Random.value < _starNearPlayerChance ? GetSpellCircleCenter() : GetRandomStarSpot());
                float spacing = GetDistanceToNearest(candidate, placed);
                if (spacing >= _starMinSpacing)
                    return candidate;

                if (spacing > bestSpacing)
                {
                    bestSpacing = spacing;
                    best = candidate;
                }
            }

            return best;
        }

        private Vector3 GetRandomStarSpot()
        {
            Vector3 spot = PerceivedPlayerPosition + GetRandomFlatOffset(0f, _starMaxPlayerDistance);
            spot.x = Mathf.Clamp(spot.x, _arenaRect.x, _arenaRect.z);
            spot.z = Mathf.Clamp(spot.z, _arenaRect.y, _arenaRect.w);
            return NavMesh.SamplePosition(spot, out NavMeshHit hit, 3f, NavMesh.AllAreas) ? hit.position : PerceivedPlayerPosition;
        }

        private Vector3 KeepAwayFromEdges(Vector3 position)
        {
            position.x = Mathf.Clamp(position.x, _arenaRect.x + _starEdgeMargin, _arenaRect.z - _starEdgeMargin);
            position.z = Mathf.Clamp(position.z, _arenaRect.y + _starEdgeMargin, _arenaRect.w - _starEdgeMargin);
            return position;
        }

        private static float GetDistanceToNearest(Vector3 position, List<Vector3> others)
        {
            float nearest = float.MaxValue;
            foreach (Vector3 other in others)
                nearest = Mathf.Min(nearest, Vector3.Distance(position, other));

            return nearest;
        }

        /// *** Timed Explosions *** ///

        // Three zones (a third of the arena each) light up one after the other and go dark; after a pause they explode in the same order, with no warning
        private IEnumerator TimedExplosionsSequence()
        {
            PlayCastArea();
            yield return WaitFacingPlayer(_zoneCastDelay);

            List<Vector4> zones = hasEnteredSecondPhase ? GetPhaseTwoExplosionZones() : GetExplosionZones(Random.value < _zoneVerticalChance, _zoneCount);

            float lightPhase = zones.Count * _zoneLightTime + (zones.Count - 1) * _zoneLightGap;
            for (int i = 0; i < zones.Count; i++)
                Instantiate(_zonePrefab).Begin(zones[i], transform.position.y, i * (_zoneLightTime + _zoneLightGap), _zoneLightTime, lightPhase + _zonePauseTime + i * _zoneExplosionInterval, _zoneDamage);

            // The boss teleports once the lights are out; the next attack starts casting as the first explosion goes off, while the others are still to come
            float firstExplosionTime = Time.time + lightPhase + _zonePauseTime;
            yield return WaitFacingPlayer(lightPhase + _zoneTeleportDelay);
            yield return TeleportRoutine();
            yield return WaitFacingPlayer(Mathf.Max(0f, firstExplosionTime - timeBetweenAttacks - Time.time));
            FinishAttack();
        }

        // Phase 2: a full set of thirds (all vertical or all horizontal, so no spot of the arena is ever safe) plus extra steps picked from the thirds of both directions, in a random order, so zones can come up twice
        private List<Vector4> GetPhaseTwoExplosionZones()
        {
            bool fullSetIsVertical = Random.value < _zoneVerticalChance;
            List<Vector4> sequence = GetExplosionZones(fullSetIsVertical, _zoneCount);

            // The first extra step always comes from the other direction, so the sequence always mixes vertical and horizontal zones
            List<Vector4> other = GetExplosionZones(!fullSetIsVertical, _zoneCount);
            List<Vector4> pool = new List<Vector4>(sequence);
            pool.AddRange(other);
            for (int i = sequence.Count; i < _phaseTwoZoneSteps; i++)
                sequence.Add(i == _zoneCount ? other[Random.Range(0, other.Count)] : pool[Random.Range(0, pool.Count)]);

            for (int i = sequence.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (sequence[i], sequence[j]) = (sequence[j], sequence[i]);
            }

            return sequence;
        }

        // Either vertical strips (left to right) or horizontal bands (bottom to top), all the same size, in a random order
        private List<Vector4> GetExplosionZones(bool vertical, int count)
        {
            List<Vector4> zones = new List<Vector4>();
            float width = (_arenaRect.z - _arenaRect.x) / count;
            float depth = (_arenaRect.w - _arenaRect.y) / count;
            for (int i = 0; i < count; i++)
                zones.Add(vertical
                    ? new Vector4(_arenaRect.x + i * width, _arenaRect.y, _arenaRect.x + (i + 1) * width, _arenaRect.w)
                    : new Vector4(_arenaRect.x, _arenaRect.y + i * depth, _arenaRect.z, _arenaRect.y + (i + 1) * depth));

            for (int i = zones.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (zones[i], zones[j]) = (zones[j], zones[i]);
            }

            return zones;
        }

        /// *** Color Intermission *** ///

        // The squares of the floor get colors that are shown for a few seconds and then hidden; then the boss asks for one color after another (every color at least once, some twice) and every square of another color explodes
        private IEnumerator ColorIntermissionSequence()
        {
            ResetClockMechanic();

            yield return TeleportRoutine(GetArenaCenter());
            isImmuneToDamage = true;

            // In the middle the boss looks straight ahead (toward the camera) for the whole intermission and does not follow the player
            transform.rotation = Quaternion.LookRotation(Vector3.back);

            // Warning that an intermission is coming: whatever is left of the earlier attacks vanishes and the boss guards itself in the middle (it stays in the guard pose for the whole intermission) while an orb above its head charges up before the colors appear
            ClearLingeringHazards();
            anim.CrossFadeInFixedTime(Block_Start_State, 0.15f);
            Instantiate(_colorChargePrefab).Begin(transform, _clockNumberOffset, _intermissionColors, _colorPreludeTime);
            yield return new WaitForSeconds(_colorPreludeTime);

            ColorSquareBoard board = Instantiate(_colorBoardPrefab);
            board.Begin(_arenaRect, transform.position.y, _intermissionColors);

            board.ShowColors(_colorFadeTime);
            yield return new WaitForSeconds(_colorShowTime);
            board.HideColors(_colorFadeTime);
            yield return new WaitForSeconds(_colorPauseTime);

            int round = 0;
            foreach (int color in GetColorRounds())
            {
                // After a few rounds the colors are thrown away: the squares shuffle, the new colors are shown for as long as the first ones and hidden again
                if (round == _colorResetAfterRound)
                {
                    PlayCastArea();
                    board.Reshuffle(_colorShuffleTime);
                    yield return new WaitForSeconds(_colorShuffleTime + _colorFadeTime);
                    yield return new WaitForSeconds(_colorShowTime);
                    board.HideColors(_colorFadeTime);
                    anim.CrossFadeInFixedTime(Block_Start_State, 0.15f);
                    yield return new WaitForSeconds(_colorPauseTime);
                }

                round++;
                ClockNumberPopup icon = Instantiate(_colorIconPrefab);
                icon.Begin(string.Empty, transform, _clockNumberOffset, 1f);
                icon.SetIconColor(_intermissionColors[color]);

                yield return new WaitForSeconds(_colorRunTime);
                board.Explode(color, _colorDamage, _colorRevealTime);
                icon.Hide();
                yield return new WaitForSeconds(_colorAfterExplosionTime);
            }

            // The boss crouches and bursts upward with its arms thrown open as the second phase starts
            board.Finish(_colorFadeTime);
            anim.CrossFadeInFixedTime(Power_Up_State, 0.2f);
            yield return new WaitForSeconds(_powerUpTime);
            yield return TeleportThenFinishAttack();
        }

        // Every color is asked at least once and the other rounds repeat random colors, all in a random order
        private List<int> GetColorRounds()
        {
            List<int> rounds = new List<int>();
            for (int i = 0; i < _colorRounds; i++)
                rounds.Add(i < _intermissionColors.Length ? i : Random.Range(0, _intermissionColors.Length));

            for (int i = rounds.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (rounds[i], rounds[j]) = (rounds[j], rounds[i]);
            }

            return rounds;
        }

        // The intermission is a memory test, so attacks still on the field (bouncing orbs, falling stars, lasers, explosion zones) are removed when it starts
        private static void ClearLingeringHazards()
        {
            BouncingOrb.DespawnAll();

            foreach (StarfallStar star in FindObjectsByType<StarfallStar>(FindObjectsSortMode.None))
                Destroy(star.gameObject);

            foreach (StarLaser laser in FindObjectsByType<StarLaser>(FindObjectsSortMode.None))
                Destroy(laser.gameObject);

            foreach (TimedExplosionZone zone in FindObjectsByType<TimedExplosionZone>(FindObjectsSortMode.None))
                Destroy(zone.gameObject);
        }

        private void ResetClockMechanic()
        {
            _clockActive = false;
            _clockNumbers.Clear();
            _clockNumbersShown = 0;
            _attacksSinceClock = 0;
            HideClockNumber();
        }

        // base cannot be used inside an iterator, so the coroutines mark the boss as busy through here
        private void MarkBusy() => base.PerformAttack();

        /// *** Diagonal Lines *** ///

        // Casts the lines, then teleports while they charge; the next attack waits for the explosion
        private IEnumerator DiagonalLinesSequence()
        {
            // Phase 2 plays the whole attack faster: wind-up, warning, teleport delay and the wait after the blast are all divided by the speed
            anim.SetTrigger(Cast_Area);
            yield return WaitFacingPlayer(_lineWindUpTime / LineSpeed);

            float blastTime = Time.time + _lineBlast.telegraphTime / LineSpeed;
            SpawnLineBlast();

            yield return WaitFacingPlayer(_lineTeleportDelay / LineSpeed);
            yield return TeleportRoutine();

            yield return WaitFacingPlayer(Mathf.Max(0f, blastTime - Time.time) + _lineAfterBlastTime / LineSpeed);
            FinishAttack();
        }

        private void SpawnLineBlast()
        {
            DiagonalLineBlast blast = Instantiate(_lineBlastPrefab);
            blast.Begin(_arenaRect, transform.position.y, _lineBlast.WithTelegraphTime(_lineBlast.telegraphTime / LineSpeed));
        }

        // The walkable area, taken from the baked NavMesh and widened by the agent radius
        private Vector4 GetArenaRect()
        {
            NavMeshTriangulation triangulation = NavMesh.CalculateTriangulation();
            if (triangulation.vertices.Length == 0)
                return new Vector4(-15f, -15f, 15f, 15f);

            Vector3 min = triangulation.vertices[0];
            Vector3 max = min;
            foreach (Vector3 vertex in triangulation.vertices)
            {
                min = Vector3.Min(min, vertex);
                max = Vector3.Max(max, vertex);
            }

            return new Vector4(min.x - _arenaEdgePadding, min.z - _arenaEdgePadding, max.x + _arenaEdgePadding, max.z + _arenaEdgePadding);
        }

        /// *** Clock Mechanic *** ///

        // A turn on its own: the boss casts a clock above its head, then teleports so the first number appears at a new spot; the next four attacks show the numbers
        private IEnumerator ClockStartSequence()
        {
            _clockActive = true;
            _clockNumbersShown = 0;

            anim.SetTrigger(Cast_Area);

            // The clock shows up when the hands are raised
            yield return WaitFacingPlayer(_clockIconDelay);
            if (_clockIconPrefab)
            {
                _activeClockIcon = Instantiate(_clockIconPrefab);
                _activeClockIcon.Begin(string.Empty, transform, _clockNumberOffset, 1f);
            }

            yield return WaitFacingPlayer(Mathf.Max(0f, _clockStartCastTime - _clockIconDelay));

            if (_activeClockIcon)
                _activeClockIcon.Hide();

            _activeClockIcon = null;
            yield return WaitFacingPlayer(_clockStartTeleportDelay);
            yield return TeleportThenFinishAttack();
        }

        // While the clock runs, every attack drops one clock number (3, 6, 9 or 12) next to the boss; after all four the intermission asks the player to remember them in order
        private void ShowNextClockNumber()
        {
            if (_clockNumbersShown == 0)
                ShuffleClockNumbers();

            int number = _clockNumbers[_clockNumbersShown];
            _clockNumbersShown++;

            // The number stays next to the boss while the skill is being cast and goes away when it ends
            if (_clockNumberPrefab)
            {
                HideClockNumber();
                _activeClockNumber = Instantiate(_clockNumberPrefab);
                _activeClockNumber.Begin(number.ToString(), transform, _clockNumberOffset, 1f);
            }
        }

        private void HideClockNumber()
        {
            if (_activeClockNumber)
                _activeClockNumber.Hide();

            _activeClockNumber = null;
        }

        // Restarts the area cast from its first frame even if the last one is still playing, so a trigger never waits in line and replays later
        private void PlayCastArea()
        {
            anim.ResetTrigger(Cast_Area);
            anim.CrossFadeInFixedTime(Cast_Area_State, 0.1f);
        }

        private void FinishAttack()
        {
            isPerformingAttack = false;
            HideClockNumber();
        }

        private void ShuffleClockNumbers()
        {
            _clockNumbers.Clear();
            _clockNumbers.AddRange(ClockPositions);
            for (int i = _clockNumbers.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (_clockNumbers[i], _clockNumbers[j]) = (_clockNumbers[j], _clockNumbers[i]);
            }
        }

        // The boss goes to the middle and sends one wave per number, in the order the numbers appeared; each wave is safe only on the line of its number
        private IEnumerator ClockIntermissionSequence()
        {
            if (_clockNumbers.Count != ClockPositions.Length)
                ShuffleClockNumbers();

            // Attacks still on the field would turn the memory test into a dodge test and felt unfair
            ClearLingeringHazards();

            Vector3 center = GetArenaCenter();
            yield return TeleportRoutine(center);

            isImmuneToDamage = true;
            yield return WaitFacingPlayer(_clockIntroTime);

            for (int i = 0; i < _clockNumbers.Count; i++)
            {
                // The camera squashes the depth axis by half, so the side spokes (which are wide along the depth) need twice the width to look as thick as the up/down ones
                bool isUpDown = _clockNumbers[i] == 12 || _clockNumbers[i] == 6;
                Instantiate(_clockWavePrefab).Begin(_arenaRect, transform.position.y, center, GetClockDirection(_clockNumbers[i]),
                    isUpDown ? _clockUpDownSpokeWidth : _clockSideSpokeWidth, _clockSpokeStartDistance, _clockWaveTelegraphTime, _clockWaveDamage);

                // The cast starts late enough for the hands to come down just as the wave explodes
                float castDelay = Mathf.Max(0f, _clockWaveTelegraphTime - _clockCastStrikeTime);
                yield return WaitFacingPlayer(castDelay);
                PlayCastArea();

                bool isLastWave = i == _clockNumbers.Count - 1;
                yield return WaitFacingPlayer(Mathf.Max(0f, (isLastWave ? _clockWaveTelegraphTime + _clockEndTime : _clockWaveInterval) - castDelay));
            }

            _clockNumbers.Clear();
            _clockNumbersShown = 0;
            _clockActive = false;
            _attacksSinceClock = 0;
            yield return TeleportThenFinishAttack();
        }

        // 12 is up on the screen, 3 right, 6 down and 9 left
        private static Vector3 GetClockDirection(int number)
        {
            switch (number)
            {
                case 3: return Vector3.right;
                case 6: return Vector3.back;
                case 9: return Vector3.left;
                default: return Vector3.forward;
            }
        }

        private Vector3 GetArenaCenter()
        {
            Vector3 center = new Vector3((_arenaRect.x + _arenaRect.z) * 0.5f, transform.position.y, (_arenaRect.y + _arenaRect.w) * 0.5f);
            return NavMesh.SamplePosition(center, out NavMeshHit hit, 3f, NavMesh.AllAreas) ? hit.position : center;
        }

        /// *** Teleport *** ///

        private IEnumerator TeleportThenFinishAttack()
        {
            yield return TeleportRoutine();
            FinishAttack();
        }

        private IEnumerator TeleportRoutine(Vector3? destination = null)
        {
            isImmuneToDamage = true;
            anim.CrossFade(Idle, 0.1f);
            anim.CrossFade(Empty, 0.1f, CastHandLayer);
            yield return ScaleOverTime(_originalScale, GetSkinnyScale(), _teleportVanishTime);

            navMeshAgent.Warp(destination ?? FindTeleportPosition());
            FacePlayer(1000f);

            yield return ScaleOverTime(GetSkinnyScale(), _originalScale, _teleportAppearTime);
            isImmuneToDamage = false;
        }

        // Thin and tall, like the boss is pulled into a line of light
        private Vector3 GetSkinnyScale() => new Vector3(
            _originalScale.x * _teleportSkinnyWidthScale,
            _originalScale.y * _teleportStretchHeightScale,
            _originalScale.z * _teleportSkinnyWidthScale);

        // A random spot on the NavMesh in a ring around the player, away from the walls and from where the boss stands now
        private Vector3 FindTeleportPosition()
        {
            // Mostly stay low enough on the screen for the clock number above the head to be seen; sometimes go anywhere
            bool allowHigh = Random.value < _teleportHighChance;
            for (int i = 0; i < _teleportAttempts; i++)
            {
                Vector3 offset = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f) * Vector3.forward
                                 * Random.Range(_teleportMinPlayerDistance, _teleportMaxPlayerDistance);
                if (!NavMesh.SamplePosition(player.position + offset, out NavMeshHit hit, 2f, NavMesh.AllAreas))
                    continue;

                if ((hit.position - player.position).sqrMagnitude < _teleportMinPlayerDistance * _teleportMinPlayerDistance)
                    continue;

                if ((hit.position - transform.position).sqrMagnitude < _teleportMinMoveDistance * _teleportMinMoveDistance)
                    continue;

                if (NavMesh.FindClosestEdge(hit.position, out NavMeshHit edge, NavMesh.AllAreas) && edge.distance < _teleportWallClearance)
                    continue;

                if (!allowHigh && !IsClockNumberVisibleAt(hit.position))
                    continue;

                return hit.position;
            }

            return transform.position;
        }

        private bool IsClockNumberVisibleAt(Vector3 position)
        {
            if (!_camera || !_clockNumberPrefab)
                return true;

            return _camera.WorldToViewportPoint(position + _clockNumberOffset).y <= _clockNumberPrefab.MaxViewportY;
        }

        private IEnumerator ScaleOverTime(Vector3 from, Vector3 to, float duration)
        {
            for (float elapsed = 0f; elapsed < duration; elapsed += Time.deltaTime)
            {
                transform.localScale = Vector3.Lerp(from, to, Mathf.SmoothStep(0f, 1f, elapsed / duration));
                yield return null;
            }

            transform.localScale = to;
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
