using Manager.GameManager;
using UnityEngine;

namespace Bosses.Second_Boss
{
    // One third of the arena in the Timed Explosions attack: it lights up once, vanishes, and later explodes with no warning, hurting the player if they stand in it
    public class TimedExplosionZone : MonoBehaviour
    {
        [SerializeField] private Renderer _visual;
        [SerializeField] private Color _lightColor = new Color(0.6f, 0.85f, 1f, 0.6f);
        [SerializeField] private Color _explosionColor = new Color(1f, 0.5f, 0.25f, 1f);
        [SerializeField] private float _lightFadeTime = 0.1f;
        [SerializeField] private float _explosionVisibleTime = 0.45f;
        [SerializeField] private float _height = 0.12f;

        private Vector4 _rect;
        private MaterialPropertyBlock _block;
        private float _lightStart;
        private float _lightTime;
        private float _explodeTime;
        private float _elapsed;
        private int _damage;
        private bool _hasExploded;

        private static readonly int Tint_Color = Shader.PropertyToID("_TintColor");

        private void Update()
        {
            _elapsed += Time.deltaTime;

            if (!_hasExploded && _elapsed >= _explodeTime)
                Explode();

            if (_hasExploded)
            {
                float t = (_elapsed - _explodeTime) / _explosionVisibleTime;
                if (t >= 1f)
                {
                    Destroy(gameObject);
                    return;
                }

                SetVisual(_explosionColor, 1f - t * t);
                return;
            }

            // Fades in, stays and fades out during the light window
            float sinceLight = _elapsed - _lightStart;
            if (sinceLight < 0f || sinceLight > _lightTime + _lightFadeTime)
            {
                SetVisual(_lightColor, 0f);
                return;
            }

            float fade = Mathf.Min(Mathf.Clamp01(sinceLight / _lightFadeTime), Mathf.Clamp01((_lightTime + _lightFadeTime - sinceLight) / _lightFadeTime));
            SetVisual(_lightColor, fade);
        }

        // rect is (minX, minZ, maxX, maxZ); the zone lights up from lightStart for lightTime seconds and explodes at explodeTime, all counted from now
        public void Begin(Vector4 rect, float groundY, float lightStart, float lightTime, float explodeTime, int damage)
        {
            _rect = rect;
            _lightStart = lightStart;
            _lightTime = lightTime;
            _explodeTime = explodeTime;
            _damage = damage;

            // The visual is a flat quad lying on the floor over the whole zone
            transform.SetPositionAndRotation(new Vector3((rect.x + rect.z) * 0.5f, groundY + _height, (rect.y + rect.w) * 0.5f), Quaternion.Euler(90f, 0f, 0f));
            transform.localScale = new Vector3(rect.z - rect.x, rect.w - rect.y, 1f);
            SetVisual(_lightColor, 0f);
        }

        private void Explode()
        {
            _hasExploded = true;

            Vector3 position = GameManager.Instance.player.transform.position;
            if (position.x >= _rect.x && position.x <= _rect.z && position.z >= _rect.y && position.z <= _rect.w)
                GameManager.Instance.player.DamagePlayer(_damage);
        }

        private void SetVisual(Color color, float alpha)
        {
            if (!_visual)
                return;

            if (_block == null)
                _block = new MaterialPropertyBlock();

            color.a *= alpha;
            _visual.enabled = color.a > 0.001f;
            _visual.GetPropertyBlock(_block);
            _block.SetColor(Tint_Color, color);
            _visual.SetPropertyBlock(_block);
        }
    }
}
