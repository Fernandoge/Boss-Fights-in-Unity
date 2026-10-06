using System;
using Manager.GameManager;
using UnityEngine;

namespace Bosses.Second_Boss
{
    [Serializable]
    public class StarfallSettings
    {
        public float radius = 1.2f;
        public float telegraphTime = 2.2f;
        public int damage = 1;
        public float laserSpeed = 14f;
        public float laserWidth = 0.6f;
        public float laserLength = 5f;
        public int laserDamage = 1;
    }

    // A star that falls from the sky onto an 8-pointed star marked on the floor; when it lands it hurts inside the body of the star and shoots 8 lasers along the floor, one through each point, with no other warning
    public class StarfallStar : MonoBehaviour
    {
        private const int LaserCount = 8;

        [SerializeField] private StarLaser _laserPrefab;
        [SerializeField] private GameObject _impactEffectPrefab;
        [SerializeField] private float _impactEffectScale = 0.7f;
        [SerializeField] private Transform _visual;
        [SerializeField] private Transform _indicatorBase;
        [SerializeField] private Transform _indicatorFill;
        [SerializeField] private float _indicatorSizeScale = 4.3f;
        [SerializeField] private float _indicatorHeight = 0.1f;
        [SerializeField] private float _fallHeight = 14f;
        [SerializeField] private float _spinSpeed = 120f;
        [SerializeField] private float _playerHitPadding = 0.2f;

        private StarfallSettings _settings;
        private Vector4 _arenaRect;
        private float _elapsed;
        private float _spin;
        private float _indicatorSize;

        private void Update()
        {
            _elapsed += Time.deltaTime;

            if (_elapsed >= _settings.telegraphTime)
            {
                Impact();
                return;
            }

            // A steady fall that ends with the telegraph fill, slow enough for the player to see it coming and walk away
            float t = _elapsed / _settings.telegraphTime;
            if (_visual)
                _visual.position = transform.position + Vector3.up * (_fallHeight * (1f - t));

            // The indicator fills from the center out, like the circle indicators
            if (_indicatorFill)
                _indicatorFill.localScale = new Vector3(_indicatorSize * t, _indicatorSize * t, 1f);
        }

        private void LateUpdate()
        {
            Camera mainCamera = Camera.main;
            if (!_visual || !mainCamera)
                return;

            _spin += _spinSpeed * Time.deltaTime;
            _visual.rotation = mainCamera.transform.rotation * Quaternion.Euler(0f, 0f, _spin);
        }

        // arenaRect is (minX, minZ, maxX, maxZ); the lasers stop at its edge
        public void Begin(Vector3 center, StarfallSettings settings, Vector4 arenaRect)
        {
            transform.position = center;
            _settings = settings;
            _arenaRect = arenaRect;
            _indicatorSize = settings.radius * _indicatorSizeScale;

            // The star shape on the floor is fixed in the world, so its 8 points are the directions of the lasers
            PlaceIndicator(_indicatorBase, center, _indicatorSize);
            PlaceIndicator(_indicatorFill, center, 0f);

            if (_visual)
                _visual.position = center + Vector3.up * _fallHeight;
        }

        private void PlaceIndicator(Transform indicator, Vector3 center, float size)
        {
            if (!indicator)
                return;

            indicator.SetPositionAndRotation(center + Vector3.up * _indicatorHeight, Quaternion.Euler(90f, 0f, 0f));
            indicator.localScale = new Vector3(size, size, 1f);
        }

        private void Impact()
        {
            if (_impactEffectPrefab)
            {
                GameObject effect = Instantiate(_impactEffectPrefab, transform.position, Quaternion.identity);
                effect.transform.localScale *= _impactEffectScale;
                Destroy(effect, 3f);
            }

            Vector3 toPlayer = GameManager.Instance.player.transform.position - transform.position;
            toPlayer.y = 0f;
            float hitRadius = _settings.radius + _playerHitPadding;
            if (toPlayer.sqrMagnitude <= hitRadius * hitRadius)
                GameManager.Instance.player.DamagePlayer(_settings.damage);

            // Straight up, down, left, right and the four diagonals, like the points of the star
            for (int i = 0; i < LaserCount; i++)
            {
                float angle = i * (360f / LaserCount) * Mathf.Deg2Rad;
                Vector3 direction = new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle));
                Instantiate(_laserPrefab).Begin(transform.position, direction, GetDistanceToArenaEdge(direction), _settings.laserSpeed, _settings.laserWidth, _settings.laserLength, _settings.laserDamage);
            }

            Destroy(gameObject);
        }

        private float GetDistanceToArenaEdge(Vector3 direction)
        {
            Vector3 position = transform.position;
            float distance = float.MaxValue;
            if (direction.x > 0.001f)
                distance = Mathf.Min(distance, (_arenaRect.z - position.x) / direction.x);
            else if (direction.x < -0.001f)
                distance = Mathf.Min(distance, (_arenaRect.x - position.x) / direction.x);

            if (direction.z > 0.001f)
                distance = Mathf.Min(distance, (_arenaRect.w - position.z) / direction.z);
            else if (direction.z < -0.001f)
                distance = Mathf.Min(distance, (_arenaRect.y - position.z) / direction.z);

            return Mathf.Max(distance, 0.5f);
        }
    }
}
