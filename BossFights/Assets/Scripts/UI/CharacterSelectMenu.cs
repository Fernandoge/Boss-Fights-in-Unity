using System;
using Characters;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    // First screen of the start scene: one card per character. Choosing one remembers it for the fight and swaps the screen for the boss cards, which the Change Character button swaps back
    public class CharacterSelectMenu : MonoBehaviour
    {
        [Serializable]
        private class CharacterCard
        {
            public string name;
            public Button button;
            public int characterIndex;
        }

        [SerializeField] private CharacterCard[] _cards;
        [SerializeField] private GameObject[] _characterScreenObjects;
        [SerializeField] private GameObject[] _bossScreenObjects;
        [SerializeField] private Button _changeCharacterButton;
        [SerializeField] private TextMeshProUGUI _playingAsLabel;

        private void Awake()
        {
            foreach (CharacterCard card in _cards)
                card.button.onClick.AddListener(() => ChooseCharacter(card));

            _changeCharacterButton.onClick.AddListener(() => ShowScreen(false));

            // Coming back from a fight keeps the character, so only the boss has to be picked again
            if (CharacterSelection.HasChosen)
                ChooseCharacter(GetCard(CharacterSelection.LastIndex));
            else
                ShowScreen(false);
        }

        private CharacterCard GetCard(int characterIndex)
        {
            foreach (CharacterCard card in _cards)
                if (card.characterIndex == characterIndex)
                    return card;

            return _cards[0];
        }

        private void ChooseCharacter(CharacterCard card)
        {
            CharacterSelection.Choose(card.characterIndex);
            _playingAsLabel.text = "Playing as " + card.name;
            ShowScreen(true);
        }

        private void ShowScreen(bool isBossScreen)
        {
            foreach (GameObject screenObject in _characterScreenObjects)
                screenObject.SetActive(!isBossScreen);

            foreach (GameObject screenObject in _bossScreenObjects)
                screenObject.SetActive(isBossScreen);
        }
    }
}
