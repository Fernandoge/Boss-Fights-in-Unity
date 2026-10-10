using System.Collections.Generic;
using Manager.GameManager;
using Shared;
using UnityEngine;

namespace Bosses.Vexara
{
    // One orb of the Orb Barrage: it slides over the floor in a straight line, bounces off the arena walls and off the other orbs, and hurts the player on contact until its time runs out
    public class BouncingOrb : MonoBehaviour
    {
        [SerializeField] private Transform _visual;
        [SerializeField] private SkillIndicator _markerPrefab;
        [SerializeField] private GameObject _bounceEffectPrefab;
        [SerializeField] private float _radius = 0.8f;
        [SerializeField] private float _floatHeight = 1.1f;
        [SerializeField] private float _spawnHeight = 2.4f;
        [SerializeField] private float _playerHitPadding = 0.2f;
        [SerializeField] private float _growTime = 0.3f;
        [SerializeField] private float _fadeTime = 0.4f;
        [SerializeField] private Vector2 _bounceSpeedBonus = new Vector2(0.45f, 0.65f);

        private SkillIndicator _marker;
        private Vector4 _bounds;
        private Vector3 _velocity;
        private float _baseSpeed;
        private float _speed;
        private float _lifetime;
        private float _age;
        private float _markerY;
        private int _damage;

        private static readonly List<BouncingOrb> ActiveOrbs = new List<BouncingOrb>();

        // Orbs deal damage and collide only while they are fully there: not while growing in and not while fading out
        private bool IsSolid => _age >= _growTime && _age < _lifetime;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => ActiveOrbs.Clear();

        private void Update()
        {
            _age += Time.deltaTime;

            if (_age >= _lifetime + _fadeTime)
            {
                Destroy(gameObject);
                return;
            }

            Move();
            UpdateVisual();
            UpdateMarker();

            if (!IsSolid)
                return;

            BounceOffOrbs();
            CheckPlayerHit();
        }

        private void OnDestroy()
        {
            ActiveOrbs.Remove(this);
            if (_marker)
                _marker.Hide();
        }

        // bounds is (minX, minZ, maxX, maxZ) for the orb's center
        public void Begin(Vector3 position, Vector3 direction, float speed, Vector4 bounds, float lifetime, int damage)
        {
            _bounds = bounds;
            _baseSpeed = speed;
            _speed = speed;
            _lifetime = lifetime;
            _damage = damage;
            _velocity = new Vector3(direction.x, 0f, direction.z).normalized * speed;

            position.x = Mathf.Clamp(position.x, bounds.x, bounds.z);
            position.z = Mathf.Clamp(position.z, bounds.y, bounds.w);
            transform.position = position;

            // The marker is the orb's footprint on the floor, so its position can be read on the tilted camera
            _marker = Instantiate(_markerPrefab);
            _marker.ShowCircle(position, _radius, 0.01f, false);
            _markerY = _marker.transform.position.y;

            ActiveOrbs.Add(this);
            UpdateVisual();
            UpdateMarker();
        }

        // Lets every orb on the field fade away early
        public static void DespawnAll()
        {
            foreach (BouncingOrb orb in ActiveOrbs)
                orb._lifetime = Mathf.Min(orb._lifetime, orb._age);
        }

        private void Move()
        {
            Vector3 position = transform.position + _velocity * Time.deltaTime;
            bool bounced = false;

            if (position.x < _bounds.x)
            {
                position.x = _bounds.x;
                _velocity.x = Mathf.Abs(_velocity.x);
                bounced = true;
            }
            else if (position.x > _bounds.z)
            {
                position.x = _bounds.z;
                _velocity.x = -Mathf.Abs(_velocity.x);
                bounced = true;
            }

            if (position.z < _bounds.y)
            {
                position.z = _bounds.y;
                _velocity.z = Mathf.Abs(_velocity.z);
                bounced = true;
            }
            else if (position.z > _bounds.w)
            {
                position.z = _bounds.w;
                _velocity.z = -Mathf.Abs(_velocity.z);
                bounced = true;
            }

            transform.position = position;

            if (bounced)
            {
                RollBounceSpeed();
                _velocity = _velocity.normalized * _speed;
            }

            if (bounced && IsSolid)
                SpawnBounceEffect(position);
        }

        // Equal masses: the orbs trade the speed along the line between their centers, then both keep the same speed as before
        private void BounceOffOrbs()
        {
            foreach (BouncingOrb other in ActiveOrbs)
            {
                if (other == this || !other.IsSolid || GetInstanceID() > other.GetInstanceID())
                    continue;

                Vector3 offset = other.transform.position - transform.position;
                offset.y = 0f;
                float minDistance = _radius + other._radius;
                if (offset.sqrMagnitude >= minDistance * minDistance)
                    continue;

                Vector3 normal = offset.sqrMagnitude > 0.0001f ? offset.normalized : Vector3.right;
                float approach = Vector3.Dot(_velocity - other._velocity, normal);
                if (approach <= 0f)
                    continue;

                _velocity -= normal * approach;
                other._velocity += normal * approach;
                RollBounceSpeed();
                other.RollBounceSpeed();
                _velocity = KeepSpeed(_velocity, -normal);
                other._velocity = other.KeepSpeed(other._velocity, normal);

                // Push the orbs apart so they do not stay stuck inside each other
                float overlap = minDistance - offset.magnitude;
                transform.position -= normal * (overlap * 0.5f);
                other.transform.position += normal * (overlap * 0.5f);

                SpawnBounceEffect(transform.position + normal * _radius);
            }
        }

        // Every bounce gives the orb a new random speed above its launch speed
        private void RollBounceSpeed() => _speed = _baseSpeed * (1f + Random.Range(_bounceSpeedBonus.x, _bounceSpeedBonus.y));

        private Vector3 KeepSpeed(Vector3 velocity, Vector3 fallbackDirection)
            => velocity.sqrMagnitude > 0.0001f ? velocity.normalized * _speed : fallbackDirection * _speed;

        private void CheckPlayerHit()
        {
            Vector3 toPlayer = GameManager.Instance.player.transform.position - transform.position;
            toPlayer.y = 0f;
            float hitRadius = _radius + _playerHitPadding;
            if (toPlayer.sqrMagnitude <= hitRadius * hitRadius)
                GameManager.Instance.player.DamagePlayer(_damage);
        }

        // Grows in while dropping from the boss's hand to its floating height, shrinks away at the end of its life
        private void UpdateVisual()
        {
            if (!_visual)
                return;

            float grow = Mathf.Clamp01(_age / _growTime);
            float fade = 1f - Mathf.Clamp01((_age - _lifetime) / _fadeTime);
            _visual.localScale = Vector3.one * (Mathf.SmoothStep(0f, 1f, grow) * fade);
            _visual.localPosition = Vector3.up * Mathf.Lerp(_spawnHeight, _floatHeight, Mathf.SmoothStep(0f, 1f, grow));
        }

        private void UpdateMarker()
        {
            if (!_marker)
                return;

            // The circle indicator's origin sits at the south rim of the circle
            Vector3 position = transform.position;
            _marker.transform.position = new Vector3(position.x, _markerY, position.z - _radius);

            if (_age >= _lifetime)
            {
                _marker.Hide();
                _marker = null;
            }
        }

        private void SpawnBounceEffect(Vector3 position)
        {
            if (_bounceEffectPrefab)
                Destroy(Instantiate(_bounceEffectPrefab, position + Vector3.up * _floatHeight, Quaternion.identity), 2f);
        }
    }
}
