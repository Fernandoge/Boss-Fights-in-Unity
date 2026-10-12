using System.Collections;
using Manager.GameManager;
using Shared;
using UnityEngine;

namespace Bosses.Ulvane
{
    // An animal Ulvane calls: it appears, performs its one skill, then leaves. It lives on its own, so the boss only waits for IsFinished.
    public abstract class UlvaneBeast : MonoBehaviour
    {
        [Header("Beast")]
        [SerializeField] private float _appearTime = 0.3f;
        [SerializeField] private float _leaveTime = 0.35f;
        [SerializeField] private float _turnSpeed = 720f;

        protected UlvaneBoss owner;

        private Animator _anim;
        private Vector3 _originalScale;
        private float _groundY;
        private float _targetVert;
        private float _targetState;

        private static readonly int Vert = Animator.StringToHash("Vert");
        private static readonly int State = Animator.StringToHash("State");

        public bool IsFinished { get; private set; }

        // The most hearts this skill takes, so the boss can show the danger icon
        public abstract int HitDamage { get; }

        private void Update()
        {
            if (!_anim)
                return;

            _anim.SetFloat(Vert, _targetVert, 0.1f, Time.deltaTime);
            _anim.SetFloat(State, _targetState, 0.1f, Time.deltaTime);
        }

        public void Summon(UlvaneBoss summoner)
        {
            owner = summoner;
            _anim = GetComponent<Animator>();
            _originalScale = transform.localScale;
            Vector3 spawnPosition = NavMeshUtility.Snap(ChooseSpawnPosition());
            _groundY = spawnPosition.y;
            transform.position = spawnPosition;
            transform.localScale = Vector3.zero;
            StartCoroutine(Life());
        }

        protected abstract IEnumerator Perform();

        protected virtual Vector3 ChooseSpawnPosition() => owner.BeastSpawnPoint;

        /// *** Animation *** ///

        protected void Idle() => _targetVert = 0f;

        protected void Gallop()
        {
            _targetVert = 1f;
            _targetState = 1f;
        }

        /// *** Movement *** ///

        protected void FaceDirection(Vector3 direction, float deltaTime) => TurnTowards(_turnSpeed, direction, deltaTime);

        protected void TurnTowards(float degreesPerSecond, Vector3 direction, float deltaTime)
        {
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.0001f)
                return;

            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(direction), degreesPerSecond * deltaTime);
        }

        // Moves over the floor and stops at trees and the edge of the arena
        protected void MoveFlat(Vector3 delta)
        {
            Vector3 allowed = NavMeshUtility.ClampLine(transform.position, transform.position + delta);
            allowed.y = _groundY;
            transform.position = allowed;
        }

        protected IEnumerator FaceFor(Vector3 direction, float duration)
        {
            for (float elapsed = 0f; elapsed < duration; elapsed += Time.deltaTime)
            {
                FaceDirection(direction, Time.deltaTime);
                yield return null;
            }
        }

        // Runs along a line, hurting the player once if they are in the way
        protected IEnumerator DashAlongLine(Vector3 direction, float length, float speed, float hitRadius, int damage)
        {
            Gallop();
            bool hasHit = false;
            for (float travelled = 0f; travelled < length; travelled += speed * Time.deltaTime)
            {
                FaceDirection(direction, Time.deltaTime);
                MoveFlat(direction * (speed * Time.deltaTime));
                if (!hasHit && IsPlayerNear(hitRadius))
                {
                    hasHit = true;
                    DamagePlayer(damage);
                }

                yield return null;
            }

            Idle();
        }

        /// *** Player *** ///

        protected bool IsPlayerNear(float radius)
        {
            Vector3 toPlayer = GameManager.Instance.player.transform.position - transform.position;
            toPlayer.y = 0f;
            return toPlayer.sqrMagnitude <= radius * radius;
        }

        protected static void DamagePlayer(int damage) => GameManager.Instance.player.DamagePlayer(damage);

        protected Vector3 FlatDirectionTo(Vector3 point)
        {
            Vector3 direction = point - transform.position;
            direction.y = 0f;
            return direction.sqrMagnitude > 0.0001f ? direction.normalized : transform.forward;
        }

        /// *** Telegraphs *** ///

        protected void ShowLine(Vector3 origin, Vector3 direction, float length, float width, float duration) =>
            Instantiate(owner.IndicatorPrefab).ShowLine(origin, direction, length, width, duration);

        protected void ShowCircle(Vector3 center, float radius, float duration) =>
            Instantiate(owner.IndicatorPrefab).ShowCircle(center, radius, duration);

        protected GroundBlast SpawnBlast(Vector3 center, float radius, float telegraphTime, int damage, GameObject visualPrefab = null) =>
            GroundBlast.Create(owner.IndicatorPrefab, center, radius, telegraphTime, damage, owner.ImpactEffectPrefab, visualPrefab);

        /// *** Life *** ///

        private IEnumerator Life()
        {
            owner.SpawnPuff(transform.position);
            yield return ScaleTo(0f, 1f, _appearTime);
            yield return Perform();
            owner.SpawnPuff(transform.position);
            yield return ScaleTo(1f, 0f, _leaveTime);
            IsFinished = true;
            Destroy(gameObject);
        }

        private IEnumerator ScaleTo(float from, float to, float duration)
        {
            for (float elapsed = 0f; elapsed < duration; elapsed += Time.deltaTime)
            {
                transform.localScale = _originalScale * Mathf.Lerp(from, to, elapsed / duration);
                yield return null;
            }

            transform.localScale = _originalScale * to;
        }
    }
}
