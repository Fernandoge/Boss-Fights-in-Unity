using UnityEngine;

namespace Bosses.Second_Boss
{
    // An orb of light above the boss's head that grows while it charges the color intermission, cycles through the colors of the squares faster and faster, then flashes and fades
    public class IntermissionCharge : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _orb;
        [SerializeField] private float _startSize = 0.3f;
        [SerializeField] private float _maxSize = 2.4f;
        [SerializeField] private float _slowCycleSpeed = 0.8f;
        [SerializeField] private float _fastCycleSpeed = 5f;
        [SerializeField] private float _pulseSpeed = 6f;
        [SerializeField] private float _flashTime = 0.5f;

        private Transform _follow;
        private Vector3 _offset;
        private Color[] _palette;
        private float _duration;
        private float _elapsed;
        private float _cycle;

        private void LateUpdate()
        {
            _elapsed += Time.deltaTime;

            if (_follow)
                transform.position = _follow.position + _offset;

            Camera mainCamera = Camera.main;
            if (mainCamera)
                transform.rotation = mainCamera.transform.rotation;

            float t = Mathf.Clamp01(_elapsed / _duration);
            _cycle += Mathf.Lerp(_slowCycleSpeed, _fastCycleSpeed, t * t) * Time.deltaTime;
            float size = Mathf.Lerp(_startSize, _maxSize, t) * (1f + 0.08f * Mathf.Sin(_elapsed * _pulseSpeed));
            float alpha = Mathf.Lerp(0.5f, 1f, t);

            // After the charge it swells and fades
            if (_elapsed > _duration)
            {
                float f = (_elapsed - _duration) / _flashTime;
                if (f >= 1f)
                {
                    Destroy(gameObject);
                    return;
                }

                size = _maxSize * (1f + f * 0.8f);
                alpha = 1f - f;
            }

            transform.localScale = Vector3.one * size;
            Color color = GetCycleColor();
            color.a = alpha;
            _orb.color = color;
        }

        public void Begin(Transform follow, Vector3 offset, Color[] palette, float duration)
        {
            _follow = follow;
            _offset = offset;
            _palette = palette;
            _duration = Mathf.Max(duration, 0.1f);
            transform.position = follow.position + offset;
            transform.localScale = Vector3.one * _startSize;
        }

        // Fades smoothly from one color of the palette to the next
        private Color GetCycleColor()
        {
            int count = _palette.Length;
            int index = Mathf.FloorToInt(_cycle) % count;
            return Color.Lerp(_palette[index], _palette[(index + 1) % count], Mathf.SmoothStep(0f, 1f, _cycle - Mathf.Floor(_cycle)));
        }
    }
}
