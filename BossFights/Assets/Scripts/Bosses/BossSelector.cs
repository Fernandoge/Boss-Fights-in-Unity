using System;
using Manager.GameManager;
using UnityEngine;

namespace Bosses
{
    // Activates one boss and the level parts that belong to it, and deactivates the other bosses and their level parts.
    // Choose the boss in the Inspector before pressing Play; the selection is applied once at load.
    [DefaultExecutionOrder(-100)]
    public class BossSelector : MonoBehaviour
    {
        [Serializable]
        private class BossEntry
        {
            public string name;
            public BossController boss;
            public GameObject[] levelObjects;
        }

        [SerializeField] private BossEntry[] _bosses;
        [SerializeField] private int _startingBossIndex;

        public BossController ActiveBoss { get; private set; }

        // Deactivating before the other scripts' Awake/Start keeps the unselected boss and its level parts from ever running
        // A boss chosen on the start screen wins; pressing Play directly in the scene uses the Inspector index
        private void Awake() => Select(BossSelection.TryConsume(out int chosenIndex) ? chosenIndex : _startingBossIndex);

        private void Start() => GameManager.Instance.ActiveBoss = ActiveBoss;

        public void Select(int index)
        {
            if (_bosses == null || _bosses.Length == 0)
                return;

            index = Mathf.Clamp(index, 0, _bosses.Length - 1);
            for (int i = 0; i < _bosses.Length; i++)
            {
                bool isSelected = i == index;
                if (_bosses[i].boss)
                    _bosses[i].boss.gameObject.SetActive(isSelected);

                foreach (GameObject levelObject in _bosses[i].levelObjects)
                    if (levelObject)
                        levelObject.SetActive(isSelected);
            }

            ActiveBoss = _bosses[index].boss;
            if (GameManager.Instance)
                GameManager.Instance.ActiveBoss = ActiveBoss;
        }
    }
}
