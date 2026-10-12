using System;
using Cameras;
using Manager.GameManager;
using UnityEngine;

namespace Characters
{
    // Activates the chosen character and deactivates the others, together with the UI that belongs to each, and points the camera and the GameManager at it.
    // The first entry is the character the scene was built with; the others take its place. The character chosen on the start screen wins, pressing Play directly in the scene uses the Inspector index.
    [DefaultExecutionOrder(-200)]
    public class CharacterSelector : MonoBehaviour
    {
        [Serializable]
        private class CharacterEntry
        {
            public string name;
            public PlayerController player;
            public GameObject[] uiObjects;
        }

        [SerializeField] private CharacterEntry[] _characters;
        [SerializeField] private int _startingCharacterIndex;

        public PlayerController ActivePlayer { get; private set; }

        private void Awake() => Select(CharacterSelection.TryConsume(out int chosenIndex) ? chosenIndex : _startingCharacterIndex);

        // The chosen character is switched on here and not in Awake: an agent that appears before the scene's NavMesh is registered fails to be created
        private void Start()
        {
            if (ActivePlayer && !ActivePlayer.gameObject.activeSelf)
                ActivePlayer.gameObject.SetActive(true);
        }

        private void Select(int index)
        {
            if (_characters == null || _characters.Length == 0)
                return;

            index = Mathf.Clamp(index, 0, _characters.Length - 1);
            CharacterSelection.LastIndex = index;

            PlayerController defaultPlayer = _characters[0].player;
            PlayerController chosenPlayer = _characters[index].player;
            if (chosenPlayer != defaultPlayer)
                chosenPlayer.transform.SetPositionAndRotation(defaultPlayer.transform.position, defaultPlayer.transform.rotation);

            for (int i = 0; i < _characters.Length; i++)
            {
                bool isSelected = i == index;
                if (!isSelected)
                    _characters[i].player.gameObject.SetActive(false);

                foreach (GameObject uiObject in _characters[i].uiObjects)
                    if (uiObject)
                        uiObject.SetActive(isSelected);
            }

            ActivePlayer = chosenPlayer;
            if (GameManager.Instance)
                GameManager.Instance.player = chosenPlayer;

            foreach (CameraController cameraController in FindObjectsByType<CameraController>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                cameraController.player = chosenPlayer.transform;

            if (chosenPlayer != defaultPlayer)
                RetargetParticleTriggers(defaultPlayer, chosenPlayer);
        }

        // Some boss attacks hurt through a particle system whose trigger module lists the player's collider, set in the scene for the character it was built with
        private static void RetargetParticleTriggers(PlayerController oldPlayer, PlayerController newPlayer)
        {
            Collider newCollider = newPlayer.GetComponent<Collider>();
            foreach (ParticleSystem particles in FindObjectsByType<ParticleSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                ParticleSystem.TriggerModule trigger = particles.trigger;
                if (!trigger.enabled)
                    continue;

                for (int i = 0; i < trigger.colliderCount; i++)
                {
                    Component listed = trigger.GetCollider(i);
                    if (listed && listed.transform.IsChildOf(oldPlayer.transform))
                        trigger.SetCollider(i, newCollider);
                }
            }
        }
    }
}
