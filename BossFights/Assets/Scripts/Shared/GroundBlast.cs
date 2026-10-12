using Manager.GameManager;
using UnityEngine;

namespace Shared
{
    // A circle on the floor that fills while it warns, then hurts whoever stands inside when it is full
    public class GroundBlast : MonoBehaviour
    {
        [SerializeField] private float _playerHitPadding = 0.2f;
        [SerializeField] private float _lifetimeAfterImpact = 0.3f;

        private GameObject _impactEffectPrefab;
        private GameObject _visual;
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

        // The visual (an egg, for example) is a prefab kept at the center until the blast goes off
        public static GroundBlast Create(SkillIndicator indicatorPrefab, Vector3 center, float radius, float telegraphTime, int damage, GameObject impactEffectPrefab = null, GameObject visualPrefab = null)
        {
            GameObject blastObject = new GameObject("Ground Blast");
            blastObject.transform.position = center;
            GroundBlast blast = blastObject.AddComponent<GroundBlast>();
            blast._radius = radius;
            blast._telegraphTime = telegraphTime;
            blast._damage = damage;
            blast._impactEffectPrefab = impactEffectPrefab;
            if (visualPrefab)
                blast._visual = Instantiate(visualPrefab, center, Quaternion.identity, blastObject.transform);

            Instantiate(indicatorPrefab).ShowCircle(center, radius, telegraphTime);
            return blast;
        }

        private void Impact()
        {
            _hasImpacted = true;
            if (_visual)
                Destroy(_visual);

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
