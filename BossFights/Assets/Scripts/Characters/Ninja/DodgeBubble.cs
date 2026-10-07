using UnityEngine;

namespace Characters.Ninja
{
    // A glowing bubble around the ninja while the dodge makes them immune: it pops in, pulses, and flickers out at the end so the player sees the immunity running out
    public class DodgeBubble : MonoBehaviour
    {
        [SerializeField] private Renderer _renderer;
        [SerializeField] private float _radius = 1.5f;
        [SerializeField] private float _growTime = 0.12f;
        [SerializeField] private float _fadeTime = 0.3f;
        [SerializeField] private float _pulseSpeed = 12f;

        private static readonly int Intensity = Shader.PropertyToID("_Intensity");

        private MaterialPropertyBlock _block;
        private float _duration;
        private float _elapsed;

        private void Update()
        {
            _elapsed += Time.deltaTime;
            if (_elapsed >= _duration)
            {
                Destroy(gameObject);
                return;
            }

            float grow = Mathf.Clamp01(_elapsed / _growTime);
            transform.localScale = Vector3.one * (_radius * 2f * (1f - (1f - grow) * (1f - grow)));

            float remaining = _duration - _elapsed;
            float intensity = 0.85f + 0.15f * Mathf.Sin(_elapsed * _pulseSpeed);
            if (remaining < _fadeTime)
                intensity *= remaining / _fadeTime * (Mathf.Sin(_elapsed * 60f) > 0f ? 1f : 0.35f);

            _renderer.GetPropertyBlock(_block);
            _block.SetFloat(Intensity, intensity);
            _renderer.SetPropertyBlock(_block);
        }

        public void Begin(float duration)
        {
            _duration = duration;
            _block = new MaterialPropertyBlock();
            transform.localPosition = Vector3.up;
            transform.localScale = Vector3.zero;
        }
    }
}
