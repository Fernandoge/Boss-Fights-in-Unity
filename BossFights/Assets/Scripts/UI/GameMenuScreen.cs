using Bosses;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace UI
{
    // The game over, victory and pause screens: a dimmed screen with a title and the Restart and Boss Selection buttons; it is built in code, so nothing needs to be wired in a scene
    public class GameMenuScreen : MonoBehaviour
    {
        private const string BossSelectionScene = "StartScene";
        private const float GameOverFadeTime = 0.6f;
        private const float PauseFadeTime = 0.15f;
        private static readonly Color BackgroundColor = new Color(0f, 0f, 0f, 0.75f);
        private static readonly Color GameOverColor = new Color(0.86f, 0.08f, 0.15f);
        private static readonly Color VictoryColor = new Color(1f, 0.82f, 0.2f);
        private static readonly Color ButtonColor = new Color(0.18f, 0.18f, 0.2f, 0.95f);

        private static int _openScreens;

        private CanvasGroup _canvasGroup;
        private string _title;
        private string _restartLabel;
        private Color _titleColor;
        private float _delay;
        private float _fadeTime;
        private float _elapsed;
        private float _previousTimeScale;
        private bool _isShown;

        // True while any of the screens is on display, so the player does not stack a pause screen on top of one
        public static bool IsOpen => _openScreens > 0;

        private void Update()
        {
            _elapsed += Time.unscaledDeltaTime;
            if (!_isShown)
            {
                // The death animation plays at normal speed first; the screen then freezes the game
                if (_elapsed >= _delay)
                    Build();
                return;
            }

            float alpha = Mathf.Clamp01(_elapsed / _fadeTime);
            _canvasGroup.alpha = alpha;
            // Clicks that were meant for the fight can not press a button by accident while the screen fades in
            _canvasGroup.interactable = alpha >= 1f;
        }

        private void OnDestroy()
        {
            if (_isShown)
                _openScreens--;
        }

        // delay is how long the death animation gets before the screen appears
        public static GameMenuScreen ShowGameOver(float delay) => Create("GAME OVER", GameOverColor, "Restart", delay, GameOverFadeTime);

        public static GameMenuScreen ShowVictory(float delay) => Create("YOU WIN!", VictoryColor, "Kill the boss again!", delay, GameOverFadeTime);

        public static GameMenuScreen ShowPaused() => Create("PAUSED", Color.white, "Restart", 0f, PauseFadeTime);

        public void Close()
        {
            Time.timeScale = _previousTimeScale;
            Destroy(gameObject);
        }

        private static GameMenuScreen Create(string title, Color titleColor, string restartLabel, float delay, float fadeTime)
        {
            GameObject screenObject = new GameObject(title + " Screen");
            GameMenuScreen screen = screenObject.AddComponent<GameMenuScreen>();
            screen._title = title;
            screen._titleColor = titleColor;
            screen._restartLabel = restartLabel;
            screen._delay = delay;
            screen._fadeTime = fadeTime;
            if (delay <= 0f)
                screen.Build();
            return screen;
        }

        private void Build()
        {
            _isShown = true;
            _openScreens++;
            _elapsed = 0f;
            _previousTimeScale = Time.timeScale;
            Time.timeScale = 0f;

            if (!EventSystem.current)
                new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

            GameObject canvasObject = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup));
            canvasObject.transform.SetParent(transform, false);

            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            _canvasGroup = canvasObject.GetComponent<CanvasGroup>();
            _canvasGroup.alpha = 0f;
            _canvasGroup.interactable = false;

            Image background = CreateImage("Background", canvasObject.transform, BackgroundColor, Vector2.zero, Vector2.zero);
            RectTransform backgroundRect = background.rectTransform;
            backgroundRect.anchorMin = Vector2.zero;
            backgroundRect.anchorMax = Vector2.one;
            backgroundRect.offsetMin = Vector2.zero;
            backgroundRect.offsetMax = Vector2.zero;

            CreateText("Title", canvasObject.transform, _title, 150f, _titleColor, new Vector2(0f, 150f));
            CreateButton("Restart Button", canvasObject.transform, _restartLabel, new Vector2(0f, -30f), Restart);
            CreateButton("Boss Selection Button", canvasObject.transform, "Boss Selection", new Vector2(0f, -160f), OpenBossSelection);
        }

        private static void CreateButton(string objectName, Transform parent, string label, Vector2 position, UnityAction onClick)
        {
            Image image = CreateImage(objectName, parent, ButtonColor, position, new Vector2(720f, 100f));
            Button button = image.gameObject.AddComponent<Button>();
            button.onClick.AddListener(onClick);
            CreateText("Label", image.transform, label, 46f, Color.white, Vector2.zero);
        }

        private static Image CreateImage(string objectName, Transform parent, Color color, Vector2 position, Vector2 size)
        {
            GameObject imageObject = new GameObject(objectName, typeof(RectTransform), typeof(Image));
            imageObject.transform.SetParent(parent, false);

            Image image = imageObject.GetComponent<Image>();
            image.color = color;
            image.rectTransform.anchoredPosition = position;
            image.rectTransform.sizeDelta = size;
            return image;
        }

        private static void CreateText(string objectName, Transform parent, string content, float fontSize, Color color, Vector2 position)
        {
            GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(parent, false);

            TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
            text.text = content;
            text.fontSize = fontSize;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
            text.color = color;
            text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.rectTransform.anchoredPosition = position;
            text.rectTransform.sizeDelta = new Vector2(1400f, fontSize * 1.4f);
        }

        private static void Restart()
        {
            BossSelection.Choose(BossSelection.LastIndex);
            LoadScene(SceneManager.GetActiveScene().name);
        }

        private static void OpenBossSelection() => LoadScene(BossSelectionScene);

        private static void LoadScene(string sceneName)
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(sceneName);
        }
    }
}
