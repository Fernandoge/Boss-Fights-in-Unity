using UnityEngine;

namespace UI
{
    // The warning sign above a boss that is casting a skill that takes two or more hearts: a yellow triangle with a red rim and an exclamation mark, flashing faster than the counter prompt so the two are never confused
    public static class DangerIcon
    {
        private const float SignSize = 2.4f;
        private const float FillShare = 0.72f;
        private const float MarkShare = 0.33f;
        private const float HeightAboveTarget = 1.3f;
        private const float BobHeight = 0.12f;
        private const float BobSpeed = 6f;
        private const float PulseAmount = 0.12f;
        private const float PulseSpeedFactor = 2f;
        private const int TextureSize = 128;
        private const float TriangleRadius = 0.9f;
        private const float CornerRounding = 0.12f;
        private static readonly Color RimColor = new Color(0.9f, 0.08f, 0.08f);
        private static readonly Color FillColor = new Color(1f, 0.85f, 0.12f);
        private static readonly Color MarkColor = new Color(0.1f, 0.02f, 0.02f);

        // The exclamation mark is a bar and a dot drawn into a sprite (not a font letter), so it is centred by construction; the whole mark is centred on the middle of the sprite
        private static readonly Vector2 BarTop = new Vector2(0f, 0.47f);
        private static readonly Vector2 BarBottom = new Vector2(0f, -0.13f);
        private static readonly Vector2 DotCenter = new Vector2(0f, -0.45f);
        private const float BarRadius = 0.1f;
        private const float DotRadius = 0.12f;

        private static Sprite _triangleSprite;
        private static Sprite _markSprite;

        public static TargetMarker Show(Collider target)
        {
            if (!target)
                return null;

            TargetMarker marker = TargetMarker.Create("Danger Icon", target, HeightAboveTarget, BobHeight, BobSpeed, PulseAmount, PulseSpeedFactor);
            marker.AddShape("Rim", GetTriangleSprite(), RimColor, SignSize, 98);
            marker.AddShape("Fill", GetTriangleSprite(), FillColor, SignSize * FillShare, 99);
            marker.AddShape("Mark", GetMarkSprite(), MarkColor, SignSize * MarkShare, 100);
            return marker;
        }

        // A white exclamation mark, centred, that the renderer tints
        private static Sprite GetMarkSprite()
        {
            if (_markSprite)
                return _markSprite;

            Texture2D texture = new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            for (int y = 0; y < TextureSize; y++)
            {
                for (int x = 0; x < TextureSize; x++)
                {
                    Vector2 point = new Vector2((x + 0.5f) / TextureSize * 2f - 1f, (y + 0.5f) / TextureSize * 2f - 1f);
                    float distance = Mathf.Min(DistanceToSegment(point, BarTop, BarBottom) - BarRadius, (point - DotCenter).magnitude - DotRadius);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(0.5f - distance * TextureSize / 2f)));
                }
            }

            texture.Apply();
            _markSprite = Sprite.Create(texture, new Rect(0f, 0f, TextureSize, TextureSize), new Vector2(0.5f, 0.5f), TextureSize);
            return _markSprite;
        }

        // A white triangle with rounded corners, apex up, one world unit wide and tall; the renderers tint it
        private static Sprite GetTriangleSprite()
        {
            if (_triangleSprite)
                return _triangleSprite;

            // The corners are rounded by drawing a smaller triangle and thickening it by the rounding
            float inset = TriangleRadius - CornerRounding * 2f;
            Vector2 apex = new Vector2(0f, inset);
            Vector2 left = new Vector2(-inset * 0.866f, -inset * 0.5f);
            Vector2 right = new Vector2(inset * 0.866f, -inset * 0.5f);

            Texture2D texture = new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            float span = 1.05f;
            for (int y = 0; y < TextureSize; y++)
            {
                for (int x = 0; x < TextureSize; x++)
                {
                    Vector2 point = new Vector2((x + 0.5f) / TextureSize * 2f - 1f, (y + 0.5f) / TextureSize * 2f - 1f) * span;
                    float distance = SignedDistance(point, apex, left, right) - CornerRounding;
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(0.5f - distance * TextureSize / (2f * span))));
                }
            }

            texture.Apply();
            _triangleSprite = Sprite.Create(texture, new Rect(0f, 0f, TextureSize, TextureSize), new Vector2(0.5f, 0.5f), TextureSize);
            return _triangleSprite;
        }

        // Negative inside the triangle, positive outside
        private static float SignedDistance(Vector2 point, Vector2 a, Vector2 b, Vector2 c)
        {
            float distance = Mathf.Min(DistanceToSegment(point, a, b), Mathf.Min(DistanceToSegment(point, b, c), DistanceToSegment(point, c, a)));
            bool sideAb = Cross(a, b, point) >= 0f;
            bool sideBc = Cross(b, c, point) >= 0f;
            bool sideCa = Cross(c, a, point) >= 0f;
            bool inside = sideAb == sideBc && sideBc == sideCa;
            return inside ? -distance : distance;
        }

        private static float Cross(Vector2 from, Vector2 to, Vector2 point) => (to.x - from.x) * (point.y - from.y) - (to.y - from.y) * (point.x - from.x);

        private static float DistanceToSegment(Vector2 point, Vector2 a, Vector2 b)
        {
            Vector2 edge = b - a;
            float along = Mathf.Clamp01(Vector2.Dot(point - a, edge) / edge.sqrMagnitude);
            return (point - (a + edge * along)).magnitude;
        }
    }
}
