using Bosses;
using Bosses.Gorath;
using Characters;
using UnityEngine;

namespace Manager.GameManager
{
    // Runs before the selectors, which hand it the character and the boss of the fight
    [DefaultExecutionOrder(-300)]
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance;

        public PlayerController player;
        public GorathBoss gorath;

        // The boss of the current fight; set by BossSelector
        public BossController ActiveBoss { get; set; }
    
        private void Awake()
        {
            if (Instance == null)
                Instance = this;
        }
    }
}
