using UnityEngine;

namespace UI
{
    // A blue bubble with a question mark that floats above an animal that does not understand what just happened. Drawn in code like the other signs
    public static class QuestionMark
    {
        private const float BubbleSize = 1.5f;
        private const float FillShare = 0.84f;
        private const float LetterFontSize = 9f;
        private const float HeightAboveTarget = 0.95f;
        private const float BobHeight = 0.15f;
        private const float BobSpeed = 4f;
        private const float PulseAmount = 0.08f;
        private const float PulseSpeedFactor = 1.5f;
        private const int TextureSize = 128;
        private static readonly Color RimColor = new Color(0.15f, 0.4f, 0.95f);
        private static readonly Color FillColor = new Color(0.97f, 0.97f, 1f);
        private static readonly Color LetterColor = new Color(0.1f, 0.3f, 0.85f);

        private static Sprite _discSprite;

        public static TargetMarker Show(Collider target)
        {
            if (!target)
                return null;

            TargetMarker marker = TargetMarker.Create("Question Mark", target, HeightAboveTarget, BobHeight, BobSpeed, PulseAmount, PulseSpeedFactor);
            marker.AddShape("Rim", GetDiscSprite(), RimColor, BubbleSize, 98);
            marker.AddShape("Bubble", GetDiscSprite(), FillColor, BubbleSize * FillShare, 99);
            marker.AddLetter("?", LetterFontSize, LetterColor, 100);
            return marker;
        }

        // A white disc, one world unit wide, tinted by the renderers
        private static Sprite GetDiscSprite()
        {
            if (_discSprite)
                return _discSprite;

            Texture2D texture = new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            float half = TextureSize / 2f;
            for (int y = 0; y < TextureSize; y++)
            {
                for (int x = 0; x < TextureSize; x++)
                {
                    float distance = new Vector2(x + 0.5f - half, y + 0.5f - half).magnitude - (half - 1f);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(0.5f - distance)));
                }
            }

            texture.Apply();
            _discSprite = Sprite.Create(texture, new Rect(0f, 0f, TextureSize, TextureSize), new Vector2(0.5f, 0.5f), TextureSize);
            return _discSprite;
        }
    }
}
