using System;
using System.Collections;
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
        [SerializeField] private float _circleTelegraphTime = 0.5f;
        [SerializeField] private float _afterCastTime = 0.4f;
        [SerializeField] private int _circleDamage = 1;
        [SerializeField, Range(0f, 1f)] private float _leadPlayerChance = 0.5f;
        [SerializeField] private float _leadOffsetDistance = 0.8f;
        [SerializeField] private Vector2 _randomOffsetDistanceRange = new Vector2(0.5f, 2.5f);

        [Header("Aiming")]
        [SerializeField] private float _jumpReactionDelay = 1f;
        [SerializeField] private float _jumpDetectDistance = 2.5f;

        [Header("Diagonal Lines")]
        [SerializeField] private DiagonalLineBlast _lineBlastPrefab;
        [SerializeField] private float _lineWindUpTime = 0.45f;
        [SerializeField] private float _lineTeleportDelay = 0.3f;
        [SerializeField] private float _lineAfterBlastTime = 0.2f;
        [SerializeField] private LineBlastSettings _lineBlast = new LineBlastSettings();
        [SerializeField] private float _arenaEdgePadding = 0.6f;

        [Header("Teleport")]
        [SerializeField] private float _teleportMinPlayerDistance = 10f;
        [SerializeField] private float _teleportMaxPlayerDistance = 22f;
        [SerializeField] private float _teleportMinMoveDistance = 8f;
        [SerializeField] private float _teleportWallClearance = 3f;
        [SerializeField] private float _teleportSkinnyWidthScale = 0.02f;
        [SerializeField] private float _teleportStretchHeightScale = 1.3f;
        [SerializeField] private float _teleportVanishTime = 0.25f;
        [SerializeField] private float _teleportAppearTime = 0.25f;
        [SerializeField] private int _teleportAttempts = 30;

        private Vector3 _originalScale;
        private Vector4 _arenaRect;
        private Vector3 _perceivedPlayerPosition;
        private Vector3 _lastPlayerPosition;
        private float _jumpReactionTimer;
        private SecondBossAttack? _lastAttack;

        private static readonly int Cast = Animator.StringToHash("Cast");
        private static readonly int Cast_Area = Animator.StringToHash("CastArea");
        private static readonly int Idle = Animator.StringToHash("Idle");
        private static readonly int Empty = Animator.StringToHash("Empty");

        private const int CastHandLayer = 1;

        public SecondBossAttack? DebugOnlyAttack { get; set; }

        protected override void Start()
        {
            base.Start();
            _originalScale = transform.localScale;
            _arenaRect = GetArenaRect();
            _perceivedPlayerPosition = player.position;
            _lastPlayerPosition = player.position;
        }

        // The boss keeps aiming at where the player was for a moment after a jump, as if it needs a second to notice the new spot
        private void LateUpdate()
        {
            if ((player.position - _lastPlayerPosition).sqrMagnitude > _jumpDetectDistance * _jumpDetectDistance)
                _jumpReactionTimer = _jumpReactionDelay;

            _lastPlayerPosition = player.position;

            if (_jumpReactionTimer > 0f)
                _jumpReactionTimer -= Time.deltaTime;
            else
                _perceivedPlayerPosition = player.position;
        }

        /// *** Debug *** ///

        public void DebugForceAttack(SecondBossAttack attack)
        {
            if (DebugIsBusy)
                return;

            base.PerformAttack();
            StartAttack(attack);
        }

        public void DebugForceSpellCircles() => DebugForceAttack(SecondBossAttack.SpellCircles);

        public void DebugForceDiagonalLines() => DebugForceAttack(SecondBossAttack.DiagonalLines);

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
            StartAttack(ChooseAttack());
        }

        // No second phase yet; the base version would freeze the boss without a transition animation
        protected override IEnumerator EnterSecondPhase()
        {
            yield break;
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
            while (attacks.Length > 1 && _lastAttack.HasValue && attack == _lastAttack.Value);

            return attack;
        }

        private void StartAttack(SecondBossAttack attack)
        {
            _lastAttack = attack;
            StartCoroutine(attack == SecondBossAttack.DiagonalLines ? DiagonalLinesSequence() : SpellCirclesSequence());
        }

        /// *** Spell Circles *** ///

        private IEnumerator SpellCirclesSequence()
        {
            // Every circle restarts the cast animation, so the boss looks like it is spamming the spell
            for (int i = 0; i < _circleCount; i++)
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
            SpellCircle circle = Instantiate(_spellCirclePrefab);
            circle.Begin(GetSpellCircleCenter(), _circleRadius, _circleTelegraphTime, _circleDamage);
        }

        // Either aims where the player is heading or lands at a random offset from where they stand, so players cannot rely on one dodge habit
        private Vector3 GetSpellCircleCenter()
        {
            Vector3 center = _perceivedPlayerPosition;
            if (Random.value < _leadPlayerChance)
            {
                // Right after a jump the boss does not know where the player is heading
                Vector3 velocity = _jumpReactionTimer > 0f ? Vector3.zero : playerNavMeshAgent.velocity;
                velocity.y = 0f;
                center += velocity * _circleTelegraphTime + GetRandomFlatOffset(0f, _leadOffsetDistance);
            }
            else
                center += GetRandomFlatOffset(_randomOffsetDistanceRange.x, _randomOffsetDistanceRange.y);

            return NavMesh.SamplePosition(center, out NavMeshHit hit, 3f, NavMesh.AllAreas) ? hit.position : _perceivedPlayerPosition;
        }

        private static Vector3 GetRandomFlatOffset(float minDistance, float maxDistance)
        {
            Vector2 direction = Random.insideUnitCircle.normalized;
            float distance = Random.Range(minDistance, maxDistance);
            return new Vector3(direction.x, 0f, direction.y) * distance;
        }

        /// *** Diagonal Lines *** ///

        // Casts the lines, then teleports while they charge; the next attack waits for the explosion
        private IEnumerator DiagonalLinesSequence()
        {
            anim.SetTrigger(Cast_Area);
            yield return WaitFacingPlayer(_lineWindUpTime);

            float blastTime = Time.time + _lineBlast.telegraphTime;
            SpawnLineBlast();

            yield return WaitFacingPlayer(_lineTeleportDelay);
            yield return TeleportRoutine();

            yield return WaitFacingPlayer(Mathf.Max(0f, blastTime - Time.time) + _lineAfterBlastTime);
            isPerformingAttack = false;
        }

        private void SpawnLineBlast()
        {
            DiagonalLineBlast blast = Instantiate(_lineBlastPrefab);
            blast.Begin(_arenaRect, transform.position.y, _lineBlast);
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

        /// *** Teleport *** ///

        private IEnumerator TeleportThenFinishAttack()
        {
            yield return TeleportRoutine();
            isPerformingAttack = false;
        }

        private IEnumerator TeleportRoutine()
        {
            isImmuneToDamage = true;
            anim.CrossFade(Idle, 0.1f);
            anim.CrossFade(Empty, 0.1f, CastHandLayer);
            yield return ScaleOverTime(_originalScale, GetSkinnyScale(), _teleportVanishTime);

            navMeshAgent.Warp(FindTeleportPosition());
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

                return hit.position;
            }

            return transform.position;
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
            Vector3 direction = _perceivedPlayerPosition - transform.position;
            direction.y = 0f;
            if (direction == Vector3.zero)
                return;

            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), Mathf.Clamp01(deltaTime * rotationSpeed));
        }
    }
}
