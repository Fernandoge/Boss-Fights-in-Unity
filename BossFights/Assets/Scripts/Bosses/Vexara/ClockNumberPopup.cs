using TMPro;
using UnityEngine;

namespace Bosses.Vexara
{
    // A big number that fades in, stays for a moment and fades out; it can follow a transform (the boss) or stay where it is
    public class ClockNumberPopup : MonoBehaviour
    {
        [SerializeField] private TextMeshPro _text;
        [SerializeField] private SpriteRenderer _icon;
        [SerializeField] private float _fadeInTime = 0.15f;
        [SerializeField] private float _fadeOutTime = 0.5f;
        [SerializeField] private float _maxLifetime = 12f;
        [SerializeField, Range(0.5f, 1f)] private float _maxViewportY = 0.9f;

        private Transform _follow;
        private Vector3 _offset;
        private float _peakAlpha = 1f;
        private float _elapsed;
        private float _hideTime = -1f;

        // The highest point of the screen (0 to 1) the number may be at; above it the number is pushed down so it never leaves the screen
        public float MaxViewportY => _maxViewportY;

        private void LateUpdate()
        {
            _elapsed += Time.deltaTime;

            if (_follow)
                transform.position = _follow.position + _offset;

            Camera mainCamera = Camera.main;
            if (mainCamera)
            {
                transform.rotation = mainCamera.transform.rotation;

                Vector3 viewport = mainCamera.WorldToViewportPoint(transform.position);
                if (viewport.y > _maxViewportY)
                {
                    viewport.y = _maxViewportY;
                    transform.position = mainCamera.ViewportToWorldPoint(viewport);
                }
            }

            // The number stays until Hide() is called; the lifetime is only a safety net
            if (_hideTime < 0f && _elapsed >= _maxLifetime)
                Hide();

            float alpha = Mathf.Clamp01(_elapsed / _fadeInTime);
            if (_hideTime >= 0f)
                alpha = Mathf.Min(alpha, 1f - (_elapsed - _hideTime) / _fadeOutTime);

            SetAlpha(Mathf.Clamp01(alpha) * _peakAlpha);

            if (_hideTime >= 0f && _elapsed >= _hideTime + _fadeOutTime)
                Destroy(gameObject);
        }

        // With a follow target the offset is relative to it; without one it is the world position
        public void Begin(string text, Transform follow, Vector3 offset, float peakAlpha)
        {
            if (_text)
                _text.text = text;

            _follow = follow;
            _offset = offset;
            _peakAlpha = peakAlpha;
            transform.position = follow ? follow.position + offset : offset;
            SetAlpha(0f);
        }

        public void SetIconColor(Color color)
        {
            if (_icon)
                _icon.color = color;
        }

        public void Hide()
        {
            if (_hideTime < 0f)
                _hideTime = _elapsed;
        }

        // The popup shows text, a sprite (the clock icon) or both
        private void SetAlpha(float alpha)
        {
            if (_text)
                _text.alpha = alpha;

            if (_icon)
            {
                Color color = _icon.color;
                color.a = alpha;
                _icon.color = color;
            }
        }
    }
}
