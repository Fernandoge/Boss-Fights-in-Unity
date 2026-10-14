using System;
using System.Collections;
using Cameras;
using Characters;
using Manager.GameManager;
using Shared;
using UI;
using UnityEngine;

namespace Bosses.Ulvane
{
    // Pet Or Claw: the cat runs up and flops on its back with a key prompt above it. Press the key near it to pet it and it purrs away; ignore it and it swells into a giant that rakes the floor with three claws
    public class KittyBeast : UlvaneBeast
    {
        private const KeyCode PetKey = KeyCode.T;

        [Header("Pet")]
        [SerializeField] private GameObject _petEffectPrefab;
        [SerializeField] private float _approachRange = 5.5f;
        [SerializeField] private float _approachSpeed = 11f;
        [SerializeField] private float _flopTime = 0.35f;
        [SerializeField] private float _lieLift = 0.2f;
        [SerializeField] private float _petWindow = 3f;
        [SerializeField] private float _petRange = 7f;
        [SerializeField] private float _purrTime = 0.5f;

        [Header("Pet Moment")]
        [SerializeField] private float _petApproachSpeed = 12f;
        [SerializeField] private float _petHandHeight = 0.35f;
        [SerializeField] private float _petStrokeLength = 0.25f;
        [SerializeField] private float _petStrokeSpeed = 6.283f;
        [SerializeField] private float _petSlideTime = 0.4f;
        [SerializeField] private float _petMomentTime = 1.7f;
        [SerializeField] private float _petReachTimeout = 1.5f;
        [SerializeField] private float _petEffectStopEarly = 0.6f;
        [SerializeField] private float _petPoseBlendTime = 0.4f;
        [SerializeField] private float _petZoom = 2.2f;

        [Header("Giant Claw")]
        [SerializeField] private float _bigScale = 5f;
        [SerializeField] private float _getUpTime = 0.4f;
        [SerializeField] private float _growTime = 0.9f;
        [SerializeField] private float _telegraphTime = 0.8f;
        [SerializeField] private int _clawCount = 3;
        [SerializeField] private float _clawSpread = 24f;
        [SerializeField] private float _clawWidth = 1.7f;
        [SerializeField] private float _clawLength = 18f;
        [SerializeField] private float _hitPadding = 0.3f;
        [SerializeField] private float _rearAngle = 22f;
        [SerializeField] private float _slamAngle = 9f;
        [SerializeField] private float _slamTime = 0.1f;
        [SerializeField] private float _recoverTime = 0.8f;
        [SerializeField] private float _shrinkTime = 0.8f;
        [SerializeField] private float _effectSpacing = 5f;
        [SerializeField] private int _damage = 1;

        [Header("Hairball Combo")]
        [SerializeField] private int _heaveCount = 3;
        [SerializeField] private float _heaveBowTime = 0.3f;
        [SerializeField] private float _heaveJerkTime = 0.16f;
        [SerializeField] private float _heaveBowAngle = 16f;
        [SerializeField] private float _heaveJerkAngle = -14f;
        [SerializeField] private float _heavePuffScale = 1.18f;
        [SerializeField] private float _spitJerkScale = 1.5f;
        [SerializeField] private float _spitFlightTime = 0.6f;
        [SerializeField] private float _spitArcHeight = 2.5f;
        [SerializeField] private float _hairballScale = 2.2f;
        [SerializeField] private float _hairballRestHeight = 0.55f;
        [SerializeField] private float _mouthForward = 0.45f;
        [SerializeField] private float _mouthHeight = 0.4f;
        [SerializeField] private float _splatTime = 0.12f;

        private float _yaw;
        private float _roll;
        private float _pitch;
        private float _lift;
        private float _scaleFactor = 1f;
        private float _groundLevel;
        private Vector3 _baseScale;
        private bool _debugPetRequested;
        private bool _isFrozen;
        private float _previousTimeScale;

        public override int HitDamage => _damage;

        private void OnDestroy()
        {
            if (!_isFrozen)
                return;

            Time.timeScale = _previousTimeScale;
            if (GameManager.Instance && GameManager.Instance.player)
                GameManager.Instance.player.EndCutscene();
        }

