using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class SpellIcon : MonoBehaviour
    {
        [SerializeField] private Image cooldownOverlay;
        [SerializeField] private TextMeshProUGUI cooldownText;
        
        private float cooldownDuration;
        private float cooldownRemaining;
        
        private void Awake()
        {
            // Disable Update() until cooldown starts
            enabled = false;
            cooldownText.gameObject.SetActive(false);
        }
    
        public void StartCooldown(float duration)
        {
            cooldownDuration = duration;
            cooldownRemaining = cooldownDuration;
            cooldownOverlay.fillAmount = 1;
            cooldownText.gameObject.SetActive(true);
            enabled = true; // Enable Update() for cooldown tracker
        }
    
        private void Update()
        {
            cooldownRemaining -= Time.deltaTime;
            cooldownOverlay.fillAmount = cooldownRemaining / cooldownDuration;
            cooldownText.text = Mathf.Ceil(cooldownRemaining).ToString();
            
            // Change text color to red when 2 seconds or less remaining
            cooldownText.color = cooldownRemaining <= 2f ? Color.red : Color.white;
        
            if (cooldownRemaining <= 0)
            {
                cooldownText.gameObject.SetActive(false);
                cooldownOverlay.fillAmount = 0;
                enabled = false; // Disable Update() when done
            }
        }
    }
}