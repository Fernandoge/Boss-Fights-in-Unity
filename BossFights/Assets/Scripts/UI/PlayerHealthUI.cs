using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    // The player's hearts: every child Image is drawn as a heart, red while it is health and grey once it is lost; the heart shape is generated in code, so no art is needed
    public class PlayerHealthUI : MonoBehaviour
    {
        private const int TextureSize = 128;
        private const int SamplesPerSide = 4;
        private const float ShapeScale = 1.2f;
        private const float ShapeShiftY = 0.08f;

        [SerializeField] private Color _fullHeartColor = new Color(0.86f, 0.08f, 0.15f);
        [SerializeField] private Color _emptyHeartColor = new Color(0.55f, 0.55f, 0.6f, 0.55f);

        private static Sprite _heartSprite;

        private Image[] _hearts;

        private void Awake()
        {
            _hearts = GetComponentsInChildren<Image>(true);
            foreach (Image heart in _hearts)
            {
                heart.sprite = GetHeartSprite();
                heart.preserveAspect = true;
            }
        }

        public void UpdateHearts(int currentHealth)
        {
            for (int i = 0; i < _hearts.Length; i++)
                _hearts[i].color = i < currentHealth ? _fullHeartColor : _emptyHeartColor;
        }

        // A white heart filling the sprite, tinted by the Image; each pixel is sampled several times so the edge is smooth
        private static Sprite GetHeartSprite()
        {
            if (_heartSprite)
                return _heartSprite;

            Texture2D texture = new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            for (int y = 0; y < TextureSize; y++)
            {
                for (int x = 0; x < TextureSize; x++)
                {
                    int inside = 0;
                    for (int sampleY = 0; sampleY < SamplesPerSide; sampleY++)
                    {
                        for (int sampleX = 0; sampleX < SamplesPerSide; sampleX++)
                        {
                            float u = (x + (sampleX + 0.5f) / SamplesPerSide) / TextureSize * 2f - 1f;
                            float v = (y + (sampleY + 0.5f) / SamplesPerSide) / TextureSize * 2f - 1f;
                            if (IsInsideHeart(u * ShapeScale, v * ShapeScale + ShapeShiftY))
                                inside++;
                        }
                    }

                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, inside / (float)(SamplesPerSide * SamplesPerSide)));
                }
            }

            texture.Apply();
            _heartSprite = Sprite.Create(texture, new Rect(0f, 0f, TextureSize, TextureSize), new Vector2(0.5f, 0.5f), TextureSize);
            return _heartSprite;
        }

        // The classic heart curve (x^2 + y^2 - 1)^3 = x^2 * y^3
        private static bool IsInsideHeart(float x, float y)
        {
            float squareSum = x * x + y * y - 1f;
            return squareSum * squareSum * squareSum - x * x * y * y * y <= 0f;
        }
    }
}
