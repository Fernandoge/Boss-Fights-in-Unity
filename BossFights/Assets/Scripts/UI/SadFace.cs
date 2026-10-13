using UnityEngine;

namespace UI
{
    // A sad face that floats above a grieving animal: a yellow disc with eyes, slanted brows, a frown and a tear, drawn into sprites in code
    public static class SadFace
    {
        private const float FaceSize = 1.0f;
        private const float FillShare = 0.88f;
        private const float HeightAboveTarget = 0.5f;
        private const float BobHeight = 0.12f;
        private const float BobSpeed = 2.5f;
        private const float PulseAmount = 0.04f;
        private const float PulseSpeedFactor = 0.8f;
        private const int TextureSize = 128;
        private static readonly Color RimColor = new Color(0.35f, 0.22f, 0.02f);
        private static readonly Color FillColor = new Color(1f, 0.82f, 0.2f);
        private static readonly Color FeatureColor = new Color(0.25f, 0.14f, 0.02f);
        private static readonly Color TearColor = new Color(0.35f, 0.7f, 1f);

        // Shapes in a space where the sprite runs from -1 to 1
        private static readonly Vector2 LeftEye = new Vector2(-0.33f, 0.12f);
        private static readonly Vector2 RightEye = new Vector2(0.33f, 0.12f);
        private const float EyeRadius = 0.11f;
        private static readonly Vector2 LeftBrowOuter = new Vector2(-0.55f, 0.36f);
        private static readonly Vector2 LeftBrowInner = new Vector2(-0.16f, 0.5f);
        private static readonly Vector2 RightBrowInner = new Vector2(0.16f, 0.5f);
        private static readonly Vector2 RightBrowOuter = new Vector2(0.55f, 0.36f);
        private const float BrowRadius = 0.05f;
        private static readonly Vector2 FrownCenter = new Vector2(0f, -0.78f);
        private const float FrownRadius = 0.44f;
        private const float FrownHalfAngle = 55f;
        private const float FrownThickness = 0.06f;
        private static readonly Vector2 TearTop = new Vector2(0.4f, -0.02f);
        private static readonly Vector2 TearBottom = new Vector2(0.4f, -0.24f);
        private const float TearRadius = 0.09f;

        private static Sprite _discSprite;
        private static Sprite _featuresSprite;
        private static Sprite _tearSprite;

        public static TargetMarker Show(Collider target)
        {
            if (!target)
                return null;

            TargetMarker marker = TargetMarker.Create("Sad Face", target, HeightAboveTarget, BobHeight, BobSpeed, PulseAmount, PulseSpeedFactor);
            marker.AddShape("Rim", GetDiscSprite(), RimColor, FaceSize, 98);
            marker.AddShape("Face", GetDiscSprite(), FillColor, FaceSize * FillShare, 99);
            marker.AddShape("Features", GetFeaturesSprite(), FeatureColor, FaceSize * FillShare, 100);
            marker.AddShape("Tear", GetTearSprite(), TearColor, FaceSize * FillShare, 101);
            return marker;
        }

        private static Sprite GetDiscSprite()
        {
            if (!_discSprite)
                _discSprite = Draw(point => point.magnitude - 0.98f);

            return _discSprite;
        }

        private static Sprite GetFeaturesSprite()
        {
            if (!_featuresSprite)
                _featuresSprite = Draw(point => Mathf.Min(
                    Mathf.Min((point - LeftEye).magnitude - EyeRadius, (point - RightEye).magnitude - EyeRadius),
                    Mathf.Min(
                        Mathf.Min(DistanceToSegment(point, LeftBrowOuter, LeftBrowInner), DistanceToSegment(point, RightBrowInner, RightBrowOuter)) - BrowRadius,
                        DistanceToFrown(point) - FrownThickness)));

            return _featuresSprite;
        }

        private static Sprite GetTearSprite()
        {
            if (!_tearSprite)
                _tearSprite = Draw(point => DistanceToSegment(point, TearTop, TearBottom) - TearRadius);

            return _tearSprite;
        }

        // Each pixel is white with an alpha from the signed distance of its shape (negative inside), so the edges are smooth and the renderers tint it
        private static Sprite Draw(System.Func<Vector2, float> signedDistance)
        {
            Texture2D texture = new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            for (int y = 0; y < TextureSize; y++)
            {
                for (int x = 0; x < TextureSize; x++)
                {
                    Vector2 point = new Vector2((x + 0.5f) / TextureSize * 2f - 1f, (y + 0.5f) / TextureSize * 2f - 1f);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(0.5f - signedDistance(point) * TextureSize / 2f)));
                }
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, TextureSize, TextureSize), new Vector2(0.5f, 0.5f), TextureSize);
        }

        // The frown is the top part of a circle (a mouth turned down)
        private static float DistanceToFrown(Vector2 point)
        {
            Vector2 offset = point - FrownCenter;
            float angle = Mathf.Atan2(offset.y, offset.x) * Mathf.Rad2Deg;
            if (Mathf.Abs(angle - 90f) <= FrownHalfAngle)
                return Mathf.Abs(offset.magnitude - FrownRadius);

            Vector2 left = FrownCenter + new Vector2(Mathf.Cos((90f + FrownHalfAngle) * Mathf.Deg2Rad), Mathf.Sin((90f + FrownHalfAngle) * Mathf.Deg2Rad)) * FrownRadius;
            Vector2 right = FrownCenter + new Vector2(Mathf.Cos((90f - FrownHalfAngle) * Mathf.Deg2Rad), Mathf.Sin((90f - FrownHalfAngle) * Mathf.Deg2Rad)) * FrownRadius;
            return Mathf.Min((point - left).magnitude, (point - right).magnitude);
        }

        private static float DistanceToSegment(Vector2 point, Vector2 a, Vector2 b)
        {
            Vector2 edge = b - a;
            float along = Mathf.Clamp01(Vector2.Dot(point - a, edge) / edge.sqrMagnitude);
            return (point - (a + edge * along)).magnitude;
        }
    }
}
