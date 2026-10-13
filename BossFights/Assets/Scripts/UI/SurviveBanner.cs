using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    // "SURVIVE!" at the top of the screen while the player has to last; built in code like the menu screens
    public class SurviveBanner : MonoBehaviour
    {
        private const float TextSize = 110f;
        private const float PulseAmount = 0.06f;
        private const float PulseSpeed = 6f;
        private static readonly Color TextColor = new Color(1f, 0.25f, 0.15f);

        private RectTransform _textRect;

        private void Update() =>
            _textRect.localScale = Vector3.one * (1f + Mathf.Sin(Time.unscaledTime * PulseSpeed) * PulseAmount);

        public static SurviveBanner Show()
        {
            GameObject bannerObject = new GameObject("Survive Banner", typeof(Canvas), typeof(CanvasScaler));
            SurviveBanner banner = bannerObject.AddComponent<SurviveBanner>();

            Canvas canvas = bannerObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 500;

            CanvasScaler scaler = bannerObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            GameObject textObject = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(bannerObject.transform, false);

            TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
            text.text = "SURVIVE!";
            text.fontSize = TextSize;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
            text.color = TextColor;
            text.outlineWidth = 0.2f;
            text.outlineColor = new Color(0.1f, 0.03f, 0f);
            text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.NoWrap;

            // Anchored to the top middle of the screen
            RectTransform rect = text.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, -100f);
            rect.sizeDelta = new Vector2(1400f, TextSize * 1.4f);
            banner._textRect = rect;
            return banner;
        }

        public void Hide() => Destroy(gameObject);
    }
}
