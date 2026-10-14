using UnityEngine;

namespace UI
{
    // The warning above an intermission slime: a white skull on a red disc, flashing as fast as the danger icon. It says the slime kills on touch, and it shows only while the slime is in plain view (never over a stone, so the shell game stays fair)
    public static class SlimeWarning
    {
        private const float BadgeSize = 1.9f;
        private const float FillShare = 0.84f;
        private const float SkullShare = 0.6f;
        private const float HeightAboveTarget = 0.9f;
        private const float BobHeight = 0.12f;
        private const float BobSpeed = 6f;
        private const float PulseAmount = 0.12f;
        private const float PulseSpeedFactor = 2f;
        private const int TextureSize = 128;
        private static readonly Color RimColor = new Color(0.25f, 0.02f, 0.04f);
        private static readonly Color FillColor = new Color(0.88f, 0.1f, 0.12f);
        private static readonly Color SkullColor = new Color(1f, 0.97f, 0.9f);

        // The skull is drawn in a square from -1 to 1: a round cranium and a rounded jaw, with two eye holes, a nose hole and teeth slits cut out
        private static readonly Vector2 CraniumCenter = new Vector2(0f, 0.1f);
        private const float CraniumRadius = 0.62f;
        private static readonly Vector2 JawCenter = new Vector2(0f, -0.45f);
        private static readonly Vector2 JawHalfSize = new Vector2(0.33f, 0.22f);
        private const float JawRounding = 0.08f;
        private static readonly Vector2 LeftEyeCenter = new Vector2(-0.26f, 0.06f);
        private static readonly Vector2 RightEyeCenter = new Vector2(0.26f, 0.06f);
        private const float EyeRadius = 0.2f;
        private static readonly Vector2 NoseCenter = new Vector2(0f, -0.2f);
        private const float NoseRadius = 0.07f;
        private const float TeethSlitX = 0.15f;
        private const float TeethSlitHalfWidth = 0.025f;
        private const float TeethSlitTop = -0.38f;
        private const float TeethSlitBottom = -0.7f;

        private static Sprite _discSprite;
        private static Sprite _skullSprite;

        public static TargetMarker Show(Collider target, float heightAboveTarget = HeightAboveTarget)
        {
            if (!target)
                return null;

            TargetMarker marker = TargetMarker.Create("Slime Warning", target, heightAboveTarget, BobHeight, BobSpeed, PulseAmount, PulseSpeedFactor);
            marker.AddShape("Rim", GetDiscSprite(), RimColor, BadgeSize, 98);
            marker.AddShape("Fill", GetDiscSprite(), FillColor, BadgeSize * FillShare, 99);
            marker.AddShape("Skull", GetSkullSprite(), SkullColor, BadgeSize * SkullShare, 100);
            return marker;
        }

        // A white disc one world unit wide that the renderers tint
        internal static Sprite GetDiscSprite()
        {
            if (_discSprite)
                return _discSprite;

            Texture2D texture = new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            for (int y = 0; y < TextureSize; y++)
            {
                for (int x = 0; x < TextureSize; x++)
                {
                    float distance = (GetPoint(x, y) - Vector2.zero).magnitude - 0.97f;
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(0.5f - distance * TextureSize / 2f)));
                }
            }

            texture.Apply();
            _discSprite = Sprite.Create(texture, new Rect(0f, 0f, TextureSize, TextureSize), new Vector2(0.5f, 0.5f), TextureSize);
            return _discSprite;
        }

        // A white skull with transparent eyes, nose and teeth slits, centred, that the renderer tints
        private static Sprite GetSkullSprite()
        {
            if (_skullSprite)
                return _skullSprite;

            Texture2D texture = new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            for (int y = 0; y < TextureSize; y++)
            {
                for (int x = 0; x < TextureSize; x++)
                {
                    Vector2 point = GetPoint(x, y);
                    float distance = Mathf.Min((point - CraniumCenter).magnitude - CraniumRadius, BoxDistance(point - JawCenter, JawHalfSize, JawRounding));
                    distance = Mathf.Max(distance, -((point - LeftEyeCenter).magnitude - EyeRadius));
                    distance = Mathf.Max(distance, -((point - RightEyeCenter).magnitude - EyeRadius));
                    distance = Mathf.Max(distance, -((point - NoseCenter).magnitude - NoseRadius));
                    for (int slit = -1; slit <= 1; slit++)
                    {
                        Vector2 slitCenter = new Vector2(slit * TeethSlitX, (TeethSlitTop + TeethSlitBottom) / 2f);
                        Vector2 slitHalfSize = new Vector2(TeethSlitHalfWidth, (TeethSlitTop - TeethSlitBottom) / 2f);
                        distance = Mathf.Max(distance, -BoxDistance(point - slitCenter, slitHalfSize, 0f));
                    }

                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(0.5f - distance * TextureSize / 2f)));
                }
            }

            texture.Apply();
            _skullSprite = Sprite.Create(texture, new Rect(0f, 0f, TextureSize, TextureSize), new Vector2(0.5f, 0.5f), TextureSize);
            return _skullSprite;
        }

        private static Vector2 GetPoint(int x, int y) => new Vector2((x + 0.5f) / TextureSize * 2f - 1f, (y + 0.5f) / TextureSize * 2f - 1f);

        // Signed distance to a box with rounded corners, negative inside
        internal static float BoxDistance(Vector2 point, Vector2 halfSize, float rounding)
        {
            Vector2 corner = new Vector2(Mathf.Abs(point.x), Mathf.Abs(point.y)) - (halfSize - Vector2.one * rounding);
            return Vector2.Max(corner, Vector2.zero).magnitude + Mathf.Min(Mathf.Max(corner.x, corner.y), 0f) - rounding;
        }
    }
}
