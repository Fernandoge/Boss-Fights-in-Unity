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
        [SerializeField] private float _lungeTime = 0.2f;
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

        private bool _isCountered;
        private bool _isCounterWindowOpen;

        public override int HitDamage => _damage;

        protected override IEnumerator Approach() => ApproachPlayer(_approachRange, _approachSpeed);

        public void TriggerCounter()
        {
            if (_isCounterWindowOpen)
                _isCountered = true;
        }

        protected override IEnumerator Perform()
        {
            _isCountered = false;
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
                float windupTime = Mathf.Max(_telegraphTime - _lungeTime, 0f);
                for (float elapsed = 0f; elapsed < windupTime && !_isCountered; elapsed += Time.deltaTime)
                {
                    FaceDirection(direction, Time.deltaTime);
                    yield return null;
                }

                CloseCounterWindow(counterTarget, prompt);

                if (_isCountered)
                {
                    cone.Hide();
                    yield return Stun();
                    yield break;
                }

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
            }
        }

        private void Strike(Vector3 origin, Vector3 direction)
        {
            if (owner.ImpactEffectPrefab)
                Destroy(Instantiate(owner.ImpactEffectPrefab, origin + direction * (_coneRadius * 0.5f), Quaternion.identity), 3f);

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
