using System.Collections.Generic;
using Interfaces;
using UnityEngine;

namespace Characters.Ninja
{
    public class GroundShuriken : MonoBehaviour
    {
        [SerializeField] private float _flightTime = 0.25f;
        [SerializeField] private float _arcHeight = 1.2f;
        [SerializeField] private float _spinSpeed = 1440f;
        [SerializeField] private float _stuckTime = 1.5f;
        [SerializeField] private float _sinkTime = 0.3f;
        [SerializeField] private GameObject _impactPrefab;
        [SerializeField] private float _impactScale = 0.35f;

        private Vector3 _from;
        private Vector3 _to;
        private int _damage;
        private float _radius;
        private float _timer;
        private State _state;
        private Vector3 _fullScale;
        private readonly Collider[] _hitColliders = new Collider[64];
        private readonly HashSet<IDamageableByPlayer> _hitTargets = new HashSet<IDamageableByPlayer>();

        /// *** Unity Events *** ///

        private void Update()
        {
            _timer += Time.deltaTime;
            switch (_state)
            {
                case State.Flying:
                    float progress = Mathf.Clamp01(_timer / _flightTime);
                    transform.position = Vector3.Lerp(_from, _to, progress) + Vector3.up * (_arcHeight * 4f * progress * (1f - progress));
                    transform.Rotate(0f, _spinSpeed * Time.deltaTime, 0f);

                    // Only the flight hurts: anything it flies through is hit, so a throw at a spot beyond the boss still hits the boss
                    if (HitTargets(transform.position, _radius))
                        Consume();
                    else if (progress >= 1f)
                        Land();
                    break;
                case State.Stuck:
                    // Once it is stuck in the ground it is only a prop and hurts nothing
                    if (_timer >= _stuckTime)
                    {
                        _state = State.Sinking;
                        _timer = 0f;
                    }
                    break;
                case State.Sinking:
                    transform.localScale = _fullScale * (1f - Mathf.Clamp01(_timer / _sinkTime));
                    if (_timer >= _sinkTime)
                        Destroy(gameObject);
                    break;
            }
        }

        /// *** Public Methods *** ///

        public void Begin(Vector3 from, Vector3 to, int damage, float radius)
        {
            _from = from;
            _to = to;
            _damage = damage;
            _radius = radius;
            _fullScale = transform.localScale;
            transform.position = from;
            transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
        }

        /// *** Private Methods *** ///

        private void Land()
        {
            _state = State.Stuck;
            _timer = 0f;
            transform.position = _to;
            SpawnImpact(_to);
        }

        // Like the other projectiles, a shuriken that hits something is gone
        private void Consume()
        {
            SpawnImpact(transform.position);
            Destroy(gameObject);
        }

        private void SpawnImpact(Vector3 position)
        {
            if (!_impactPrefab)
                return;

            GameObject impact = Instantiate(_impactPrefab, position, Quaternion.identity);
            impact.transform.localScale *= _impactScale;
            Destroy(impact, 1.5f);
        }

        // Hurts every target in the sphere once (a target has several colliders) and tells whether it hit any
        private bool HitTargets(Vector3 center, float radius)
        {
            _hitTargets.Clear();
            int count = Physics.OverlapSphereNonAlloc(center, radius, _hitColliders);
            for (int i = 0; i < count; i++)
            {
                IDamageableByPlayer damageable = _hitColliders[i].GetComponentInParent<IDamageableByPlayer>();
                if (damageable != null && _hitTargets.Add(damageable))
                    damageable.TakeDamage(DamageRoll.Roll(_damage, out bool isCrit), isCrit);
            }

            return _hitTargets.Count > 0;
        }

        private enum State
        {
            Flying,
            Stuck,
            Sinking
        }
    }
}
