using TMPro;
using UnityEngine;

namespace Bosses.Second_Boss
{
    // A big number that fades in, stays for a moment and fades out; it can follow a transform (the boss) or stay where it is
    public class ClockNumberPopup : MonoBehaviour
    {
        [SerializeField] private TextMeshPro _text;
        [SerializeField] private float _fadeInTime = 0.15f;
        [SerializeField] private float _fadeOutTime = 0.5f;
        [SerializeField] private float _maxLifetime = 12f;

        private Transform _follow;
        private Vector3 _offset;
        private float _peakAlpha = 1f;
        private float _elapsed;
        private float _hideTime = -1f;

        private void LateUpdate()
        {
            _elapsed += Time.deltaTime;

            if (_follow)
                transform.position = _follow.position + _offset;

            if (Camera.main)
                transform.rotation = Camera.main.transform.rotation;

            // The number stays until Hide() is called; the lifetime is only a safety net
            if (_hideTime < 0f && _elapsed >= _maxLifetime)
                Hide();

            float alpha = Mathf.Clamp01(_elapsed / _fadeInTime);
            if (_hideTime >= 0f)
                alpha = Mathf.Min(alpha, 1f - (_elapsed - _hideTime) / _fadeOutTime);

            _text.alpha = Mathf.Clamp01(alpha) * _peakAlpha;

            if (_hideTime >= 0f && _elapsed >= _hideTime + _fadeOutTime)
                Destroy(gameObject);
        }

        // With a follow target the offset is relative to it; without one it is the world position
        public void Begin(string text, Transform follow, Vector3 offset, float peakAlpha)
        {
            _text.text = text;
            _follow = follow;
            _offset = offset;
            _peakAlpha = peakAlpha;
            transform.position = follow ? follow.position + offset : offset;
            _text.alpha = 0f;
        }

        public void Hide()
        {
            if (_hideTime < 0f)
                _hideTime = _elapsed;
        }
    }
}
