using Manager.GameManager;
using Shared;
using UnityEngine;
using UnityEngine.AI;

namespace Bosses.Ulvane
{
    // Flies straight over the floor (an arrow or a snowball), hurts the player it touches and stops at the first obstacle
    public class UlvaneProjectile : MonoBehaviour
    {
        [SerializeField] private float _hitRadius = 0.5f;

        private Vector3 _direction;
        private float _speed;
        private float _timeLeft;
        private float _groundY;
        private int _damage;

        private void Update()
        {
            float deltaTime = Time.deltaTime;
            Vector3 start = transform.position;
            Vector3 step = _direction * (_speed * deltaTime);

            // The floor under the projectile is the NavMesh, so trees and the arena edge stop it
            Vector3 groundStart = new Vector3(start.x, _groundY, start.z);
            if (NavMesh.Raycast(groundStart, groundStart + step, out NavMeshHit _, NavMesh.AllAreas))
            {
                Destroy(gameObject);
                return;
            }

            transform.position = start + step;
            if (IsPlayerHit(start, start + step))
            {
                GameManager.Instance.player.DamagePlayer(_damage);
                Destroy(gameObject);
                return;
            }

            _timeLeft -= deltaTime;
            if (_timeLeft <= 0f)
                Destroy(gameObject);
        }

        public void Launch(Vector3 direction, float speed, float lifetime, int damage)
        {
            direction.y = 0f;
            _direction = direction.normalized;
            _speed = speed;
            _timeLeft = lifetime;
            _damage = damage;
            _groundY = NavMeshUtility.Snap(transform.position).y;
            transform.rotation = Quaternion.LookRotation(_direction);
        }

        private bool IsPlayerHit(Vector3 from, Vector3 to)
        {
            Vector3 playerPosition = GameManager.Instance.player.transform.position;
            from.y = playerPosition.y;
            to.y = playerPosition.y;
            Vector3 segment = to - from;
            float t = segment.sqrMagnitude > 0f ? Mathf.Clamp01(Vector3.Dot(playerPosition - from, segment) / segment.sqrMagnitude) : 0f;
            return (from + segment * t - playerPosition).sqrMagnitude <= _hitRadius * _hitRadius;
        }
    }
}
