using Manager.GameManager;
using Shared;
using UnityEngine;

namespace Bosses.Second_Boss
{
    // One wave of the clock intermission: the whole arena is warned in red, then hurts everywhere except one straight "spoke" from the centre
    public class ClockWaveBlast : MonoBehaviour
    {
        [SerializeField] private SkillIndicator _telegraphPrefab;
        [SerializeField] private SkillIndicator _flashPrefab;
        [SerializeField] private SkillIndicator _safeLinePrefab;
        [SerializeField] private float _safeTolerance = 0.2f;
        [SerializeField] private float _lifetimeAfterBlast = 1.2f;
        [SerializeField] private float _maxHitImmunityTime = 1f;

        private SkillIndicator _telegraph;
        private Vector3 _center;
        private Vector3 _direction;
        private Vector3 _side;
        private Vector4 _arenaRect;
        private float _groundY;
        private float _halfWidth;
        private float _startDistance;
        private float _length;
        private float _telegraphTime;
        private float _elapsed;
        private int _damage;
        private bool _hasBlasted;

        private void Update()
        {
            _elapsed += Time.deltaTime;

            if (!_hasBlasted && _elapsed >= _telegraphTime)
                Blast();

            if (_hasBlasted && _elapsed >= _telegraphTime + _lifetimeAfterBlast)
                Destroy(gameObject);
        }

        // The spoke must point along an arena axis; arenaRect is (minX, minZ, maxX, maxZ)
        public void Begin(Vector4 arenaRect, float groundY, Vector3 center, Vector3 spokeDirection, float spokeWidth, float spokeStartDistance, float telegraphTime, int damage)
        {
            _arenaRect = arenaRect;
            _groundY = groundY;
            _center = center;
            _direction = spokeDirection.normalized;
            _side = new Vector3(-_direction.z, 0f, _direction.x);
            _halfWidth = spokeWidth * 0.5f;
            _startDistance = spokeStartDistance;
            _telegraphTime = telegraphTime;
            _damage = damage;

            _length = Mathf.Abs(_direction.x) > 0.5f
                ? (_direction.x > 0f ? arenaRect.z - center.x : center.x - arenaRect.x)
                : (_direction.z > 0f ? arenaRect.w - center.z : center.z - arenaRect.y);

            // The warning covers the whole arena, safe line included, so it does not give the answer away
            _telegraph = Instantiate(_telegraphPrefab);
            ShowRect(_telegraph, arenaRect.x, arenaRect.y, arenaRect.z, arenaRect.w, telegraphTime, false);
        }

        public bool DebugIsPositionSafe(Vector3 position) => IsSafe(position);

        private void Blast()
        {
            _hasBlasted = true;

            if (_telegraph)
                _telegraph.Hide();

            GetSpokeRect(out float spokeMinX, out float spokeMinZ, out float spokeMaxX, out float spokeMaxZ);

            // Everything except the spoke flashes; the spoke is lit in a different colour so players can learn where the safe line was
            FlashRect(_arenaRect.x, _arenaRect.y, _arenaRect.z, spokeMinZ);
            FlashRect(_arenaRect.x, spokeMaxZ, _arenaRect.z, _arenaRect.w);
            FlashRect(_arenaRect.x, spokeMinZ, spokeMinX, spokeMaxZ);
            FlashRect(spokeMaxX, spokeMinZ, _arenaRect.z, spokeMaxZ);
            ShowRect(Instantiate(_safeLinePrefab), spokeMinX, spokeMinZ, spokeMaxX, spokeMaxZ, 0.01f, true);

            if (!IsSafe(GameManager.Instance.player.transform.position))
            {
                GameManager.Instance.player.DamagePlayer(_damage);
                // Waves come closer together than the player's normal hit immunity, so the next one must still be able to hurt
                GameManager.Instance.player.LimitDamageImmunity(_maxHitImmunityTime);
            }
        }

        private bool IsSafe(Vector3 position)
        {
            Vector3 relative = position - _center;
            float along = Vector3.Dot(relative, _direction);
            float across = Mathf.Abs(Vector3.Dot(relative, _side));
            // No tolerance on the near edge: the ground at the boss's feet must stay dangerous in every wave, or standing against the boss would be safe
            return along >= _startDistance && along <= _length + _safeTolerance && across <= _halfWidth + _safeTolerance;
        }

        private void GetSpokeRect(out float minX, out float minZ, out float maxX, out float maxZ)
        {
            if (Mathf.Abs(_direction.x) > 0.5f)
            {
                minX = _direction.x > 0f ? _center.x + _startDistance : _arenaRect.x;
                maxX = _direction.x > 0f ? _arenaRect.z : _center.x - _startDistance;
                minZ = _center.z - _halfWidth;
                maxZ = _center.z + _halfWidth;
            }
            else
            {
                minZ = _direction.z > 0f ? _center.z + _startDistance : _arenaRect.y;
                maxZ = _direction.z > 0f ? _arenaRect.w : _center.z - _startDistance;
                minX = _center.x - _halfWidth;
                maxX = _center.x + _halfWidth;
            }
        }

        private void FlashRect(float minX, float minZ, float maxX, float maxZ)
        {
            if (maxX - minX < 0.05f || maxZ - minZ < 0.05f)
                return;

            ShowRect(Instantiate(_flashPrefab), minX, minZ, maxX, maxZ, 0.01f, true);
        }

        private void ShowRect(SkillIndicator indicator, float minX, float minZ, float maxX, float maxZ, float duration, bool hideWhenFilled)
            => indicator.ShowLine(new Vector3((minX + maxX) * 0.5f, _groundY, minZ), Vector3.forward, maxZ - minZ, maxX - minX, duration, hideWhenFilled);
    }
}
