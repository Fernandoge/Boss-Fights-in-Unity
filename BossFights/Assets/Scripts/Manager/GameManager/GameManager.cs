using Bosses;
using Bosses.First_Boss;
using Characters;
using UnityEngine;

namespace Manager.GameManager
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance;

        public PlayerController player;
        public FirstBoss firstBoss;

        // The boss of the current fight; set by BossSelector
        public BossController ActiveBoss { get; set; }
    
        private void Awake()
        {
            if (Instance == null)
                Instance = this;
        }
    }
}
