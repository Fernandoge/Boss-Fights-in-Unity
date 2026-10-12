using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    // The prompt of a button-smashing cast: a bar that fills with every press of the key, and a thin timer under it that runs out
    public class SmashPrompt : MonoBehaviour
    {
        [SerializeField] private Image _fill;
        [SerializeField] private Image _timerFill;
        [SerializeField] private TextMeshProUGUI _keyLabel;
        [SerializeField] private TextMeshProUGUI _countLabel;
        [SerializeField] private RectTransform _pulseTarget;
        [SerializeField] private Color _fillColor = new Color(0.35f, 0.85f, 0.4f);
        [SerializeField] private Color _completeColor = new Color(0.6f, 1f, 0.7f);
        [SerializeField] private Color _failColor = new Color(0.9f, 0.2f, 0.2f);
        [SerializeField] private float _pulseStrength = 0.18f;
        [SerializeField] private float _pulseSpeed = 8f;
        [SerializeField] private float _finishedShowTime = 0.5f;

        private float _pulse;
        private float _hideTimer;
        private bool _isFinished;

        /// *** Unity Events *** ///

        private void Update()
        {
            _pulse = Mathf.MoveTowards(_pulse, 0f, _pulseSpeed * Time.deltaTime);
            _pulseTarget.localScale = Vector3.one * (1f + _pulseStrength * _pulse);

            if (!_isFinished)
                return;

            _hideTimer -= Time.deltaTime;
            if (_hideTimer <= 0f)
                Hide();
        }

        /// *** Public Methods *** ///

        public void Show(KeyCode key, int total)
        {
            gameObject.SetActive(true);
            _isFinished = false;
            _pulse = 0f;
            _fill.color = _fillColor;
            _fill.fillAmount = 0f;
            _timerFill.fillAmount = 1f;
            _keyLabel.text = key.ToString();
            _countLabel.text = "0 / " + total;
        }

        public void SetProgress(int current, int total)
        {
            _fill.fillAmount = Mathf.Clamp01(current / (float)total);
            _countLabel.text = Mathf.Min(current, total) + " / " + total;
            _pulse = 1f;
        }

        public void SetTimeLeft(float normalizedTime) => _timerFill.fillAmount = Mathf.Clamp01(normalizedTime);

        public void Complete() => Finish(_completeColor, "DONE");

        public void Fail() => Finish(_failColor, "FAILED");

        public void Hide() => gameObject.SetActive(false);

        /// *** Private Methods *** ///

        private void Finish(Color color, string text)
        {
            if (!gameObject.activeSelf)
                return;

            _isFinished = true;
            _hideTimer = _finishedShowTime;
            _fill.color = color;
            _countLabel.text = text;
            _pulse = 1f;
        }
    }
}
