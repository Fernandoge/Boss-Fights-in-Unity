using System;
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
        private Transform _followTarget;
        private Vector3 _followOffset;
        private Action<UlvaneProjectile> _onArrive;

        private void Update()
        {
            float deltaTime = Time.deltaTime;
            if (_followTarget)
            {
                FlyToTarget(deltaTime);
                return;
            }

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
            enabled = true;
            _followTarget = null;
            direction.y = 0f;
            _direction = direction.normalized;
            _speed = speed;
            _timeLeft = lifetime;
            _damage = damage;
            _groundY = NavMeshUtility.Snap(transform.position).y;
            transform.rotation = Quaternion.LookRotation(_direction);
        }

        // A harmless ball that flies to a target (a combo partner); on arrival it stops and hands itself to the callback, which owns it from then on
        public void LaunchAtTarget(Transform target, Vector3 offset, float speed, Action<UlvaneProjectile> onArrive)
        {
            enabled = true;
            _followTarget = target;
            _followOffset = offset;
            _speed = speed;
            _onArrive = onArrive;
        }

        // Stops the ball from moving and hurting, so it can be carried as a prop until Launch is called again
        public void Hold() => enabled = false;

        private void FlyToTarget(float deltaTime)
        {
            Vector3 destination = _followTarget.position + _followOffset;
            transform.position = Vector3.MoveTowards(transform.position, destination, _speed * deltaTime);
            if ((destination - transform.position).sqrMagnitude > 0.09f)
                return;

            _followTarget = null;
            enabled = false;
            _onArrive?.Invoke(this);
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
