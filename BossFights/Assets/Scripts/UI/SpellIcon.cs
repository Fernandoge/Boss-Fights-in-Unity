using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class SpellIcon : MonoBehaviour
    {
        [SerializeField] private Image cooldownOverlay;
        [SerializeField] private TextMeshProUGUI cooldownText;
        [SerializeField] private TextMeshProUGUI keyLabel;
        [SerializeField] private Image highlight;
        [SerializeField] private Sprite highlightedSprite;
        [SerializeField] private float highlightPulseSpeed = 14f;
        [SerializeField] private Color activeTimerColor = new Color(0.3f, 0.8f, 1f, 0.6f);

        private float cooldownDuration;
        private float cooldownRemaining;
        private bool isHighlighted;
        private Image iconImage;
        private Sprite normalSprite;
        private Color cooldownColor;

        private void Awake()
        {
            // Disable Update() until cooldown starts or the icon is highlighted
            enabled = false;
            iconImage = GetComponent<Image>();
            normalSprite = iconImage.sprite;
            cooldownColor = cooldownOverlay.color;
            cooldownText.gameObject.SetActive(false);
            if (highlight)
                highlight.gameObject.SetActive(false);
        }

        public void SetKeyLabel(KeyCode key) => keyLabel.text = key == KeyCode.Space ? "SPACE" : key.ToString();

        // A pulsing frame around the icon (and the highlighted picture, when it has one), to show the player that this key can be pressed now
        public void SetHighlighted(bool value)
        {
            isHighlighted = value;
            if (highlightedSprite && iconImage)
                iconImage.sprite = value ? highlightedSprite : normalSprite;

            if (highlight)
                highlight.gameObject.SetActive(value);

            enabled = value || cooldownRemaining > 0f;
        }

        public void StartCooldown(float duration)
        {
            cooldownOverlay.color = cooldownColor;
            cooldownDuration = duration;
            cooldownRemaining = cooldownDuration;
            cooldownOverlay.fillAmount = 1;
            cooldownText.gameObject.SetActive(true);
            enabled = true; // Enable Update() for cooldown tracker
        }

        // The same spinner in another color and without the number (numbers are only for cooldowns), for a skill that is active right now and runs out
        public void StartActiveTimer(float duration)
        {
            StartCooldown(duration);
            cooldownOverlay.color = activeTimerColor;
            cooldownText.gameObject.SetActive(false);
        }

        private void Update()
        {
            if (isHighlighted && highlight)
            {
                Color color = highlight.color;
                color.a = Mathf.Lerp(0.45f, 1f, (Mathf.Sin(Time.unscaledTime * highlightPulseSpeed) + 1f) * 0.5f);
                highlight.color = color;
            }

            if (cooldownRemaining <= 0f)
            {
                enabled = isHighlighted;
                return;
            }

            cooldownRemaining -= Time.deltaTime;
            cooldownOverlay.fillAmount = cooldownRemaining / cooldownDuration;
            cooldownText.text = Mathf.Ceil(cooldownRemaining).ToString();

            // Change text color to red when 2 seconds or less remaining
            cooldownText.color = cooldownRemaining <= 2f ? Color.red : Color.white;

            if (cooldownRemaining <= 0)
            {
                cooldownText.gameObject.SetActive(false);
                cooldownOverlay.fillAmount = 0;
                enabled = isHighlighted; // Disable Update() when done, unless it is still highlighted
            }
        }
    }
}
