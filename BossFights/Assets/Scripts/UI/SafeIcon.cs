using UnityEngine;

namespace UI
{
    // The mark above the stone that hides no slime: a white check mark on a green disc, pulsing at the pace of the slime warning next to it
    public static class SafeIcon
    {
        private const float BadgeSize = 1.9f;
        private const float FillShare = 0.84f;
        private const float CheckShare = 0.62f;
        private const float HeightAboveTarget = 0.9f;
        private const float BobHeight = 0.12f;
        private const float BobSpeed = 6f;
        private const float PulseAmount = 0.12f;
        private const float PulseSpeedFactor = 2f;
        private const int TextureSize = 128;
        private static readonly Color RimColor = new Color(0.02f, 0.2f, 0.06f);
        private static readonly Color FillColor = new Color(0.12f, 0.72f, 0.26f);
        private static readonly Color CheckColor = new Color(1f, 0.99f, 0.95f);

        // The check mark is two thick strokes in a square from -1 to 1: a short one down to the bottom, a long one up to the right
        private static readonly Vector2 StrokeStart = new Vector2(-0.52f, -0.02f);
        private static readonly Vector2 StrokeBottom = new Vector2(-0.16f, -0.4f);
        private static readonly Vector2 StrokeEnd = new Vector2(0.55f, 0.42f);
        private const float StrokeRadius = 0.13f;

        private static Sprite _checkSprite;

        public static TargetMarker Show(Collider target, float heightAboveTarget = HeightAboveTarget)
        {
            if (!target)
                return null;

            TargetMarker marker = TargetMarker.Create("Safe Icon", target, heightAboveTarget, BobHeight, BobSpeed, PulseAmount, PulseSpeedFactor);
            marker.AddShape("Rim", SlimeWarning.GetDiscSprite(), RimColor, BadgeSize, 98);
            marker.AddShape("Fill", SlimeWarning.GetDiscSprite(), FillColor, BadgeSize * FillShare, 99);
            marker.AddShape("Check", GetCheckSprite(), CheckColor, BadgeSize * CheckShare, 100);
            return marker;
        }

        // A white check mark, centred, that the renderer tints
        private static Sprite GetCheckSprite()
        {
            if (_checkSprite)
                return _checkSprite;

            Texture2D texture = new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            for (int y = 0; y < TextureSize; y++)
            {
                for (int x = 0; x < TextureSize; x++)
                {
                    Vector2 point = new Vector2((x + 0.5f) / TextureSize * 2f - 1f, (y + 0.5f) / TextureSize * 2f - 1f);
                    float distance = Mathf.Min(DistanceToSegment(point, StrokeStart, StrokeBottom), DistanceToSegment(point, StrokeBottom, StrokeEnd)) - StrokeRadius;
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(0.5f - distance * TextureSize / 2f)));
                }
            }

            texture.Apply();
            _checkSprite = Sprite.Create(texture, new Rect(0f, 0f, TextureSize, TextureSize), new Vector2(0.5f, 0.5f), TextureSize);
            return _checkSprite;
        }

        private static float DistanceToSegment(Vector2 point, Vector2 a, Vector2 b)
        {
            Vector2 edge = b - a;
            float along = Mathf.Clamp01(Vector2.Dot(point - a, edge) / edge.sqrMagnitude);
            return (point - (a + edge * along)).magnitude;
        }
    }
}
