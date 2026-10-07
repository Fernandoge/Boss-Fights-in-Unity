using System;
using Interfaces;
using UnityEngine;
using UnityEngine.AI;

namespace Characters.Ninja
{
    // The big shuriken of the ninja: it flies straight until it hits something (damaging it if it can be hurt) or runs out of range, then it stays on the ground for a few seconds, where the player can swap places with it
    public class HuumaShuriken : MonoBehaviour
    {
        [SerializeField] private Transform _visual;
        [SerializeField] private TrailRenderer _trail;
        [SerializeField] private Light _glow;
        [SerializeField] private float _radius = 0.5f;
        [SerializeField] private float _flightSpinSpeed = 900f;
        [SerializeField] private float _stuckSpinSpeed = 25f;
        [SerializeField] private float _stuckHeight = 0.12f;
        [SerializeField] private float _blinkTime = 1f;

        private readonly RaycastHit[] _hits = new RaycastHit[16];
        private Transform _owner;
        private Action _onEnded;
        private Vector3 _direction;
        private int _damage;
        private float _flightHeight;
        private float _speed;
        private float _range;
        private float _traveled;
        private float _lifetime;
        private float _stuckTimer;
        private bool _isStuck;
        private bool _hasEnded;

        public bool IsStuck => _isStuck;

        private void Update()
        {
            _visual.Rotate(0f, (_isStuck ? _stuckSpinSpeed : _flightSpinSpeed) * Time.deltaTime, 0f, Space.World);

            if (_isStuck)
                UpdateStuck();
            else
                Fly();
        }

        // owner is the player (and anything under it), which the shuriken flies through
        public void Begin(Vector3 origin, Vector3 direction, int damage, float range, float speed, float lifetime, Transform owner, Action onEnded)
        {
            transform.position = origin;
            _flightHeight = origin.y - owner.position.y;
            _direction = new Vector3(direction.x, 0f, direction.z).normalized;
            _damage = damage;
            _range = range;
            _speed = speed;
            _lifetime = lifetime;
            _owner = owner;
            _onEnded = onEnded;
        }

        // Used when the player swaps places with it: it ends up where the player was, waiting on the floor (a flying one lands there with a fresh ground timer)
        public void RelocateTo(Vector3 position)
        {
            if (!_isStuck)
            {
                _isStuck = true;
                _stuckTimer = _lifetime;
            }

            PlaceOnFloor(position);
            if (_stuckTimer > _blinkTime)
                _visual.gameObject.SetActive(true);

            if (_trail)
            {
                _trail.emitting = false;
                _trail.Clear();
            }
        }

        private void Fly()
        {
            float step = Mathf.Min(_speed * Time.deltaTime, _range - _traveled);
            if (TryGetHit(step, out RaycastHit hit))
            {
                transform.position += _direction * Mathf.Max(hit.distance, 0f);
                hit.collider.GetComponentInParent<IDamageableByPlayer>()?.TakeDamage(_damage);
                Stick();
                return;
            }

            // The walls are low, so the edge of the walkable area is what stops it
            Vector3 floorStart = transform.position - Vector3.up * _flightHeight;
            if (NavMesh.Raycast(floorStart, floorStart + _direction * step, out NavMeshHit edge, NavMesh.AllAreas))
            {
                transform.position = edge.position + Vector3.up * _flightHeight;
                Stick();
                return;
            }

            transform.position += _direction * step;
            _traveled += step;
            if (_traveled >= _range - 0.001f)
                Stick();
        }

        // Anything solid stops it, except the player and their clones
        private bool TryGetHit(float distance, out RaycastHit nearest)
        {
            nearest = default;
            int count = Physics.SphereCastNonAlloc(transform.position, _radius, _direction, _hits, distance, ~0, QueryTriggerInteraction.Ignore);
            float nearestDistance = float.MaxValue;
            bool found = false;
            for (int i = 0; i < count; i++)
            {
                RaycastHit hit = _hits[i];
                if (hit.collider.transform.IsChildOf(_owner) || hit.collider.GetComponentInParent<NinjaClone>() != null)
                    continue;

                if (hit.distance < nearestDistance)
                {
                    nearestDistance = hit.distance;
                    nearest = hit;
                    found = true;
                }
            }

            return found;
        }

        // It drops to the floor where it landed and waits there
        private void Stick()
        {
            _isStuck = true;
            _stuckTimer = _lifetime;
            PlaceOnFloor(transform.position);

            if (_trail)
                _trail.emitting = false;
        }

        private void PlaceOnFloor(Vector3 position)
        {
            transform.position = NavMesh.SamplePosition(position, out NavMeshHit navHit, 6f, NavMesh.AllAreas)
                ? navHit.position + Vector3.up * _stuckHeight
                : position;
        }

        private void UpdateStuck()
        {
            _stuckTimer -= Time.deltaTime;
            if (_stuckTimer <= 0f)
            {
                End();
                return;
            }

            // It blinks for the last moment, so the player knows it is about to vanish
            if (_stuckTimer < _blinkTime)
                _visual.gameObject.SetActive(Mathf.FloorToInt(_stuckTimer * 10f) % 2 == 0);
        }

        private void End()
        {
            if (_hasEnded)
                return;

            _hasEnded = true;
            _onEnded?.Invoke();
            Destroy(gameObject);
        }
    }
}
