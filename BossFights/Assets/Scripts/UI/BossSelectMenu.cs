using System;
using Bosses;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace UI
{
    // Start screen: each card starts a fight against one boss
    public class BossSelectMenu : MonoBehaviour
    {
        [Serializable]
        private class BossCard
        {
            public string name;
            public Button button;
            public string sceneName;
            public int bossIndex;
        }

        [SerializeField] private BossCard[] _cards;

        private void Awake()
        {
            foreach (BossCard card in _cards)
                card.button.onClick.AddListener(() => StartFight(card));
        }

        private static void StartFight(BossCard card)
        {
            BossSelection.Choose(card.bossIndex);
            SceneManager.LoadScene(card.sceneName);
        }
    }
}
