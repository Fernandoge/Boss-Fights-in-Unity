using System;
using UnityEngine;
using UnityEngine.AI;

namespace Characters.Ninja
{
    // The big shuriken of the ninja: it flies straight until it hits something or runs out of range, then it stays on the ground for a few seconds, where the player can swap places with it
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
        private Action _onStuck;
        private Action _onEnded;
        private Vector3 _direction;
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
        public void Begin(Vector3 origin, Vector3 direction, float range, float speed, float lifetime, Transform owner, Action onStuck, Action onEnded)
        {
            transform.position = origin;
            _direction = new Vector3(direction.x, 0f, direction.z).normalized;
            _range = range;
            _speed = speed;
            _lifetime = lifetime;
            _owner = owner;
            _onStuck = onStuck;
            _onEnded = onEnded;
        }

        // Used when the player swaps places with it
        public void Consume() => End();

        private void Fly()
        {
            float step = Mathf.Min(_speed * Time.deltaTime, _range - _traveled);
            if (TryGetHit(step, out RaycastHit hit))
            {
                transform.position += _direction * Mathf.Max(hit.distance, 0f);
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

            Vector3 position = transform.position;
            transform.position = NavMesh.SamplePosition(position, out NavMeshHit navHit, 6f, NavMesh.AllAreas)
                ? navHit.position + Vector3.up * _stuckHeight
                : position;

            if (_trail)
                _trail.emitting = false;

            _onStuck?.Invoke();
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
