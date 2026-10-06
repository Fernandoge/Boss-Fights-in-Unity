using System.Collections;
using UnityEngine;
using UnityEngine.AI;

namespace Bosses.Second_Boss
{
    public class SecondBoss : BossController
    {
        [Header("Spell Circles")]
        [SerializeField] private SpellCircle _spellCirclePrefab;
        [SerializeField] private int _circleCount = 6;
        [SerializeField] private float _circleRadius = 1.2f;
        [SerializeField] private float _castReleaseDelay = 0.35f;
        [SerializeField] private float _circleInterval = 0.55f;
        [SerializeField] private float _circleTelegraphTime = 0.6f;
        [SerializeField] private float _afterCastTime = 0.4f;
        [SerializeField] private int _circleDamage = 1;
        [SerializeField, Range(0f, 1f)] private float _leadPlayerChance = 0.5f;
        [SerializeField] private float _leadOffsetDistance = 0.8f;
        [SerializeField] private Vector2 _randomOffsetDistanceRange = new Vector2(0.5f, 2.5f);

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

        private static readonly int Cast = Animator.StringToHash("Cast");
        private static readonly int Idle = Animator.StringToHash("Idle");
        private static readonly int Empty = Animator.StringToHash("Empty");

        private const int CastHandLayer = 1;

        protected override void Start()
        {
            base.Start();
            _originalScale = transform.localScale;
        }

        /// *** Debug *** ///

        public void DebugForceSpellCircles()
        {
            if (!DebugIsBusy)
                PerformAttack();
        }

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
            StartCoroutine(SpellCirclesSequence());
        }

        // No second phase yet; the base version would freeze the boss without a transition animation
        protected override IEnumerator EnterSecondPhase()
        {
            yield break;
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
            Vector3 center = player.position;
            if (Random.value < _leadPlayerChance)
            {
                Vector3 velocity = playerNavMeshAgent.velocity;
                velocity.y = 0f;
                center += velocity * _circleTelegraphTime + GetRandomFlatOffset(0f, _leadOffsetDistance);
            }
            else
                center += GetRandomFlatOffset(_randomOffsetDistanceRange.x, _randomOffsetDistanceRange.y);

            return NavMesh.SamplePosition(center, out NavMeshHit hit, 3f, NavMesh.AllAreas) ? hit.position : player.position;
        }

        private static Vector3 GetRandomFlatOffset(float minDistance, float maxDistance)
        {
            Vector2 direction = Random.insideUnitCircle.normalized;
            float distance = Random.Range(minDistance, maxDistance);
            return new Vector3(direction.x, 0f, direction.y) * distance;
        }

        /// *** Teleport *** ///

        private IEnumerator TeleportThenFinishAttack()
        {
            isImmuneToDamage = true;
            anim.CrossFade(Idle, 0.1f);
            anim.CrossFade(Empty, 0.1f, CastHandLayer);
            yield return ScaleOverTime(_originalScale, GetSkinnyScale(), _teleportVanishTime);

            navMeshAgent.Warp(FindTeleportPosition());
            FacePlayer(1000f);

            yield return ScaleOverTime(GetSkinnyScale(), _originalScale, _teleportAppearTime);
            isImmuneToDamage = false;
            isPerformingAttack = false;
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
            Vector3 direction = player.position - transform.position;
            direction.y = 0f;
            if (direction == Vector3.zero)
                return;

            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), Mathf.Clamp01(deltaTime * rotationSpeed));
        }
    }
}
