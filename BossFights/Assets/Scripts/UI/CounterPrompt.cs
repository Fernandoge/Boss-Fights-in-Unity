using UnityEngine;

namespace UI
{
    // The key cap above something the player can kick or counter, shown only while its counter window is open
    public static class CounterPrompt
    {
        private const string KeyLabel = "E";
        private const float KeySize = 1.36f;
        private const float CapShare = 0.82f;
        private const float LetterFontSize = 8f;
        private const float HeightAboveTarget = 0.95f;
        private const float BobHeight = 0.2f;
        private const float BobSpeed = 4f;
        private const float PulseAmount = 0.07f;
        private const float PulseSpeedFactor = 1.5f;
        private const int TextureSize = 128;
        private const float CornerRadius = 30f;
        private static readonly Color RimColor = new Color(0.3f, 1f, 0.35f);
        private static readonly Color CapColor = new Color(0.97f, 0.97f, 0.92f);
        private static readonly Color LetterColor = new Color(0.08f, 0.08f, 0.08f);

        private static Sprite _keySprite;

        public static TargetMarker Show(Collider target)
        {
            if (!target)
                return null;

            TargetMarker marker = TargetMarker.Create("Counter Prompt", target, HeightAboveTarget, BobHeight, BobSpeed, PulseAmount, PulseSpeedFactor);
            marker.AddShape("Rim", GetKeySprite(), RimColor, KeySize, 98);
            marker.AddShape("Cap", GetKeySprite(), CapColor, KeySize * CapShare, 99);
            marker.AddLetter(KeyLabel, LetterFontSize, LetterColor, 100);
            return marker;
        }

        // A white rounded square, one world unit wide, tinted by the renderers
        private static Sprite GetKeySprite()
        {
            if (_keySprite)
                return _keySprite;

            Texture2D texture = new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            float half = TextureSize / 2f;
            for (int y = 0; y < TextureSize; y++)
            {
                for (int x = 0; x < TextureSize; x++)
                {
                    Vector2 corner = new Vector2(Mathf.Abs(x + 0.5f - half), Mathf.Abs(y + 0.5f - half)) - Vector2.one * (half - CornerRadius);
                    float distance = Vector2.Max(corner, Vector2.zero).magnitude + Mathf.Min(Mathf.Max(corner.x, corner.y), 0f) - CornerRadius;
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(0.5f - distance)));
                }
            }

            texture.Apply();
            _keySprite = Sprite.Create(texture, new Rect(0f, 0f, TextureSize, TextureSize), new Vector2(0.5f, 0.5f), TextureSize);
            return _keySprite;
        }
    }
}