        // For CLI tests, which cannot press keys
        public void DebugPet() => _debugPetRequested = true;

        protected override IEnumerator Approach() => ApproachPlayer(_approachRange, _approachSpeed);

        protected override void OnComboReleased()
        {
            if (_baseScale != Vector3.zero)
                ApplyPose(0f, 0f, 0f, 1f);
        }

        // Combo: the cat turns towards the landing point and retches a few times (bowing with puffed cheeks, then jerking up); on the last one it spits a big hairball that lands there. onLanded gets the ball, held still
        public IEnumerator ComboPuke(Vector3 landing, UlvaneProjectile hairballPrefab, Action<UlvaneProjectile> onLanded)
        {
            _baseScale = transform.localScale;
            _groundLevel = transform.position.y;
            _roll = 0f;
            _pitch = 0f;
            _lift = 0f;
            _scaleFactor = 1f;

            Idle();
            yield return FaceFor(FlatDirectionTo(landing), 0.3f);
            _yaw = transform.eulerAngles.y;

            Coroutine spit = null;
            for (int i = 0; i < _heaveCount; i++)
            {
                bool isLast = i == _heaveCount - 1;
                yield return MoveToPose(_heaveBowTime, 0f, _heaveBowAngle, 0f, _heavePuffScale);
                if (isLast)
                {
                    UlvaneProjectile hairball = Instantiate(hairballPrefab, GetMouthPosition(), Quaternion.identity);
                    hairball.Hold();
                    spit = StartCoroutine(Spit(hairball, landing, onLanded));
                }

                yield return MoveToPose(_heaveJerkTime, 0f, _heaveJerkAngle * (isLast ? _spitJerkScale : 1f), 0f, 1f);
            }

            yield return MoveToPose(0.3f, 0f, 0f, 0f, 1f);
            if (spit != null)
                yield return spit;
        }

        protected override IEnumerator Perform()
        {
            _debugPetRequested = false;
            _baseScale = transform.localScale;
            _groundLevel = transform.position.y;
            _roll = 0f;
            _pitch = 0f;
            _lift = 0f;
            _scaleFactor = 1f;

            Idle();
            yield return FaceFor(FlatDirectionTo(owner.PlayerAimPoint), 0.3f);
            _yaw = transform.eulerAngles.y;

            // Flops on its back and waits to be petted
            yield return MoveToPose(_flopTime, 90f, 0f, _lieLift, 1f);
            Transform petTarget = CreatePetTarget();
            TargetMarker prompt = CounterPrompt.Show(petTarget.GetComponent<Collider>(), PetKey);
            bool isPetted = false;
            for (float elapsed = 0f; elapsed < _petWindow && !isPetted; elapsed += Time.deltaTime)
            {
                isPetted = (Input.GetKeyDown(PetKey) || _debugPetRequested) && IsPlayerNear(_petRange);
                yield return null;
            }

            if (prompt)
                prompt.Hide();

            Destroy(petTarget.gameObject);

            if (isPetted)
            {
                yield return PetMoment();
                yield return new WaitForSeconds(_purrTime);
                yield return MoveToPose(_getUpTime, 0f, 0f, 0f, 1f);
                yield break;
            }

            // Nobody petted it: it gets up angry and swells
            yield return MoveToPose(_getUpTime, 0f, 0f, 0f, 1f);
            yield return MoveToPose(_growTime, 0f, 0f, 0f, _bigScale);
            yield return Claw();
            yield return new WaitForSeconds(_recoverTime);
            yield return MoveToPose(_shrinkTime, 0f, 0f, 0f, 1f);
        }

