using UnityEngine;

namespace UI
{
    // Three dark bars above Gorath that turn red, one for every skill he casts; with all of them red the next skill is the Cataclysm, and they go dark again once it starts. The marker is made in code, so nothing is wired in a scene
    public class CataclysmBars : MonoBehaviour
    {
        private const float BarWidth = 0.42f;
        private const float BarHeight = 1.15f;
        private const float BarSpacing = 0.34f;
        private const float FillShare = 0.78f;
        private const float HeightAboveTarget = 1.3f;
        private const float PunchTime = 0.3f;
        private const float PunchScale = 0.5f;
        private const float WarnPulseSpeed = 8f;
        private const int TextureWidth = 40;
        private const int TextureHeight = 128;
        private const float SpriteAspect = (float)TextureWidth / TextureHeight;
        private static readonly Color DarkRimColor = new Color(0.02f, 0.02f, 0.03f, 0.95f);
        private static readonly Color DarkFillColor = new Color(0.1f, 0.1f, 0.12f, 0.95f);
        private static readonly Color RedRimColor = new Color(0.4f, 0f, 0f, 1f);
        private static readonly Color RedFillColor = new Color(0.95f, 0.08f, 0.06f, 1f);
        private static readonly Color RedBrightColor = new Color(1f, 0.45f, 0.3f, 1f);

        private static Sprite _barSprite;

        private TargetMarker _marker;
        private SpriteRenderer[] _rims;
        private SpriteRenderer[] _fills;
        private float[] _punchElapsed;
        private int _filled;

        private void Update()
        {
            bool allRed = _filled == _fills.Length;
            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * WarnPulseSpeed);

            for (int i = 0; i < _fills.Length; i++)
            {
                _punchElapsed[i] += Time.deltaTime;
                float punch = 1f + PunchScale * Mathf.Clamp01(1f - _punchElapsed[i] / PunchTime);
                _fills[i].transform.parent.localScale = new Vector3(punch, punch, 1f);

                if (allRed)
                    _fills[i].color = Color.Lerp(RedFillColor, RedBrightColor, pulse);
            }
        }

        // They vanish at once, without the marker's pop-out, so they never share the screen with the danger icon of the next skill
        public void Hide() => Destroy(_marker.gameObject);

        // filled is how many skills the boss has cast since the last Cataclysm
        public void SetFilled(int filled)
        {
            filled = Mathf.Clamp(filled, 0, _fills.Length);
            if (filled > _filled)
                _punchElapsed[filled - 1] = 0f;

            _filled = filled;
            for (int i = 0; i < _fills.Length; i++)
            {
                bool isRed = i < _filled;
                _rims[i].color = isRed ? RedRimColor : DarkRimColor;
                _fills[i].color = isRed ? RedFillColor : DarkFillColor;
            }
        }

        public static CataclysmBars Show(Collider target, int barCount, int filled)
        {
            if (!target)
                return null;

            TargetMarker marker = TargetMarker.Create("Cataclysm Bars", target, HeightAboveTarget, 0f, 1f, 0f, 1f);
            CataclysmBars bars = marker.gameObject.AddComponent<CataclysmBars>();
            bars.Build(marker, barCount);
            bars.SetFilled(filled);
            return bars;
        }

        private void Build(TargetMarker marker, int barCount)
        {
            _marker = marker;
            _rims = new SpriteRenderer[barCount];
            _fills = new SpriteRenderer[barCount];
            _punchElapsed = new float[barCount];

            float totalWidth = barCount * BarWidth + (barCount - 1) * BarSpacing;
            for (int i = 0; i < barCount; i++)
            {
                float x = -totalWidth / 2f + BarWidth / 2f + i * (BarWidth + BarSpacing);
                Vector3 position = new Vector3(x, 0f, 0f);

                // Each bar is a rim and a fill under one object, so a punch scales both together
                GameObject barObject = new GameObject("Bar " + (i + 1));
                barObject.transform.SetParent(marker.transform, false);
                barObject.transform.localPosition = position;

                _rims[i] = AddLayer(barObject.transform, "Rim", DarkRimColor, 1f, 98);
                _fills[i] = AddLayer(barObject.transform, "Fill", DarkFillColor, FillShare, 99);
                _punchElapsed[i] = PunchTime;
            }
        }

        private static SpriteRenderer AddLayer(Transform parent, string layerName, Color color, float sizeShare, int sortingOrder)
        {
            GameObject layerObject = new GameObject(layerName);
            layerObject.transform.SetParent(parent, false);
            layerObject.transform.localScale = new Vector3(BarWidth * sizeShare / SpriteAspect, BarHeight * sizeShare, 1f);

            SpriteRenderer layer = layerObject.AddComponent<SpriteRenderer>();
            layer.sprite = GetBarSprite();
            layer.color = color;
            layer.sortingOrder = sortingOrder;
            return layer;
        }

        // A white rounded vertical bar, one world unit tall once scaled, with a soft edge; the renderers tint it
        private static Sprite GetBarSprite()
        {
            if (_barSprite)
                return _barSprite;

            Texture2D texture = new Texture2D(TextureWidth, TextureHeight, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            float halfWidth = TextureWidth / 2f;
            float halfHeight = TextureHeight / 2f;
            float rounding = halfWidth * 0.7f;
            for (int y = 0; y < TextureHeight; y++)
            {
                for (int x = 0; x < TextureWidth; x++)
                {
                    Vector2 corner = new Vector2(Mathf.Abs(x + 0.5f - halfWidth), Mathf.Abs(y + 0.5f - halfHeight)) - new Vector2(halfWidth - rounding, halfHeight - rounding);
                    float distance = Vector2.Max(corner, Vector2.zero).magnitude + Mathf.Min(Mathf.Max(corner.x, corner.y), 0f) - rounding;
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(0.5f - distance)));
                }
            }

            texture.Apply();
            _barSprite = Sprite.Create(texture, new Rect(0f, 0f, TextureWidth, TextureHeight), new Vector2(0.5f, 0.5f), TextureHeight);
            return _barSprite;
        }
    }
}
