using System.Collections;
using Interfaces;
using Manager.GameManager;
using Shared;
using UI;
using UnityEngine;

namespace Bosses.Ulvane
{
    // Antler Gore: from far away the deer faces the player and a huge cone fills in front of it, then it lunges and hits everything in the cone; four times in a row, aiming again each time. The player can counter it while a cone fills (it shows the counter key) and the deer is stunned
    public class DeerBeast : UlvaneBeast, ICounterable
    {
        private const string CounterableTag = "Counterable";

        [Header("Antler Gore")]
        [SerializeField] private int _goreCount = 4;
        [SerializeField] private float _approachRange = 12f;
        [SerializeField] private float _approachSpeed = 10f;
        [SerializeField] private float _coneRadius = 15f;
        [SerializeField] private float _coneAngle = 50f;
        [SerializeField] private float _telegraphTime = 1.5f;
        [SerializeField] private float _aimTurnTime = 0.2f;
        [SerializeField] private float _lungeTime = 0.3f;
        [SerializeField] private float _lungeDistance = 1.5f;
        [SerializeField] private float _playerHitPadding = 0.3f;
        [SerializeField] private float _recoverTime = 0.3f;
        [SerializeField] private int _damage = 1;

        [Header("Counter")]
        [SerializeField] private float _counterTargetRadius = 1.4f;
        [SerializeField] private float _counterTargetHeight = 1.5f;
        [SerializeField] private float _stunTime = 3f;
        [SerializeField] private float _stunWobbleAngle = 8f;
        [SerializeField] private float _stunWobbleSpeed = 14f;

        [Header("Antler Lunge")]
        [SerializeField] private float _headDownAngle = 88f;
        [SerializeField] private float _headUpAngle = 35f;
        [SerializeField] private float _headUpTime = 0.4f;
        [SerializeField] private float _sandWaveTime = 0.25f;
        [SerializeField] private float _sandStartDistance = 1.5f;
        [SerializeField] private float _sandRowSpacing = 2.2f;
        [SerializeField] private float _sandPuffSpacing = 3.2f;
        [SerializeField] private float _sandPuffScale = 1.5f;
        [SerializeField] private int _sandParticleCount = 12;
        [SerializeField] private float _sandLifetime = 0.9f;
        [SerializeField] private float _sandSpeedScale = 0.5f;
        [SerializeField] private Color _sandColor = new Color(0.62f, 0.5f, 0.34f, 1f);
        [SerializeField] private string[] _neckBoneNames = { "spine.006", "spine.007", "spine.008", "scull" };

        private bool _isCountered;
        private bool _isCounterWindowOpen;
        private Transform[] _neckBones;
        private float _headTilt;
        private float _headFrom;
        private float _headTo;
        private float _headProgress = 1f;
        private float _headDuration = 1f;

        public override int HitDamage => _damage;

        // After the Animator has posed the deer, the neck bends: antlers to the floor while the cone fills (tilt 1), then scooping up through the lunge (tilt -1); every move eases in and out
        private void LateUpdate()
        {
            _headProgress = Mathf.MoveTowards(_headProgress, 1f, Time.deltaTime / _headDuration);
            _headTilt = Mathf.Lerp(_headFrom, _headTo, Mathf.SmoothStep(0f, 1f, _headProgress));
            if (Mathf.Approximately(_headTilt, 0f) || _neckBones == null)
                return;

            float angle = _headTilt > 0f ? _headDownAngle * _headTilt : _headUpAngle * _headTilt;
            Quaternion bend = Quaternion.AngleAxis(angle / _neckBones.Length, transform.right);
            foreach (Transform bone in _neckBones)
                bone.rotation = bend * bone.rotation;
        }

        protected override IEnumerator Approach() => ApproachPlayer(_approachRange, _approachSpeed);

        public void TriggerCounter()
        {
            if (_isCounterWindowOpen)
                _isCountered = true;
        }

        protected override IEnumerator Perform()
        {
            _isCountered = false;
            FindNeckBones();
            for (int i = 0; i < _goreCount; i++)
            {
                if (i > 0)
                    yield return ApproachPlayer(_approachRange, _approachSpeed);

                // A quick turn to the perceived player, then the direction is locked for the whole telegraph
                Idle();
                yield return FaceFor(FlatDirectionTo(owner.PlayerAimPoint), _aimTurnTime);
                Vector3 origin = transform.position;
                Vector3 direction = transform.forward;
                direction.y = 0f;
                direction.Normalize();
                SkillIndicator cone = Instantiate(owner.IndicatorPrefab);
                cone.ShowCone(origin, direction, _coneRadius, _coneAngle, _telegraphTime);

                GameObject counterTarget = OpenCounterWindow(out TargetMarker prompt);
                // The neck lowers smoothly over the whole time the cone fills and the antlers reach the floor exactly when it is full; only then comes the lunge and the hit
                MoveHead(1f, _telegraphTime);
                for (float elapsed = 0f; elapsed < _telegraphTime && !_isCountered; elapsed += Time.deltaTime)
                {
                    FaceDirection(direction, Time.deltaTime);
                    yield return null;
                }

                CloseCounterWindow(counterTarget, prompt);

                if (_isCountered)
                {
                    cone.Hide();
                    MoveHead(0f, _headUpTime);
                    yield return Stun();
                    yield break;
                }

                MoveHead(-1f, _lungeTime);
                StartCoroutine(SandWave(origin, direction));
                Gallop();
                float speed = _lungeDistance / Mathf.Max(_lungeTime, 0.01f);
                for (float elapsed = 0f; elapsed < _lungeTime; elapsed += Time.deltaTime)
                {
                    MoveFlat(direction * (speed * Time.deltaTime));
                    yield return null;
                }

                Idle();
                Strike(origin, direction);
                yield return new WaitForSeconds(_recoverTime);
                MoveHead(0f, _headUpTime);
            }
        }

