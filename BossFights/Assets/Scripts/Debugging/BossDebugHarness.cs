#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using Bosses.First_Boss;
using Manager.GameManager;
using UnityEngine;

namespace Debugging
{
    public class BossDebugHarness : MonoBehaviour
    {
        [SerializeField] private float _slowMotionScale = 0.25f;
        [SerializeField] private float _messageDuration = 2f;

        private static readonly KeyCode[] AttackKeys =
        {
            KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3, KeyCode.Alpha4, KeyCode.Alpha5,
            KeyCode.Alpha6, KeyCode.Alpha7, KeyCode.Alpha8, KeyCode.Alpha9, KeyCode.Alpha0
        };
        private static readonly FirstBossAttack[] Attacks = (FirstBossAttack[])Enum.GetValues(typeof(FirstBossAttack));

        private GUIStyle _style;
        private string _helpText;
        private string _message = "";
        private float _messageTimer;
        private bool _isHelpVisible = true;
        private bool _isSlowMotion;

        private void Awake() => _helpText = BuildHelpText();

        private void Update()
        {
            for (int i = 0; i < Attacks.Length && i < AttackKeys.Length; i++)
                if (Input.GetKeyDown(AttackKeys[i]))
                    ForceAttack(Attacks[i]);

            if (Input.GetKeyDown(KeyCode.F1))
                ToggleAutoAttacks();
            if (Input.GetKeyDown(KeyCode.F2))
                ToggleInvulnerable();
            if (Input.GetKeyDown(KeyCode.F3))
                TogglePhaseTwoSkillVariants();
            if (Input.GetKeyDown(KeyCode.F4))
                ToggleSlowMotion();
            if (Input.GetKeyDown(KeyCode.Tab))
                _isHelpVisible = !_isHelpVisible;

            if (_messageTimer > 0f)
                _messageTimer -= Time.unscaledDeltaTime;
        }

        private void OnDisable() => Time.timeScale = 1f;

        private void OnGUI()
        {
            if (_style == null)
                _style = new GUIStyle(GUI.skin.box) { alignment = TextAnchor.UpperLeft, fontSize = 13, richText = true };

            if (_isHelpVisible)
            {
                GUIContent content = new GUIContent(_helpText + "\n" + GetStatusText());
                Vector2 size = _style.CalcSize(content);
                GUI.Box(new Rect(Screen.width - size.x - 10f, 50f, size.x, size.y), content, _style);
            }

            if (_messageTimer > 0f)
                GUI.Box(new Rect(Screen.width / 2f - 150f, 60f, 300f, 28f), _message, _style);
        }

        public void ForceAttack(FirstBossAttack attack)
        {
            FirstBoss boss = GameManager.Instance.firstBoss;
            if (boss.DebugIsBusy)
            {
                Show("Boss is busy");
                return;
            }

            boss.DebugForceAttack(attack);
            Show("Forced " + attack);
        }

        public void ToggleAutoAttacks()
        {
            FirstBoss boss = GameManager.Instance.firstBoss;
            boss.DebugAutoAttacksDisabled = !boss.DebugAutoAttacksDisabled;
            Show("Boss auto attacks " + (boss.DebugAutoAttacksDisabled ? "OFF" : "ON"));
        }

        public void ToggleInvulnerable()
        {
            GameManager.Instance.player.DebugInvulnerable = !GameManager.Instance.player.DebugInvulnerable;
            Show("Player invulnerable " + (GameManager.Instance.player.DebugInvulnerable ? "ON" : "OFF"));
        }

        // Only switches the phase 2 versions of skills (more lines, etc.); it does not run the real phase 2 transition
        public void TogglePhaseTwoSkillVariants()
        {
            FirstBoss boss = GameManager.Instance.firstBoss;
            boss.DebugSetSecondPhaseFlag(!boss.IsInSecondPhase);
            Show("Phase 2 skill variants " + (boss.IsInSecondPhase ? "ON" : "OFF"));
        }

        public void ToggleSlowMotion()
        {
            _isSlowMotion = !_isSlowMotion;
            Time.timeScale = _isSlowMotion ? _slowMotionScale : 1f;
            Show("Slow motion " + (_isSlowMotion ? "ON" : "OFF"));
        }

        private void Show(string message)
        {
            _message = message;
            _messageTimer = _messageDuration;
        }

        private string GetStatusText()
        {
            FirstBoss boss = GameManager.Instance.firstBoss;
            return "Auto attacks: " + (boss.DebugAutoAttacksDisabled ? "OFF" : "ON") +
                   "   Invulnerable: " + (GameManager.Instance.player.DebugInvulnerable ? "ON" : "OFF") +
                   "   Phase 2 variants: " + (boss.IsInSecondPhase ? "ON" : "OFF") +
                   "   Slow motion: " + (_isSlowMotion ? "ON" : "OFF");
        }

        private static string BuildHelpText()
        {
            string text = "<b>Debug harness</b> (Tab hides)\n";
            for (int i = 0; i < Attacks.Length && i < AttackKeys.Length; i++)
                text += AttackKeys[i].ToString().Replace("Alpha", "") + "  " + Attacks[i] + "\n";
            text += "F1  Toggle boss auto attacks\nF2  Toggle player invulnerable\nF3  Toggle phase 2 skill variants\nF4  Toggle slow motion";
            return text;
        }
    }
}
#endif
