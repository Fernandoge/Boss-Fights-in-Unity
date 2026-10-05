using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class PlayerHealthUI : MonoBehaviour
    {
        [SerializeField] private Color _fullHeartColor = new Color(0.86f, 0.08f, 0.15f);
        [SerializeField] private Color _emptyHeartColor = new Color(0.15f, 0.15f, 0.15f, 0.75f);

        private Image[] _hearts;

        private void Awake() => _hearts = GetComponentsInChildren<Image>(true);

        public void UpdateHearts(int currentHealth)
        {
            for (int i = 0; i < _hearts.Length; i++)
                _hearts[i].color = i < currentHealth ? _fullHeartColor : _emptyHeartColor;
        }
    }
}
