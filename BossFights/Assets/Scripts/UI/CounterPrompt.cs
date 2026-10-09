using TMPro;
using UnityEngine;

namespace UI
{
    // A key cap that floats above something the player can kick or counter, shown only while its counter window is open; it is built in code, so nothing needs to be wired in a scene
    public class CounterPrompt : MonoBehaviour
    {
        private const string KeyLabel = "E";
        private const float KeySize = 1.7f;
        private const float CapShare = 0.82f;
        private const float LetterFontSize = 10f;
        private const float HeightAboveTarget = 1.1f;
        private const float BobHeight = 0.2f;
        private const float BobSpeed = 4f;
        private const float PulseAmount = 0.07f;
        private const float PopTime = 0.12f;
        private const int TextureSize = 128;
        private const float CornerRadius = 30f;
        private static readonly Color RimColor = new Color(0.3f, 1f, 0.35f);
        private static readonly Color CapColor = new Color(0.97f, 0.97f, 0.92f);
        private static readonly Color LetterColor = new Color(0.08f, 0.08f, 0.08f);

        private static Sprite _keySprite;

        private Collider _target;
        private float _elapsed;
        private float _hideElapsed;
        private bool _isHiding;

        private void LateUpdate()
        {
            if (!_target || !_target.gameObject.activeInHierarchy)
            {
                Destroy(gameObject);
                return;
            }

            _elapsed += Time.deltaTime;
            float popScale = Mathf.Clamp01(_elapsed / PopTime);
            if (_isHiding)
            {
                _hideElapsed += Time.deltaTime;
                popScale = Mathf.Min(popScale, 1f - _hideElapsed / PopTime);
                if (popScale <= 0f)
                {
                    Destroy(gameObject);
                    return;
                }
            }

            Bounds bounds = _target.bounds;
            Vector3 position = new Vector3(bounds.center.x, bounds.max.y + HeightAboveTarget, bounds.center.z);
            position.y += Mathf.Sin(_elapsed * BobSpeed) * BobHeight;
            transform.position = position;

            Camera mainCamera = Camera.main;
            if (mainCamera)
                transform.rotation = mainCamera.transform.rotation;

            float pulse = 1f + Mathf.Sin(_elapsed * BobSpeed * 1.5f) * PulseAmount;
            transform.localScale = Vector3.one * (pulse * popScale);
        }

        public void Hide() => _isHiding = true;

        // target is the collider of whatever can be countered; the prompt sits above its bounds and follows it
        public static CounterPrompt Show(Collider target)
        {
            if (!target)
                return null;

            GameObject promptObject = new GameObject("Counter Prompt");
            CounterPrompt prompt = promptObject.AddComponent<CounterPrompt>();
            prompt._target = target;

            AddKeyShape(promptObject.transform, "Rim", RimColor, KeySize, 98);
            AddKeyShape(promptObject.transform, "Cap", CapColor, KeySize * CapShare, 99);

            GameObject letterObject = new GameObject("Letter");
            letterObject.transform.SetParent(promptObject.transform, false);
            TextMeshPro letter = letterObject.AddComponent<TextMeshPro>();
            letter.text = KeyLabel;
            letter.fontSize = LetterFontSize;
            letter.fontStyle = FontStyles.Bold;
            letter.alignment = TextAlignmentOptions.Center;
            letter.textWrappingMode = TextWrappingModes.NoWrap;
            letter.color = LetterColor;
            letter.sortingOrder = 100;

            promptObject.transform.localScale = Vector3.zero;
            return prompt;
        }

        private static void AddKeyShape(Transform parent, string shapeName, Color color, float size, int sortingOrder)
        {
            GameObject shapeObject = new GameObject(shapeName);
            shapeObject.transform.SetParent(parent, false);
            shapeObject.transform.localScale = Vector3.one * size;

            SpriteRenderer shape = shapeObject.AddComponent<SpriteRenderer>();
            shape.sprite = GetKeySprite();
            shape.color = color;
            shape.sortingOrder = sortingOrder;
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
