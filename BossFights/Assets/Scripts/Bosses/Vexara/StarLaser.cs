using Manager.GameManager;
using UnityEngine;

namespace Bosses.Vexara
{
    // One of the 8 beams of the starfall burst: a bolt of fixed length that flies out of the impact point along the floor (the tail follows the head, so the part behind disappears) and ends at the arena edge
    public class StarLaser : MonoBehaviour
    {
        [SerializeField] private Transform _beam;
        [SerializeField] private float _playerHitPadding = 0.15f;
        [SerializeField] private float _height = 0.12f;

        private Vector3 _origin;
        private Vector3 _direction;
        private float _maxLength;
        private float _speed;
        private float _width;
        private float _length;
        private float _elapsed;
        private int _damage;

        private void Update()
        {
            _elapsed += Time.deltaTime;

            float head = Mathf.Min(_maxLength, _elapsed * _speed);
            float tail = Mathf.Max(0f, _elapsed * _speed - _length);
            if (tail >= _maxLength)
            {
                Destroy(gameObject);
                return;
            }

            UpdateBeam(tail, head);
            CheckPlayerHit(tail, head);
        }

        // maxLength is how far the head goes before the arena edge; length is the size of the bolt
        public void Begin(Vector3 origin, Vector3 direction, float maxLength, float speed, float width, float length, int damage)
        {
            _origin = origin;
            _direction = new Vector3(direction.x, 0f, direction.z).normalized;
            _maxLength = Mathf.Max(maxLength, 0.1f);
            _speed = speed;
            _width = width;
            _length = length;
            _damage = damage;
            UpdateBeam(0f, 0.01f);
        }

        // The beam is a flat quad lying on the floor: its length axis points along the beam and its width axis across it
        private void UpdateBeam(float tail, float head)
        {
            if (!_beam)
                return;

            float size = Mathf.Max(head - tail, 0.01f);
            _beam.SetPositionAndRotation(_origin + _direction * ((head + tail) * 0.5f) + Vector3.up * _height, Quaternion.LookRotation(Vector3.down, _direction));
            _beam.localScale = new Vector3(_width, size, 1f);
        }

        private void CheckPlayerHit(float tail, float head)
        {
            Vector3 toPlayer = GameManager.Instance.player.transform.position - _origin;
            toPlayer.y = 0f;
            float along = Vector3.Dot(toPlayer, _direction);
            float across = Mathf.Abs(Vector3.Dot(toPlayer, new Vector3(-_direction.z, 0f, _direction.x)));
            if (along >= tail - _playerHitPadding && along <= head + _playerHitPadding && across <= _width * 0.5f + _playerHitPadding)
                GameManager.Instance.player.DamagePlayer(_damage);
        }
    }
}