        // The game freezes and the camera zooms in: the cat slides over to the player, who crouches beside it, and hearts float up. Everything here runs on real time
        private IEnumerator PetMoment()
        {
            PlayerController player = GameManager.Instance.player;
            _previousTimeScale = Time.timeScale;
            _isFrozen = true;
            Time.timeScale = 0f;

            // The player walks to the cat, not the other way round: the cat stays where it lies and the player stops beside it
            Vector3 playerPosition = player.transform.position;
            Vector3 toPlayer = playerPosition - transform.position;
            toPlayer.y = 0f;
            Vector3 standPosition = toPlayer.sqrMagnitude > 0.0001f ? transform.position + toPlayer.normalized * player.PetMeetDistance : playerPosition;
            standPosition.y = playerPosition.y;
            float slideTime = Mathf.Max(_petSlideTime, Vector3.Distance(playerPosition, standPosition) / _petApproachSpeed);
            player.BeginCutscene(transform.position, _petPoseBlendTime, transform, _petHandHeight, _petStrokeLength, _petStrokeSpeed);
            _yaw = Quaternion.LookRotation(FlatDirectionTo(playerPosition)).eulerAngles.y;
            ApplyPose(_roll, _pitch, _lift, _scaleFactor);

            CameraZoomPunch zoom = Camera.main.GetComponent<CameraZoomPunch>();
            if (!zoom)
                zoom = Camera.main.gameObject.AddComponent<CameraZoomPunch>();

            zoom.Punch(_petZoom, 0.35f, slideTime + _petPoseBlendTime + _petMomentTime, 0.45f);

            for (float elapsed = 0f; elapsed < slideTime; elapsed += Time.unscaledDeltaTime)
            {
                player.SetCutscenePosition(Vector3.Lerp(playerPosition, standPosition, Mathf.SmoothStep(0f, 1f, elapsed / slideTime)));
                yield return null;
            }

            player.SetCutscenePosition(standPosition);

            // The hearts burst when the hand lands on the cat
            for (float waited = 0f; waited < _petReachTimeout && !player.IsPetHandReady; waited += Time.unscaledDeltaTime)
                yield return null;

            GameObject effect = null;
            if (_petEffectPrefab)
            {
                effect = Instantiate(_petEffectPrefab, transform.position + Vector3.up * 0.5f, Quaternion.identity);
                foreach (ParticleSystem particles in effect.GetComponentsInChildren<ParticleSystem>())
                {
                    ParticleSystem.MainModule main = particles.main;
                    main.useUnscaledTime = true;
                }
            }

            // The hearts stop a little before the pet ends, so the last ones fade while the player gets up, and whatever is left goes with the moment
            yield return new WaitForSecondsRealtime(Mathf.Max(0f, _petMomentTime - _petEffectStopEarly));
            if (effect)
                foreach (ParticleSystem particles in effect.GetComponentsInChildren<ParticleSystem>())
                    particles.Stop(false, ParticleSystemStopBehavior.StopEmitting);

            yield return new WaitForSecondsRealtime(Mathf.Min(_petMomentTime, _petEffectStopEarly));
            player.EndCutscene();
            if (effect)
                Destroy(effect);

            yield return new WaitForSecondsRealtime(_petPoseBlendTime);

            Time.timeScale = _previousTimeScale;
            _isFrozen = false;
        }

        private IEnumerator Claw()
        {
            Vector3 origin = transform.position;
            Vector3 baseDirection = FlatDirectionTo(owner.PlayerAimPoint);
            Vector3[] directions = new Vector3[_clawCount];
            float[] lengths = new float[_clawCount];
            for (int i = 0; i < _clawCount; i++)
            {
                float angle = _clawCount > 1 ? Mathf.Lerp(-_clawSpread, _clawSpread, i / (float)(_clawCount - 1)) : 0f;
                directions[i] = Quaternion.Euler(0f, angle, 0f) * baseDirection;
                lengths[i] = NavMeshUtility.DistanceToEdge(origin, directions[i], _clawLength);
                ShowLine(origin, directions[i], lengths[i], _clawWidth, _telegraphTime);
            }

            _yaw = Quaternion.LookRotation(baseDirection).eulerAngles.y;
            yield return MoveToPose(_telegraphTime, 0f, -_rearAngle, 0f, _bigScale);

            yield return MoveToPose(_slamTime, 0f, _slamAngle, 0f, _bigScale);
            bool hasHit = false;
            for (int i = 0; i < _clawCount; i++)
            {
                SpawnClawEffects(origin, directions[i], lengths[i]);
                if (!hasHit && IsPlayerInLane(origin, directions[i], lengths[i]))
                {
                    hasHit = true;
                    DamagePlayer(_damage);
                }
            }
        }

