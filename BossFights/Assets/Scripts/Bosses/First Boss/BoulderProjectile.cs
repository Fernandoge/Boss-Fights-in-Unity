using Characters;
using Manager.GameManager;
using UnityEngine;
using UnityEngine.AI;

namespace Bosses.First_Boss
{
    public class BoulderProjectile : MonoBehaviour
    {
        [SerializeField] private Transform _model;
        [SerializeField] private SphereCollider _collider;
        [SerializeField] private GameObject _breakEffectPrefab;
        [SerializeField] private float _breakEffectBaseRadius = 4f;
        [SerializeField] private float _breakEffectLifetime = 4f;

        private Vector3 _direction;
        private Vector3 _rollAxis;
        private float _speed;
        private float _radius;
        private int _damage;
        private float _ignoreObstaclesDistance;
        private float _maxDistance;
        private float _distanceTraveled;
        private bool _isLaunched;
        private bool _isBroken;

        private void Update()
        {
            if (!_isLaunched)
                return;

            float step = _speed * Time.deltaTime;
            transform.position += _direction * step;
            _distanceTraveled += step;

            // Roll without slipping: angular speed = speed / radius around the axis perpendicular to the direction
            _model.Rotate(_rollAxis, step / _radius * Mathf.Rad2Deg, Space.World);

            if (_distanceTraveled >= _maxDistance)
                Break();
        }

        // Obstacles are ignored for the first ignoreObstaclesDistance so the boulder can leave the wall it spawns inside
        public void Launch(Vector3 direction, float speed, float radius, int damage, float ignoreObstaclesDistance, float maxDistance)
        {
            _direction = new Vector3(direction.x, 0f, direction.z).normalized;
            _rollAxis = Vector3.Cross(Vector3.up, _direction);
            _speed = speed;
            _radius = radius;
            _damage = damage;
            _ignoreObstaclesDistance = ignoreObstaclesDistance;
            _maxDistance = maxDistance;

            _model.localScale = Vector3.one * radius * 2f;
            _collider.radius = radius;
            _isLaunched = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_isBroken || !_isLaunched)
                return;

            if (other.GetComponent<PlayerController>())
            {
                GameManager.Instance.player.DamagePlayer(_damage);
                Break();
            }
            else if (other.GetComponent<BoulderProjectile>())
                Break();
            else if (other.GetComponent<NavMeshObstacle>() && _distanceTraveled >= _ignoreObstaclesDistance)
                Break();
        }

        private void Break()
        {
            _isBroken = true;

            if (_breakEffectPrefab)
            {
                GameObject effect = Instantiate(_breakEffectPrefab, transform.position, Quaternion.identity);
                effect.transform.localScale = Vector3.one * (_radius / _breakEffectBaseRadius);
                Destroy(effect, _breakEffectLifetime);
            }

            Destroy(gameObject);
        }
    }
}
