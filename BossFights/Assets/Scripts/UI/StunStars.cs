using UnityEngine;

namespace UI
{
    // The classic dizzy stars: a ring of yellow stars that circles above the head of a stunned animal. The sprite is drawn in code, so nothing needs to be wired in a scene
    public static class StunStars
    {
        private const int StarCount = 4;
        private const float StarSize = 1f;
        private const float OrbitRadius = 1.4f;
        private const float OrbitFlatten = 0.28f;
        private const float OrbitSpeed = 230f;
        private const float HeightAboveTarget = 0.15f;
        private const float BobHeight = 0.08f;
        private const float BobSpeed = 3f;
        private const int TextureSize = 128;
        private const float StarOuterRadius = 0.95f;
        private const float StarInnerRadius = 0.4f;
        private const float OutlineThickness = 0.14f;
        private static readonly Color FillColor = new Color(1f, 0.88f, 0.12f);
        private static readonly Color OutlineColor = new Color(0.9f, 0.5f, 0.05f);

        private static Sprite _starSprite;

        public static TargetMarker Show(Collider target)
        {
            if (!target)
                return null;

            TargetMarker marker = TargetMarker.Create("Stun Stars", target, HeightAboveTarget, BobHeight, BobSpeed, 0f, 1f);
            for (int i = 0; i < StarCount; i++)
                marker.AddShape("Star", GetStarSprite(), Color.white, StarSize, 100);

            marker.gameObject.AddComponent<StunStarOrbit>().Setup(OrbitRadius, OrbitFlatten, OrbitSpeed);
            return marker;
        }

        // A five-point star with a darker outline; the colours are baked in, so the renderers stay white
        private static Sprite GetStarSprite()
        {
            if (_starSprite)
                return _starSprite;

            Vector2[] corners = new Vector2[10];
            for (int i = 0; i < corners.Length; i++)
            {
                float angle = (90f + i * 36f) * Mathf.Deg2Rad;
                float radius = i % 2 == 0 ? StarOuterRadius : StarInnerRadius;
                corners[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
            }

            Texture2D texture = new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            for (int y = 0; y < TextureSize; y++)
            {
                for (int x = 0; x < TextureSize; x++)
                {
                    Vector2 point = new Vector2((x + 0.5f) / TextureSize * 2f - 1f, (y + 0.5f) / TextureSize * 2f - 1f);
                    float signedDistance = SignedDistance(point, corners);
                    float depth = -signedDistance;
                    Color color = Color.Lerp(FillColor, OutlineColor, Mathf.Clamp01((OutlineThickness + 0.04f - depth) / 0.04f));
                    color.a = Mathf.Clamp01(0.5f - signedDistance * TextureSize / 2f);
                    texture.SetPixel(x, y, color);
                }
            }

            texture.Apply();
            _starSprite = Sprite.Create(texture, new Rect(0f, 0f, TextureSize, TextureSize), new Vector2(0.5f, 0.5f), TextureSize);
            return _starSprite;
        }

        // Negative inside the polygon, positive outside
        private static float SignedDistance(Vector2 point, Vector2[] corners)
        {
            float distance = float.MaxValue;
            bool isInside = false;
            for (int i = 0; i < corners.Length; i++)
            {
                Vector2 a = corners[i];
                Vector2 b = corners[(i + 1) % corners.Length];
                distance = Mathf.Min(distance, DistanceToSegment(point, a, b));

                // Even-odd test: a ray towards +x crosses this edge
                if ((a.y > point.y) != (b.y > point.y) && point.x < (b.x - a.x) * (point.y - a.y) / (b.y - a.y) + a.x)
                    isInside = !isInside;
            }

            return isInside ? -distance : distance;
        }

        private static float DistanceToSegment(Vector2 point, Vector2 a, Vector2 b)
        {
            Vector2 edge = b - a;
            float along = Mathf.Clamp01(Vector2.Dot(point - a, edge) / edge.sqrMagnitude);
            return (point - (a + edge * along)).magnitude;
        }
    }
}