        private bool IsPlayerInLane(Vector3 origin, Vector3 direction, float length)
        {
            Vector3 toPlayer = GameManager.Instance.player.transform.position - origin;
            toPlayer.y = 0f;
            float along = Vector3.Dot(toPlayer, direction);
            float across = Mathf.Abs(direction.x * toPlayer.z - direction.z * toPlayer.x);
            return along >= 0f && along <= length && across <= _clawWidth * 0.5f + _hitPadding;
        }

        private void SpawnClawEffects(Vector3 origin, Vector3 direction, float length)
        {
            if (!owner.ImpactEffectPrefab)
                return;

            for (float distance = _effectSpacing; distance <= length; distance += _effectSpacing)
                Destroy(Instantiate(owner.ImpactEffectPrefab, origin + direction * distance, Quaternion.identity), 3f);
        }

        private Vector3 GetMouthPosition() => transform.position + transform.forward * (_mouthForward * _scaleFactor) + Vector3.up * _mouthHeight;

        // The hairball grows as it leaves the mouth, flies in an arc and lands with a squash
        private IEnumerator Spit(UlvaneProjectile hairball, Vector3 landing, Action<UlvaneProjectile> onLanded)
        {
            Vector3 start = hairball.transform.position;
            Vector3 end = new Vector3(landing.x, GroundY + _hairballRestHeight, landing.z);
            for (float elapsed = 0f; elapsed < _spitFlightTime; elapsed += Time.deltaTime)
            {
                float progress = elapsed / _spitFlightTime;
                hairball.transform.position = Vector3.Lerp(start, end, progress) + Vector3.up * (_spitArcHeight * 4f * progress * (1f - progress));
                hairball.transform.localScale = Vector3.one * (_hairballScale * Mathf.Lerp(0.3f, 1f, progress));
                yield return null;
            }

            hairball.transform.position = end;
            for (float elapsed = 0f; elapsed < _splatTime; elapsed += Time.deltaTime)
            {
                float squash = Mathf.Sin(elapsed / _splatTime * Mathf.PI) * 0.25f;
                hairball.transform.localScale = new Vector3(1f + squash, 1f - squash, 1f + squash) * _hairballScale;
                yield return null;
            }

            hairball.transform.localScale = Vector3.one * _hairballScale;
            onLanded?.Invoke(hairball);
        }

        // A small trigger on the Ignore Raycast layer for the prompt to float above; it is not part of the cat so it never gets in the way of clicks
        private Transform CreatePetTarget()
        {
            GameObject target = new GameObject("Pet Target");
            target.layer = 2;
            target.transform.SetParent(transform, false);
            SphereCollider sphere = target.AddComponent<SphereCollider>();
            sphere.isTrigger = true;
            sphere.radius = 0.4f;
            sphere.center = Vector3.up * 0.3f;
            return target.transform;
        }

        // Eases from the current pose to the wanted one: roll lays the cat on its side, pitch rears it up (negative) or slams it down
        private IEnumerator MoveToPose(float duration, float roll, float pitch, float lift, float scaleFactor)
        {
            float startRoll = _roll;
            float startPitch = _pitch;
            float startLift = _lift;
            float startScale = _scaleFactor;
            for (float elapsed = 0f; elapsed < duration; elapsed += Time.deltaTime)
            {
                float progress = Mathf.SmoothStep(0f, 1f, elapsed / duration);
                ApplyPose(Mathf.Lerp(startRoll, roll, progress), Mathf.Lerp(startPitch, pitch, progress), Mathf.Lerp(startLift, lift, progress), Mathf.Lerp(startScale, scaleFactor, progress));
                yield return null;
            }

            ApplyPose(roll, pitch, lift, scaleFactor);
        }

        private void ApplyPose(float roll, float pitch, float lift, float scaleFactor)
        {
            _roll = roll;
            _pitch = pitch;
            _lift = lift;
            _scaleFactor = scaleFactor;
            transform.rotation = Quaternion.Euler(0f, _yaw, 0f) * Quaternion.Euler(pitch, 0f, roll);
            transform.position = new Vector3(transform.position.x, _groundLevel + lift, transform.position.z);
            transform.localScale = _baseScale * scaleFactor;
        }
    }
}
