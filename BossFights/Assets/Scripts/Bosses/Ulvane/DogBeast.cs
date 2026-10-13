using System.Collections;
using System.Collections.Generic;
using Shared;
using UnityEngine;

namespace Bosses.Ulvane
{
    // Tail Whirlwind: the dog chases its own tail faster and faster while a circle fills around it, then it turns into a whirlwind that hunts the player for a few seconds
    public class DogBeast : UlvaneBeast
    {
        [Header("Tail Chase")]
        [SerializeField] private float _spinTime = 1.6f;
        [SerializeField] private float _tailCircleRadius = 0.9f;
        [SerializeField] private float _startSpinSpeed = 360f;
        [SerializeField] private float _endSpinSpeed = 1500f;

        [Header("Whirlwind")]
        [SerializeField] private float _whirlRadius = 2.2f;
        [SerializeField] private float _whirlHeight = 4.5f;
        [SerializeField] private float _growTime = 0.5f;
        [SerializeField] private float _chaseTime = 6f;
        [SerializeField] private float _speed = 5.5f;
        [SerializeField] private float _turnRate = 90f;
        [SerializeField] private float _whirlSpinSpeed = 1500f;
        [SerializeField] private float _hitCooldown = 0.7f;
        [SerializeField] private float _fadeTime = 0.6f;
        [SerializeField] private float _dizzyTime = 1.2f;
        [SerializeField] private int _damage = 1;

        [Header("Snow Tornado Combo")]
        [SerializeField] private int _stackSize = 5;
        [SerializeField] private float _comboRunSpeed = 9f;
        [SerializeField] private float _stackOrbitRadius = 1.3f;
        [SerializeField] private float _stackOrbitHeight = 1.6f;
        [SerializeField] private float _stackOrbitSpeed = 320f;
        [SerializeField] private float _windDownTime = 0.6f;
        [SerializeField] private float _throwLaneTime = 0.45f;
        [SerializeField] private float _throwLaneWidth = 0.8f;
        [SerializeField] private float _throwInterval = 0.25f;
        [SerializeField] private float _throwLeadTime = 0.3f;
        [SerializeField] private float _throwSpeed = 48f;
        [SerializeField] private float _throwMaxLaneLength = 40f;
        [SerializeField] private float _throwMuzzleHeight = 1f;
        [SerializeField] private float _throwRecoverTime = 0.9f;
        [SerializeField] private int _comboDamage = 2;

        private Whirlwind _whirlwind;
        private Transform _stackPivot;
        private readonly List<UlvaneProjectile> _stack = new List<UlvaneProjectile>();
        private bool _isStormActive;
        private SkillIndicator _indicator;

        public override int HitDamage => _damage;

        public int StackSize => _stackSize;

        public int StackCount => _stack.Count;

        public int ComboDamage => _comboDamage;

        private void LateUpdate()
        {
            if (_stackPivot)
                _stackPivot.Rotate(0f, _stackOrbitSpeed * Time.deltaTime, 0f, Space.World);
        }

        private void OnDestroy() => ClearStorm();

        protected override void OnComboReleased() => ClearStorm();

        /// *** Snow Tornado Combo *** ///

        // Runs to the spot, spins up into a whirlwind and then stays there spinning, hurting whoever walks into it, until ThrowStack
        public IEnumerator StormStart(Vector3 point)
        {
            yield return ComboRunTo(point, _comboRunSpeed);

            Vector3 center = transform.position;
            _indicator = ShowCircle(center, _whirlRadius, _spinTime, false);
            _whirlwind = Whirlwind.Create(center, _whirlRadius, _whirlHeight);
            _stackPivot = new GameObject("Snowball Stack").transform;
            _stackPivot.position = center;

            yield return ChaseTail(center);
            _isStormActive = true;
            StartCoroutine(StormLoop());
        }

        // The dog catches a snowball: it joins the ones circling inside the whirlwind
        public void AddSnowball(UlvaneProjectile snowball)
        {
            if (!_stackPivot)
            {
                Destroy(snowball.gameObject);
                return;
            }

            snowball.Hold();
            _stack.Add(snowball);
            snowball.transform.SetParent(_stackPivot, true);
            for (int i = 0; i < _stack.Count; i++)
            {
                float angle = 360f * i / _stackSize * Mathf.Deg2Rad;
                _stack[i].transform.localPosition = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * _stackOrbitRadius + Vector3.up * (_stackOrbitHeight + 0.15f * i);
            }
        }

        // The dog keeps spinning in its whirlwind while the stacked snowballs go one after the other, each aimed at where the player is when its lane starts (like Gorath's rock shower): the lane fills for a moment, then the snowball flies down it. The whirlwind only winds down after the last one.
        public IEnumerator ThrowStack()
        {
            int count = _stack.Count;
            for (int i = 0; i < count; i++)
            {
                Vector3 direction = FlatDirectionTo(owner.PlayerAimPoint + owner.PlayerAimVelocity * _throwLeadTime);
                float length = NavMeshUtility.DistanceToEdge(transform.position, direction, _throwMaxLaneLength);
                ShowLine(transform.position, direction, length, _throwLaneWidth, _throwLaneTime);
                StartCoroutine(LaunchAfter(_stack[i], direction, length, _throwLaneTime));
                yield return new WaitForSeconds(_throwInterval);
            }

            yield return new WaitForSeconds(_throwLaneTime);
            _stack.Clear();

            _isStormActive = false;
            if (_indicator)
                _indicator.Hide();

            Idle();
            for (float elapsed = 0f; elapsed < _windDownTime && _whirlwind; elapsed += Time.deltaTime)
            {
                transform.Rotate(0f, _whirlSpinSpeed * (1f - elapsed / _windDownTime) * Time.deltaTime, 0f, Space.World);
                _whirlwind.SetGrowth(1f - elapsed / _windDownTime);
                _whirlwind.transform.position = transform.position;
                yield return null;
            }

            ClearStorm();
            yield return new WaitForSeconds(_throwRecoverTime);
        }

