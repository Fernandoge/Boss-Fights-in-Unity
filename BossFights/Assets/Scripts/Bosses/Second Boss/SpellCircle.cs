using Manager.GameManager;
using Shared;
using UnityEngine;

namespace Bosses.Second_Boss
{
    public class SpellCircle : MonoBehaviour
    {
        [SerializeField] private SkillIndicator _indicatorPrefab;
        [SerializeField] private GameObject _impactEffectPrefab;
        [SerializeField] private float _playerHitPadding = 0.2f;
        [SerializeField] private float _lifetimeAfterImpact = 0.3f;

        private float _radius;
        private float _telegraphTime;
        private float _elapsed;
        private int _damage;
        private bool _hasImpacted;

        private void Update()
        {
            _elapsed += Time.deltaTime;

            if (!_hasImpacted && _elapsed >= _telegraphTime)
                Impact();

            if (_hasImpacted && _elapsed >= _telegraphTime + _lifetimeAfterImpact)
                Destroy(gameObject);
        }

        public void Begin(Vector3 center, float radius, float telegraphTime, int damage)
        {
            transform.position = center;
            _radius = radius;
            _telegraphTime = telegraphTime;
            _damage = damage;

            Instantiate(_indicatorPrefab).ShowCircle(center, radius, telegraphTime);
        }

        private void Impact()
        {
            _hasImpacted = true;

            if (_impactEffectPrefab)
                Destroy(Instantiate(_impactEffectPrefab, transform.position, Quaternion.identity), 3f);

            Vector3 toPlayer = GameManager.Instance.player.transform.position - transform.position;
            toPlayer.y = 0f;
            float hitRadius = _radius + _playerHitPadding;
            if (toPlayer.sqrMagnitude <= hitRadius * hitRadius)
                GameManager.Instance.player.DamagePlayer(_damage);
        }
    }
}