        // A fast wave of sand sweeps across the whole cone, row by row from the deer outwards
        private IEnumerator SandWave(Vector3 origin, Vector3 direction)
        {
            int rows = Mathf.Max(1, Mathf.FloorToInt((_coneRadius - _sandStartDistance) / _sandRowSpacing) + 1);
            float rowDelay = _sandWaveTime / rows;
            for (int row = 0; row < rows; row++)
            {
                float distance = _sandStartDistance + row * _sandRowSpacing;
                float width = 2f * distance * Mathf.Tan(_coneAngle * 0.5f * Mathf.Deg2Rad);
                int count = Mathf.Max(1, Mathf.RoundToInt(width / _sandPuffSpacing));
                for (int i = 0; i < count; i++)
                {
                    float angle = count > 1 ? (i / (float)(count - 1) - 0.5f) * _coneAngle * 0.9f : 0f;
                    Vector3 point = origin + Quaternion.Euler(0f, angle, 0f) * direction * distance;
                    point.y = origin.y + 0.1f;
                    SpawnSand(point);
                }

                yield return new WaitForSeconds(rowDelay);
            }
        }

        // A small low puff of the ninja smoke, tinted like sand and played once
        private void SpawnSand(Vector3 position)
        {
            if (!owner.ImpactEffectPrefab)
                return;

            GameObject sand = Instantiate(owner.ImpactEffectPrefab, position, Quaternion.identity);
            sand.transform.localScale *= _sandPuffScale;
            foreach (ParticleSystem particles in sand.GetComponentsInChildren<ParticleSystem>())
            {
                ParticleSystem.MainModule main = particles.main;
                main.loop = false;
                main.startColor = _sandColor;
                main.startLifetime = _sandLifetime;
                main.startSpeedMultiplier *= _sandSpeedScale;
                ParticleSystem.EmissionModule emission = particles.emission;
                if (emission.burstCount > 0)
                {
                    ParticleSystem.Burst burst = emission.GetBurst(0);
                    burst.count = _sandParticleCount;
                    emission.SetBurst(0, burst);
                }
            }

            Destroy(sand, _sandLifetime + 1f);
        }

        private void FindNeckBones()
        {
            if (_neckBones != null)
                return;

            System.Collections.Generic.List<Transform> bones = new System.Collections.Generic.List<Transform>();
            Transform[] all = GetComponentsInChildren<Transform>();
            foreach (string boneName in _neckBoneNames)
                foreach (Transform candidate in all)
                    if (candidate.name == boneName)
                    {
                        bones.Add(candidate);
                        break;
                    }

            _neckBones = bones.Count > 0 ? bones.ToArray() : null;
        }

        private void MoveHead(float target, float duration)
        {
            _headFrom = _headTilt;
            _headTo = target;
            _headProgress = 0f;
            _headDuration = Mathf.Max(duration, 0.01f);
        }

        private void Strike(Vector3 origin, Vector3 direction)
        {
            Vector3 toPlayer = GameManager.Instance.player.transform.position - origin;
            toPlayer.y = 0f;
            float reach = _coneRadius + _playerHitPadding;
            if (toPlayer.sqrMagnitude > reach * reach)
                return;

            if (Vector3.Angle(direction, toPlayer) <= _coneAngle * 0.5f)
                DamagePlayer(_damage);
        }

        // A trigger on the Ignore Raycast layer that the kick can find, so the player's clicks never hit it; the prompt floats above it
        private GameObject OpenCounterWindow(out TargetMarker prompt)
        {
            GameObject target = new GameObject("Counter Target");
            target.layer = 2;
            target.tag = CounterableTag;
            target.transform.SetParent(transform, false);
            float scale = Mathf.Max(transform.lossyScale.y, 0.01f);
            SphereCollider sphere = target.AddComponent<SphereCollider>();
            sphere.isTrigger = true;
            sphere.radius = _counterTargetRadius / scale;
            sphere.center = Vector3.up * (_counterTargetHeight / scale);
            prompt = CounterPrompt.Show(sphere);
            _isCounterWindowOpen = true;
            ShowCounterTint();
            return target;
        }

        private void CloseCounterWindow(GameObject target, TargetMarker prompt)
        {
            _isCounterWindowOpen = false;
            HideCounterTint();
            if (prompt)
                prompt.Hide();

            if (target)
                Destroy(target);
        }

        // The deer shakes its head dizzily where it stands
        private IEnumerator Stun()
        {
            Idle();
            if (owner.ImpactEffectPrefab)
                Destroy(Instantiate(owner.ImpactEffectPrefab, transform.position + Vector3.up * _counterTargetHeight, Quaternion.identity), 3f);

            float yaw = transform.eulerAngles.y;
            for (float elapsed = 0f; elapsed < _stunTime; elapsed += Time.deltaTime)
            {
                float wobble = Mathf.Sin(elapsed * _stunWobbleSpeed) * _stunWobbleAngle;
                transform.rotation = Quaternion.Euler(0f, yaw, wobble);
                yield return null;
            }

            transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        }
    }
}
