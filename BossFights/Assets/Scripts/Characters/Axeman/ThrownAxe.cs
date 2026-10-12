using System.Collections.Generic;
using Interfaces;
using UnityEngine;

namespace Characters.Axeman
{
    // One of the two axes of the Axeman's throw (Beastmaster Wild Axes): the axes fan out from the hands to two points on either side of the target point (Spread apart), then converge again on their way back to the owner.
    // Each axe hurts a target once on the way out and once on the way back, so a target standing between the two landing points can take four hits; the way back ends where the owner is by then, so moving does not strand it.
    public class ThrownAxe : MonoBehaviour
    {
        [SerializeField] private Transform _spinner;
        [SerializeField] private float _spinSpeed = 1260f;
        [SerializeField] private float _hitRadius = 1.2f;

        private AxemanController _owner;
        private bool _isLeft;
        private Vector3 _origin;
        private Vector3 _forward;
        private Vector3 _side;
        private float _length;
        private float _width;
        private bool _isReturning;
        private Vector3 _far;
        private float _duration;
        private float _elapsed;
        private int _damage;
        private readonly Collider[] _buffer = new Collider[24];
        private readonly HashSet<IDamageableByPlayer> _hitTargets = new HashSet<IDamageableByPlayer>();

        /// *** Unity Events *** ///

        private void Update()
        {
            if (!_owner || _owner.IsDead)
            {
                Destroy(gameObject);
                return;
            }

            _elapsed += Time.deltaTime;
            float progress = _elapsed / _duration;
            if (progress >= 1f)
            {
                Catch();
                return;
            }

            // Turning around starts a new pass, so each axe hurts a target once per pass
            if (!_isReturning && progress >= 0.5f)
            {
                _isReturning = true;
                _hitTargets.Clear();
            }

            transform.position = GetPosition(progress);
            _spinner.Rotate(0f, (_isLeft ? -_spinSpeed : _spinSpeed) * Time.deltaTime, 0f, Space.Self);
            Strike();
        }

        /// *** Public Methods *** ///

        public void Launch(AxemanController owner, bool isLeft, Vector3 origin, Vector3 direction, float length, float width, float spread, float duration, int damage)
        {
            _owner = owner;
            _isLeft = isLeft;
            _origin = origin;
            _forward = direction;
            _side = Vector3.Cross(Vector3.up, direction) * (isLeft ? -1f : 1f);
            _length = length;
            _width = width;
            _far = origin + direction * length + _side * (spread * 0.5f);
            _duration = duration;
            _damage = damage;
            transform.position = origin;
        }

        /// *** Private Methods *** ///

        // Out to this axe's landing point and back to the owner, each leg an arc bulging outward
        private Vector3 GetPosition(float progress)
        {
            if (progress < 0.5f)
                return Arc(_origin, _far, progress * 2f);

            return Arc(_far, _owner.CatchPoint, (progress - 0.5f) * 2f);
        }

        // A cubic curve bulging to this axe's side: 1 on Width is a half circle over the chord (the 4/3 pushes the control points out like a circle's), 0 is a straight line
        private Vector3 Arc(Vector3 start, Vector3 end, float t)
        {
            Vector3 push = _side * (Vector3.Distance(start, end) * 0.5f * (4f / 3f) * _width);
            float inverse = 1f - t;
            return inverse * inverse * inverse * start + 3f * inverse * inverse * t * (start + push) + 3f * inverse * t * t * (end + push) + t * t * t * end;
        }

        private void Strike()
        {
            int count = Physics.OverlapSphereNonAlloc(transform.position, _hitRadius, _buffer, ~0, QueryTriggerInteraction.Collide);
            for (int i = 0; i < count; i++)
            {
                IDamageableByPlayer target = _buffer[i].GetComponentInParent<IDamageableByPlayer>();
                if (target == null || !_hitTargets.Add(target))
                    continue;

                int damage = DamageRoll.Roll(_damage, out bool isCrit);
                target.TakeDamage(damage, isCrit);
                _owner.PlayImpact(_buffer[i], transform.position);
            }
        }

        private void Catch()
        {
            _owner.OnAxeCaught(_isLeft);
            Destroy(gameObject);
        }
    }
}
