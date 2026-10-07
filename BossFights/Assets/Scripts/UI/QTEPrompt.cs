using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class QTEPrompt : MonoBehaviour
    {
        [Header("Layout")]
        [SerializeField] private float _tileSize = 112f;
        [SerializeField] private float _tileSpacing = 20f;
        [SerializeField] private float _frameThickness = 6f;
        [SerializeField] private float _fontSize = 36f;
        // The canvas shader depth-tests, so without this push the tiles are hidden behind the arena
        [SerializeField] private float _depth = 100f;
        [Header("Colors")]
        [SerializeField] private Color _fillColor = new Color(0.05f, 0.06f, 0.1f, 0.94f);
        [SerializeField] private Color _currentColor = new Color(1f, 0.82f, 0.2f, 1f);
        [SerializeField] private Color _pendingColor = new Color(0.5f, 0.56f, 0.68f, 1f);
        [SerializeField] private Color _doneColor = new Color(0.35f, 0.9f, 0.5f, 1f);
        [SerializeField] private Color _failColor = new Color(0.95f, 0.25f, 0.25f, 1f);
        [Header("Animation")]
        [SerializeField] private float _pulseSpeed = 10f;
        [SerializeField] private float _pulseAmount = 0.08f;
        [SerializeField] private float _punchDuration = 0.18f;
        [SerializeField] private float _punchAmount = 0.35f;
        [SerializeField] private float _completeDuration = 0.25f;
        [SerializeField] private float _failDuration = 0.4f;
        [SerializeField] private float _shakeAmount = 14f;

        private readonly List<Tile> _tiles = new List<Tile>();
        private PromptState _state;
        private int _keyCount;
        private int _currentIndex;
        private float _stateTimer;
        private RectTransform _rect;
        private Vector2 _restPosition;
        private static Sprite _roundedSprite;

        /// *** Unity Events *** ///

        private void Update()
        {
            switch (_state)
            {
                case PromptState.Failed:
                    _stateTimer += Time.unscaledDeltaTime;
                    float fade = 1f - Mathf.Clamp01(_stateTimer / _failDuration);
                    _rect.anchoredPosition = _restPosition + Vector2.right * (Mathf.Sin(_stateTimer * 70f) * _shakeAmount * fade);
                    if (_stateTimer >= _failDuration)
                        Hide();
                    break;
                case PromptState.Completed:
                    _stateTimer += Time.unscaledDeltaTime;
                    if (_stateTimer >= _completeDuration)
                        Hide();
                    break;
            }

            for (int i = 0; i < _keyCount; i++)
                UpdateTile(_tiles[i], i);
        }

        /// *** Public Methods *** ///

        public void Show(IReadOnlyList<KeyCode> keys)
        {
            EnsureRect();
            _keyCount = keys.Count;
            _currentIndex = 0;
            _state = PromptState.Casting;
            _stateTimer = 0f;
            _rect.anchoredPosition = _restPosition;

            while (_tiles.Count < _keyCount)
                _tiles.Add(CreateTile());

            for (int i = 0; i < _tiles.Count; i++)
            {
                Tile tile = _tiles[i];
                tile.Root.gameObject.SetActive(i < _keyCount);
                if (i >= _keyCount)
                    continue;

                tile.Letter.text = keys[i].ToString();
                tile.Punch = 0f;
                tile.Root.anchoredPosition = new Vector2((i - (_keyCount - 1) * 0.5f) * (_tileSize + _tileSpacing), 0f);
                UpdateTile(tile, i);
            }

            gameObject.SetActive(true);
        }

        public void Advance()
        {
            if (_state != PromptState.Casting || _currentIndex >= _keyCount)
                return;

            _tiles[_currentIndex].Punch = _punchDuration;
            _currentIndex++;
        }

        public void Complete()
        {
            if (!gameObject.activeSelf)
                return;

            _currentIndex = _keyCount;
            _state = PromptState.Completed;
            _stateTimer = 0f;
        }

        public void Fail()
        {
            EnsureRect();
            _state = PromptState.Failed;
            _stateTimer = 0f;
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            _state = PromptState.Casting;
            gameObject.SetActive(false);
        }

        /// *** Private Methods *** ///

        private void EnsureRect()
        {
            if (_rect != null)
                return;

            _rect = (RectTransform)transform;
            _restPosition = _rect.anchoredPosition;
            _rect.anchoredPosition3D = new Vector3(_restPosition.x, _restPosition.y, _depth);
        }

        private void UpdateTile(Tile tile, int index)
        {
            Color frameColor;
            Color letterColor;
            float scale;

            if (_state == PromptState.Failed)
            {
                frameColor = _failColor;
                letterColor = Color.white;
                scale = 1f;
            }
            else if (index < _currentIndex)
            {
                frameColor = _doneColor;
                letterColor = new Color(_doneColor.r, _doneColor.g, _doneColor.b, 0.7f);
                scale = _state == PromptState.Completed ? 1f + 0.2f * Mathf.Sin(Mathf.Clamp01(_stateTimer / _completeDuration) * Mathf.PI) : 0.82f;
            }
            else if (index == _currentIndex)
            {
                frameColor = _currentColor;
                letterColor = Color.white;
                scale = 1.08f + Mathf.Sin(Time.unscaledTime * _pulseSpeed) * _pulseAmount;
            }
            else
            {
                frameColor = _pendingColor;
                letterColor = new Color(0.82f, 0.86f, 0.94f, 1f);
                scale = 0.92f;
            }

            if (tile.Punch > 0f)
            {
                tile.Punch -= Time.unscaledDeltaTime;
                scale += _punchAmount * Mathf.Clamp01(tile.Punch / _punchDuration);
            }

            tile.Frame.color = frameColor;
            tile.Letter.color = letterColor;
            tile.Root.localScale = Vector3.one * scale;
        }

        private Tile CreateTile()
        {
            Tile tile = new Tile();

            GameObject root = new GameObject("QTE Tile", typeof(RectTransform), typeof(Image));
            root.layer = gameObject.layer;
            tile.Root = (RectTransform)root.transform;
            tile.Root.SetParent(transform, false);
            tile.Root.sizeDelta = new Vector2(_tileSize, _tileSize);
            tile.Frame = root.GetComponent<Image>();
            ConfigureRoundedImage(tile.Frame);

            GameObject fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fill.layer = gameObject.layer;
            RectTransform fillRect = (RectTransform)fill.transform;
            fillRect.SetParent(tile.Root, false);
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.one * _frameThickness;
            fillRect.offsetMax = Vector2.one * -_frameThickness;
            Image fillImage = fill.GetComponent<Image>();
            ConfigureRoundedImage(fillImage);
            fillImage.color = _fillColor;

            GameObject letter = new GameObject("Letter", typeof(RectTransform), typeof(TextMeshProUGUI));
            letter.layer = gameObject.layer;
            RectTransform letterRect = (RectTransform)letter.transform;
            letterRect.SetParent(tile.Root, false);
            letterRect.anchorMin = Vector2.zero;
            letterRect.anchorMax = Vector2.one;
            letterRect.offsetMin = Vector2.zero;
            letterRect.offsetMax = Vector2.zero;
            tile.Letter = letter.GetComponent<TextMeshProUGUI>();
            tile.Letter.alignment = TextAlignmentOptions.Center;
            tile.Letter.fontStyle = FontStyles.Bold;
            tile.Letter.fontSize = _fontSize;
            tile.Letter.raycastTarget = false;

            return tile;
        }

        private static void ConfigureRoundedImage(Image image)
        {
            image.sprite = GetRoundedSprite();
            image.type = Image.Type.Sliced;
            image.raycastTarget = false;
        }

        // Rounded square built in code so the prompt needs no art assets
        private static Sprite GetRoundedSprite()
        {
            if (_roundedSprite != null)
                return _roundedSprite;

            const int size = 64;
            const float radius = 18f;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            Color32[] pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = Mathf.Max(radius - (x + 0.5f), (x + 0.5f) - (size - radius), 0f);
                    float dy = Mathf.Max(radius - (y + 0.5f), (y + 0.5f) - (size - radius), 0f);
                    float alpha = Mathf.Clamp01(radius - Mathf.Sqrt(dx * dx + dy * dy) + 0.5f);
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)(alpha * 255f));
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();
            _roundedSprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
            return _roundedSprite;
        }

        /// *** Nested Types *** ///

        private enum PromptState
        {
            Casting,
            Completed,
            Failed
        }

        private class Tile
        {
            public RectTransform Root;
            public Image Frame;
            public TextMeshProUGUI Letter;
            public float Punch;
        }
    }
}
