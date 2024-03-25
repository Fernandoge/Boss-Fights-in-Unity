using Characters;
using UnityEngine;

namespace Manager.GameManager
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance;

        public PlayerController player;
        public FirstBoss firstBoss;
    
        private void Awake()
        {
            if (Instance == null)
                Instance = this;
        }
    }
}