        private IEnumerator LaunchAfter(UlvaneProjectile snowball, Vector3 direction, float length, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (!snowball)
                yield break;

            snowball.transform.SetParent(null, true);
            snowball.transform.position = transform.position + direction * 0.6f + Vector3.up * _throwMuzzleHeight;
            snowball.Launch(direction, _throwSpeed, length / _throwSpeed + 0.2f, _comboDamage);
        }

        private IEnumerator StormLoop()
        {
            float hitTimer = 0f;
            while (_isStormActive)
            {
                transform.Rotate(0f, _whirlSpinSpeed * Time.deltaTime, 0f, Space.World);

                hitTimer -= Time.deltaTime;
                if (hitTimer <= 0f && IsPlayerNear(_whirlRadius))
                {
                    DamagePlayer(_comboDamage);
                    hitTimer = _hitCooldown;
                }

                yield return null;
            }
        }

        private void ClearStorm()
        {
            _isStormActive = false;
            if (_whirlwind)
                Destroy(_whirlwind.gameObject);

            if (_indicator)
                _indicator.Hide();

            if (_stackPivot)
                Destroy(_stackPivot.gameObject);

            _stack.Clear();
        }

        /// *** Tail Whirlwind *** ///

        protected override IEnumerator Perform()
        {
            // The circle is the danger zone: it fills while the dog spins up, then it stays and follows the dog
            Vector3 center = transform.position;
            _indicator = ShowCircle(center, _whirlRadius, _spinTime, false);
            _whirlwind = Whirlwind.Create(center, _whirlRadius, _whirlHeight);

            yield return ChaseTail(center);

            Vector3 heading = transform.forward;
            Gallop();
            float hitTimer = 0f;
            for (float elapsed = 0f; elapsed < _chaseTime; elapsed += Time.deltaTime)
            {
                heading = Vector3.RotateTowards(heading, FlatDirectionTo(owner.PlayerAimPoint), _turnRate * Mathf.Deg2Rad * Time.deltaTime, 0f);
                heading.y = 0f;
                heading.Normalize();
                MoveFlat(heading * (_speed * Time.deltaTime));
                transform.Rotate(0f, _whirlSpinSpeed * Time.deltaTime, 0f, Space.World);
                FollowDog();

                hitTimer -= Time.deltaTime;
                if (hitTimer <= 0f && IsPlayerNear(_whirlRadius))
                {
                    DamagePlayer(_damage);
                    hitTimer = _hitCooldown;
                }

                yield return null;
            }

            // The whirlwind dies down and leaves the dog dizzy
            _indicator.Hide();
            Idle();
            for (float elapsed = 0f; elapsed < _fadeTime; elapsed += Time.deltaTime)
            {
                transform.Rotate(0f, _whirlSpinSpeed * (1f - elapsed / _fadeTime) * Time.deltaTime, 0f, Space.World);
                _whirlwind.SetGrowth(1f - elapsed / _fadeTime);
                _whirlwind.transform.position = transform.position;
                yield return null;
            }

            Destroy(_whirlwind.gameObject);
            yield return new WaitForSeconds(_dizzyTime);
        }

        // Runs in a small circle that keeps tightening its pace, with the whirlwind growing around it
        private IEnumerator ChaseTail(Vector3 center)
        {
            Gallop();
            float angle = 0f;
            for (float elapsed = 0f; elapsed < _spinTime; elapsed += Time.deltaTime)
            {
                float progress = elapsed / _spinTime;
                angle += Mathf.Lerp(_startSpinSpeed, _endSpinSpeed, progress * progress) * Time.deltaTime;
                float radians = angle * Mathf.Deg2Rad;
                Vector3 offset = new Vector3(Mathf.Cos(radians), 0f, Mathf.Sin(radians)) * _tailCircleRadius;
                Vector3 tangent = new Vector3(-Mathf.Sin(radians), 0f, Mathf.Cos(radians));
                transform.position = new Vector3(center.x + offset.x, transform.position.y, center.z + offset.z);
                transform.rotation = Quaternion.LookRotation(tangent);
                _whirlwind.SetGrowth(Mathf.Clamp01((elapsed - (_spinTime - _growTime)) / _growTime));
                yield return null;
            }

            transform.position = new Vector3(center.x, transform.position.y, center.z);
            _whirlwind.SetGrowth(1f);
        }

        private void FollowDog()
        {
            _whirlwind.transform.position = transform.position;
            _indicator.MoveCircle(transform.position);
        }
    }
}
