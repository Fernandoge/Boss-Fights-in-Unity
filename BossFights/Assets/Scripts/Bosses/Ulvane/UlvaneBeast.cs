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
    // An animal that patrols the arena for the whole fight and runs its one skill when Ulvane's turn order sends it (Command), then goes back to patrolling. A command never interrupts a skill that is already running.
    public abstract class UlvaneBeast : MonoBehaviour
    {
        [Header("Beast")]
        [SerializeField] private float _turnSpeed = 720f;

        [Header("Patrol")]
        [SerializeField] private UlvanePatrolArea _patrolArea = UlvanePatrolArea.Anywhere;
        [SerializeField] private float _patrolSpeed = 2.5f;
        [SerializeField] private float _returnSpeed = 7f;
        [SerializeField] private float _patrolTurnSpeed = 180f;
        [SerializeField] private Vector2 _patrolPauseTime = new Vector2(1.5f, 4f);
        [SerializeField] private float _patrolPointTimeout = 15f;

        [Header("Enraged")]
        [SerializeField] private float _enragedAnimationSpeed = 1.3f;
        [SerializeField] private Color _enragedTint = new Color(1f, 0.2f, 0.15f);
        [SerializeField, Range(0f, 1f)] private float _enragedTintAmount = 0.5f;

        [Header("Mourning")]
        [SerializeField] private float _mournRunSpeed = 9f;

        protected UlvaneBoss owner;

        private Animator _anim;
        private UlvanePatrolZone _zone;
        private bool _isCommanded;
        private bool _isComboHeld;
        private SphereCollider _heightTarget;
        private TargetMarker _dangerIcon;
        private bool _isEnraged;
        private bool _isMourningRequested;
        private NavMeshPath _mournPath;
        private readonly List<TintedMaterial> _tintedMaterials = new List<TintedMaterial>();
        private readonly List<TintedMaterial> _counterTintedMaterials = new List<TintedMaterial>();
        private Vector3 _mournCenter;
        private float _mournRadius;
        private float _mournAngle;
        private float _groundY;
        private float _targetVert;
        private float _targetState;

        private static readonly int Vert = Animator.StringToHash("Vert");
        private static readonly int State = Animator.StringToHash("State");

        public bool IsFinished { get; private set; } = true;

        public float GroundY => _groundY;

        // Free to start a skill: not on its way to one and not in the middle of one
        public bool IsAvailable => !_isCommanded;

        public UlvanePatrolArea PatrolArea => _patrolArea;

        // Standing on its place around the body, circling and howling
        public bool HasArrivedAtMourning { get; private set; }

        // The most hearts this skill takes, so the boss can show the danger icon
        public abstract int HitDamage { get; }

        private void Update()
        {
            if (!_anim)
                return;

            _anim.SetFloat(Vert, _targetVert, 0.1f, Time.deltaTime);
            _anim.SetFloat(State, _targetState, 0.1f, Time.deltaTime);
        }

        public void Spawn(UlvaneBoss summoner, Vector3 position, UlvanePatrolZone zone)
        {
            owner = summoner;
            _zone = zone;
            _anim = GetComponent<Animator>();
            Vector3 spawnPosition = NavMeshUtility.Snap(position);
            _groundY = spawnPosition.y;
            transform.SetPositionAndRotation(spawnPosition, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f));
            StartCoroutine(Life());
        }

        // Interrupts the patrol: the beast runs its skill and sets IsFinished when it is patrolling again
        public void Command()
        {
            _isCommanded = true;
            IsFinished = false;
        }

        // A combo: the boss drives this beast step by step (through the public Combo methods) instead of the beast running its own skill; EndCombo gives it back
        public void BeginCombo()
        {
            Command();
            _isComboHeld = true;
        }

        public void EndCombo() => _isComboHeld = false;

        public IEnumerator ComboRunTo(Vector3 point, float speed) => RunTo(point, speed);

        public IEnumerator ComboFaceFor(Vector3 direction, float duration) => FaceFor(direction, duration);

        // A combo can hide the animal in the sky or leave it posed, so the beast is put back on the floor when the combo is released
        public void SetVisible(bool isVisible)
        {
            foreach (Renderer meshRenderer in GetComponentsInChildren<Renderer>())
                meshRenderer.enabled = isVisible;
        }

        // Flung up and out of the screen, tumbling; it is hidden when it gets there
        public IEnumerator ComboFlyUp(float height, float duration, float spinSpeed)
        {
            Idle();
            Vector3 start = transform.position;
            for (float elapsed = 0f; elapsed < duration; elapsed += Time.deltaTime)
            {
                float progress = elapsed / duration;
                transform.position = new Vector3(start.x, _groundY + height * progress * progress, start.z);
                transform.Rotate(spinSpeed * Time.deltaTime, 0f, 0f, Space.Self);
                yield return null;
            }

            SetVisible(false);
        }

        // Comes down from the sky onto the landing point facing a direction: exponent 1 is a steady glide, 2 or more speeds up like a fall; sway rolls it from side to side, pitch tilts the nose down
        public IEnumerator ComboFallDown(Vector3 landing, Vector3 facing, float startHeight, float duration, float exponent, float sway, float pitch)
        {
            facing.y = 0f;
            Quaternion yaw = Quaternion.LookRotation(facing.sqrMagnitude > 0.0001f ? facing.normalized : transform.forward);
            SetVisible(true);
            for (float elapsed = 0f; elapsed < duration; elapsed += Time.deltaTime)
            {
                float progress = elapsed / duration;
                float height = startHeight * (1f - Mathf.Pow(progress, exponent));
                float roll = Mathf.Sin(elapsed * 14f) * sway * (1f - progress);
                transform.SetPositionAndRotation(new Vector3(landing.x, _groundY + height, landing.z), yaw * Quaternion.Euler(pitch * (1f - progress), 0f, roll));
                yield return null;
            }

            transform.SetPositionAndRotation(new Vector3(landing.x, _groundY, landing.z), yaw);
        }

        // The last stand: the animal plays faster and turns red
        public void Enrage()
        {
            if (_isEnraged)
                return;

            _isEnraged = true;
            _isComboHeld = false;
            if (_anim)
                _anim.speed = _enragedAnimationSpeed;

            ApplyTint(_tintedMaterials, _enragedTint, _enragedTintAmount);
        }

        // After the skill it is in, the animal runs to its place on a circle around the body and circles it, howling, until the end of the fight
        public void Mourn(Vector3 center, float radius, float angleDegrees)
        {
            _mournCenter = center;
            _mournRadius = radius;
            _mournAngle = angleDegrees * Mathf.Deg2Rad;
            _isMourningRequested = true;
        }

        protected abstract IEnumerator Perform();

        // Runs when a combo ends or is cut short, to remove what the beast built for it
        protected virtual void OnComboReleased() { }

        // Runs before the skill, to get to the place the skill starts from
        protected virtual IEnumerator Approach() { yield break; }

        /// *** Animation *** ///

        protected void Idle() => _targetVert = 0f;

        protected void Gallop()
        {
            _targetVert = 1f;
            _targetState = 1f;
        }

        private void Walk()
        {
            _targetVert = 1f;
            _targetState = 0f;
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

        protected IEnumerator RunTo(Vector3 point, float speed, float timeout = 6f)
        {
            for (float elapsed = 0f; elapsed < timeout && FlatDistanceTo(point) > 0.6f; elapsed += Time.deltaTime)
            {
                // Every frame: a combo starts this while the patrol step is still ending, and its last Idle() would leave the legs still
                Gallop();
                Vector3 direction = FlatDirectionTo(point);
                FaceDirection(direction, Time.deltaTime);
                MoveFlat(direction * (speed * Time.deltaTime));
                yield return null;
            }

            Idle();
        }

        protected IEnumerator ApproachPlayer(float range, float speed, float timeout = 6f)
        {
            Gallop();
            for (float elapsed = 0f; elapsed < timeout && !IsPlayerNear(range); elapsed += Time.deltaTime)
            {
                Vector3 direction = FlatDirectionTo(owner.PlayerAimPoint);
                FaceDirection(direction, Time.deltaTime);
                MoveFlat(direction * (speed * Time.deltaTime));
                yield return null;
            }

            Idle();
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

        protected float FlatDistanceTo(Vector3 point)
        {
            Vector3 offset = point - transform.position;
            offset.y = 0f;
            return offset.magnitude;
        }

        protected Vector3 FlatDirectionTo(Vector3 point)
        {
            Vector3 direction = point - transform.position;
            direction.y = 0f;
            return direction.sqrMagnitude > 0.0001f ? direction.normalized : transform.forward;
        }

        /// *** Telegraphs *** ///

        protected void ShowLine(Vector3 origin, Vector3 direction, float length, float width, float duration) =>
            Instantiate(owner.IndicatorPrefab).ShowLine(origin, direction, length, width, duration);

        protected SkillIndicator ShowCircle(Vector3 center, float radius, float duration, bool hideWhenFilled = true)
        {
            SkillIndicator indicator = Instantiate(owner.IndicatorPrefab);
            indicator.ShowCircle(center, radius, duration, hideWhenFilled);
            return indicator;
        }

        protected GroundBlast SpawnBlast(Vector3 center, float radius, float telegraphTime, int damage, GameObject visualPrefab = null, float visualFallHeight = 0f) =>
            GroundBlast.Create(owner.IndicatorPrefab, center, radius, telegraphTime, damage, owner.ImpactEffectPrefab, visualPrefab, visualFallHeight);

        /// *** Life *** ///

        private IEnumerator Life()
        {
            while (true)
            {
                if (_isCommanded)
                {
                    if (_isComboHeld)
                    {
                        while (_isComboHeld && !_isMourningRequested)
                            yield return null;

                        HideDanger();
                        OnComboReleased();
                        PutBackOnFloor();
                    }
                    else
                    {
                        yield return Approach();
                        yield return Perform();
                    }

                    Idle();
                    _isCommanded = false;
                    IsFinished = true;
                }
                else if (_isMourningRequested)
                    yield return MournRoutine();
                else
                    yield return PatrolStep();
            }
        }

        private Vector3 MournPoint(float angle) =>
            NavMeshUtility.Snap(_mournCenter + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * _mournRadius);

        // Runs to its place in the circle, turns to face the body and lies down for good; the boss ends the fight
        private IEnumerator MournRoutine()
        {
            RestoreTint();
            if (_mournPath == null)
                _mournPath = new NavMeshPath();

            Vector3 slot = MournPoint(_mournAngle);
            Gallop();

            // Follows the NavMesh path round the trees, looking for a new one twice a second
            float repathTimer = 0f;
            for (float elapsed = 0f; elapsed < 14f && FlatDistanceTo(slot) > 0.8f; elapsed += Time.deltaTime)
            {
                repathTimer -= Time.deltaTime;
                if (repathTimer <= 0f)
                {
                    NavMesh.CalculatePath(transform.position, slot, NavMesh.AllAreas, _mournPath);
                    repathTimer = 0.5f;
                }

                Vector3 waypoint = _mournPath.corners.Length > 1 ? _mournPath.corners[1] : slot;
                Vector3 direction = FlatDirectionTo(waypoint);
                FaceDirection(direction, Time.deltaTime);
                MoveFlat(direction * (_mournRunSpeed * Time.deltaTime));
                yield return null;
            }

            Idle();
            yield return FaceFor(FlatDirectionTo(_mournCenter), 0.4f);
            HasArrivedAtMourning = true;
            ShowSadFace();

            while (true)
                yield return null;
        }

        // A trigger on the Ignore Raycast layer just as tall as the animal, for the sad face and the danger icon to float above
        protected SphereCollider GetHeightTarget()
        {
            if (_heightTarget)
                return _heightTarget;

            Bounds bounds = new Bounds(transform.position, Vector3.zero);
            foreach (SkinnedMeshRenderer skinnedRenderer in GetComponentsInChildren<SkinnedMeshRenderer>())
                bounds.Encapsulate(skinnedRenderer.bounds);

            GameObject target = new GameObject("Height Target");
            target.layer = 2;
            target.transform.SetParent(transform, false);
            float scale = Mathf.Max(transform.lossyScale.y, 0.01f);
            _heightTarget = target.AddComponent<SphereCollider>();
            _heightTarget.isTrigger = true;
            _heightTarget.radius = Mathf.Max(bounds.extents.y, 0.1f) / scale;
            _heightTarget.center = transform.InverseTransformPoint(bounds.center);
            return _heightTarget;
        }

        private void ShowSadFace() => SadFace.Show(GetHeightTarget());

        // The warning sign above this animal while it takes part in a combo that hurts for two hearts
        public void ShowDanger()
        {
            if (!_dangerIcon)
                _dangerIcon = DangerIcon.Show(GetHeightTarget());
        }

        // Also used while a combo step needs the sign gone for a moment (the penguin's question mark)
        public void HideDanger()
        {
            if (!_dangerIcon)
                return;

            _dangerIcon.Hide();
            _dangerIcon = null;
        }

        // A combo that was cut short (the last stand) can leave the animal in the sky, tilted or invisible
        private void PutBackOnFloor()
        {
            SetVisible(true);
            Vector3 position = NavMeshUtility.Snap(new Vector3(transform.position.x, _groundY, transform.position.z));
            transform.SetPositionAndRotation(new Vector3(position.x, _groundY, position.z), Quaternion.Euler(0f, transform.eulerAngles.y, 0f));
        }

        // The same green the first boss turns while its counter window is open
        protected void ShowCounterTint() => ApplyTint(_counterTintedMaterials, Color.green, 1f);

        protected void HideCounterTint() => ClearTint(_counterTintedMaterials);

        private void ApplyTint(List<TintedMaterial> store, Color tint, float amount)
        {
            ClearTint(store);
            foreach (Renderer meshRenderer in GetComponentsInChildren<Renderer>())
            {
                if (!(meshRenderer is SkinnedMeshRenderer) && !(meshRenderer is MeshRenderer))
                    continue;

                foreach (Material material in meshRenderer.materials)
                {
                    string colorName = material.HasProperty("_BaseColor") ? "_BaseColor" : material.HasProperty("_Color") ? "_Color" : null;
                    if (colorName == null)
                        continue;

                    Color original = material.GetColor(colorName);
                    store.Add(new TintedMaterial { material = material, colorName = colorName, original = original });
                    material.SetColor(colorName, Color.Lerp(original, tint, amount));
                }
            }
        }

        private static void ClearTint(List<TintedMaterial> store)
        {
            foreach (TintedMaterial tinted in store)
                if (tinted.material)
                    tinted.material.SetColor(tinted.colorName, tinted.original);

            store.Clear();
        }

        // Calm again: the animals are grieving, not angry
        private void RestoreTint() => ClearTint(_tintedMaterials);

        // After a skill the animal runs back to its own patrol place before it strolls again
        private IEnumerator ReturnToZone()
        {
            if (!_zone || _zone.Contains(transform.position))
                yield break;

            Vector3 target = owner.GetPatrolPoint(_zone, _patrolArea);
            Gallop();
            for (float elapsed = 0f; !_isCommanded && elapsed < _patrolPointTimeout && !_zone.Contains(transform.position, -1f); elapsed += Time.deltaTime)
            {
                Vector3 direction = FlatDirectionTo(target);
                FaceDirection(direction, Time.deltaTime);
                MoveFlat(direction * (_returnSpeed * Time.deltaTime));
                yield return null;
            }

            Idle();
        }

        // Walks to a random point, then stands for a while; both stop the moment the beast is commanded or its cooldown is over
        private IEnumerator PatrolStep()
        {
            yield return ReturnToZone();
            Vector3 point = owner.GetPatrolPoint(_zone, _patrolArea, 0f, transform.position);
            Walk();
            for (float elapsed = 0f; !_isCommanded && elapsed < _patrolPointTimeout && FlatDistanceTo(point) > 0.8f; elapsed += Time.deltaTime)
            {
                Vector3 direction = FlatDirectionTo(point);
                TurnTowards(_patrolTurnSpeed, direction, Time.deltaTime);
                float alignment = Mathf.Clamp01(Vector3.Dot(transform.forward, direction));
                MoveFlat(transform.forward * (_patrolSpeed * alignment * Time.deltaTime));
                yield return null;
            }

            Idle();
            float pause = Random.Range(_patrolPauseTime.x, _patrolPauseTime.y);
            for (float elapsed = 0f; !_isCommanded && elapsed < pause; elapsed += Time.deltaTime)
                yield return null;
        }

        /// *** Nested Types *** ///

        private struct TintedMaterial
        {
            public Material material;
            public string colorName;
            public Color original;
        }
    }
}
