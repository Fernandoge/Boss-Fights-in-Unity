using UnityEngine;

namespace Shared
{
    // The ring that bursts across the floor where a heavy attack lands, as wide as the area it hits. Built in code like DamageNumber, so nothing is wired in a scene
    public class GroundSplash : MonoBehaviour
    {
        private const float GroundOffset = 0.06f;
        private const float StartShare = 0.3f;
        private const int TextureSize = 128;
        private const float RimRadius = 0.82f;
        private const float RimThickness = 0.18f;
        private const float FillStrength = 0.18f;

        private static Sprite _ringSprite;

        private SpriteRenderer _spriteRenderer;
        private Color _color;
        private float _diameter;
        private float _duration;
        private float _elapsed;

        private void Update()
        {
            _elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(_elapsed / _duration);

            // The ring opens fast and slows as it dies out
            float eased = 1f - (1f - progress) * (1f - progress);
            transform.localScale = Vector3.one * (_diameter * Mathf.Lerp(StartShare, 1f, eased));
            _spriteRenderer.color = new Color(_color.r, _color.g, _color.b, _color.a * (1f - progress));

            if (_elapsed >= _duration)
                Destroy(gameObject);
        }

        public static GroundSplash Show(Vector3 center, float radius, Color color, float duration)
        {
            GameObject splashObject = new GameObject("Ground Splash");
            splashObject.transform.SetPositionAndRotation(
                new Vector3(center.x, center.y + GroundOffset, center.z),
                Quaternion.Euler(90f, 0f, 0f));

            GroundSplash splash = splashObject.AddComponent<GroundSplash>();
            splash._diameter = radius * 2f;
            splash._duration = Mathf.Max(duration, 0.01f);
            splash._color = color;
            splash.Build();
            return splash;
        }

        private void Build()
        {
            _spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            _spriteRenderer.sprite = GetRingSprite();
            _spriteRenderer.color = _color;
            transform.localScale = Vector3.one * (_diameter * StartShare);
        }

        // A white ring with a soft edge and a faint filled centre, one world unit wide, tinted by the renderer
        private static Sprite GetRingSprite()
        {
            if (_ringSprite)
                return _ringSprite;

            Texture2D texture = new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            float half = TextureSize / 2f;
            for (int y = 0; y < TextureSize; y++)
            {
                for (int x = 0; x < TextureSize; x++)
                {
                    float distance = new Vector2(x + 0.5f - half, y + 0.5f - half).magnitude / half;
                    float rim = Mathf.Clamp01(1f - Mathf.Abs(distance - RimRadius) / RimThickness);
                    float fill = Mathf.Clamp01(1f - distance / RimRadius) * FillStrength;
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, distance > 1f ? 0f : Mathf.Clamp01(rim * rim + fill)));
                }
            }

            texture.Apply();
            _ringSprite = Sprite.Create(texture, new Rect(0f, 0f, TextureSize, TextureSize), new Vector2(0.5f, 0.5f), TextureSize);
            return _ringSprite;
        }
    }
}
