#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using Bosses;
using Bosses.First_Boss;
using Bosses.Second_Boss;
using Manager.GameManager;
using UnityEngine;

namespace Debugging
{
    public class BossDebugHarness : MonoBehaviour
    {
        [SerializeField] private float _slowMotionScale = 0.25f;
        [SerializeField] private float _messageDuration = 2f;

        [Header("Only Use One Attack")]
        [SerializeField] private bool _onlyUseAttack;
        [SerializeField] private FirstBossAttack _attackToUse = FirstBossAttack.BoulderRoll;
        [SerializeField] private SecondBossAttack _secondBossAttackToUse = SecondBossAttack.DiagonalLines;

        private static readonly KeyCode[] AttackKeys =
        {
            KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3, KeyCode.Alpha4, KeyCode.Alpha5,
            KeyCode.Alpha6, KeyCode.Alpha7, KeyCode.Alpha8, KeyCode.Alpha9, KeyCode.Alpha0
        };
        private static readonly FirstBossAttack[] Attacks = (FirstBossAttack[])Enum.GetValues(typeof(FirstBossAttack));

        private GUIStyle _style;
        private string _helpText;
        private BossController _helpTextBoss;
        private string _message = "";
        private float _messageTimer;
        private bool _isHelpVisible = true;
        private bool _isSlowMotion;

        private void Start()
        {
            BossController boss = GetBoss();
            if (boss is FirstBoss firstBoss)
                firstBoss.DebugOnlyAttack = _onlyUseAttack ? _attackToUse : (FirstBossAttack?)null;
            else if (boss is SecondBoss secondBoss)
                secondBoss.DebugOnlyAttack = _onlyUseAttack ? _secondBossAttackToUse : (SecondBossAttack?)null;
        }

        private void Update()
        {
            BossController activeBoss = GetBoss();
            if (activeBoss is FirstBoss)
                for (int i = 0; i < Attacks.Length && i < AttackKeys.Length; i++)
                    if (Input.GetKeyDown(AttackKeys[i]))
                        ForceAttack(Attacks[i]);

            if (activeBoss is SecondBoss secondBoss)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1))
                    ForceSecondBossAction(secondBoss.DebugForceSpellCircles, "Forced SpellCircles");
                if (Input.GetKeyDown(KeyCode.Alpha2))
                    ForceSecondBossAction(secondBoss.DebugTeleport, "Forced Teleport");
                if (Input.GetKeyDown(KeyCode.Alpha3))
                    ForceSecondBossAction(secondBoss.DebugForceDiagonalLines, "Forced DiagonalLines");
                if (Input.GetKeyDown(KeyCode.Alpha4))
                    ForceSecondBossAction(secondBoss.DebugForceIntermission, "Forced Intermission");
                if (Input.GetKeyDown(KeyCode.Alpha5))
                    ForceSecondBossAction(secondBoss.DebugForceOrbBarrage, "Forced OrbBarrage");
                if (Input.GetKeyDown(KeyCode.Alpha6))
                    ForceSecondBossAction(secondBoss.DebugForceClockStart, "Forced ClockStart");
                if (Input.GetKeyDown(KeyCode.Alpha7))
                    ForceSecondBossAction(secondBoss.DebugForceStarfall, "Forced Starfall");
                if (Input.GetKeyDown(KeyCode.Alpha8))
                    ForceSecondBossAction(secondBoss.DebugForceTimedExplosions, "Forced TimedExplosions");
                if (Input.GetKeyDown(KeyCode.Alpha9))
                    ForceSecondBossAction(secondBoss.DebugForceColorIntermission, "Forced ColorIntermission");
            }

            if (Input.GetKeyDown(KeyCode.F1))
                ToggleAutoAttacks();
            if (Input.GetKeyDown(KeyCode.F2))
                ToggleInvulnerable();
            if (Input.GetKeyDown(KeyCode.F3))
                TogglePhaseTwoSkillVariants();
            if (Input.GetKeyDown(KeyCode.F4))
                ToggleSlowMotion();
            if (Input.GetKeyDown(KeyCode.F5))
                ToggleOnlyUseAttack();
            if (Input.GetKeyDown(KeyCode.F6))
                SetBossHealthJustAboveHalf();
            if (Input.GetKeyDown(KeyCode.F7))
                SetBossHealthToOne();
            if (Input.GetKeyDown(KeyCode.Tab))
                _isHelpVisible = !_isHelpVisible;

            if (_messageTimer > 0f)
                _messageTimer -= Time.unscaledDeltaTime;
        }

        public void SetBossHealthJustAboveHalf()
        {
            GetBoss().DebugSetHealthJustAboveHalf();
            Show("Boss health set just above the phase 2 line");
        }

        public void SetBossHealthToOne()
        {
            GetBoss().DebugSetHealthToOne();
            Show("Boss health set to 1 (next hit kills)");
        }

        private void OnDisable() => Time.timeScale = 1f;

        private void OnGUI()
        {
            if (_style == null)
                _style = new GUIStyle(GUI.skin.box) { alignment = TextAnchor.UpperLeft, fontSize = 13, richText = true };

            BossController boss = GetBoss();
            if (_isHelpVisible && boss)
            {
                // The help text depends on which boss is active, so rebuild it when the active boss changes
                if (_helpTextBoss != boss)
                {
                    _helpTextBoss = boss;
                    _helpText = BuildHelpText(boss);
                }

                GUIContent content = new GUIContent(_helpText + "\n" + GetStatusText(boss));
                Vector2 size = _style.CalcSize(content);
                GUI.Box(new Rect(Screen.width - size.x - 10f, 50f, size.x, size.y), content, _style);
            }

            if (_messageTimer > 0f)
                GUI.Box(new Rect(Screen.width / 2f - 150f, 60f, 300f, 28f), _message, _style);
        }

        public void ForceAttack(FirstBossAttack attack)
        {
            FirstBoss boss = GetBoss() as FirstBoss;
            if (!boss)
            {
                Show("No first boss attacks for this boss");
                return;
            }

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
            BossController boss = GetBoss();
            boss.DebugAutoAttacksDisabled = !boss.DebugAutoAttacksDisabled;
            Show("Boss auto attacks " + (boss.DebugAutoAttacksDisabled ? "OFF" : "ON"));
        }

        // The boss repeats the attack picked in the Inspector instead of its normal attack rotation
        public void ToggleOnlyUseAttack()
        {
            if (GetBoss() is SecondBoss secondBoss)
            {
                secondBoss.DebugOnlyAttack = secondBoss.DebugOnlyAttack.HasValue ? (SecondBossAttack?)null : _secondBossAttackToUse;
                Show("Only use attack: " + (secondBoss.DebugOnlyAttack.HasValue ? secondBoss.DebugOnlyAttack.Value.ToString() : "OFF"));
                return;
            }

            FirstBoss boss = GetBoss() as FirstBoss;
            if (!boss)
            {
                Show("Only use attack is not available for this boss");
                return;
            }

            boss.DebugOnlyAttack = boss.DebugOnlyAttack.HasValue ? (FirstBossAttack?)null : _attackToUse;
            Show("Only use attack: " + (boss.DebugOnlyAttack.HasValue ? boss.DebugOnlyAttack.Value.ToString() : "OFF"));
        }

        public void ToggleInvulnerable()
        {
            GameManager.Instance.player.DebugInvulnerable = !GameManager.Instance.player.DebugInvulnerable;
            Show("Player invulnerable " + (GameManager.Instance.player.DebugInvulnerable ? "ON" : "OFF"));
        }

        // Only switches the phase 2 versions of skills (more lines, etc.); it does not run the real phase 2 transition
        public void TogglePhaseTwoSkillVariants()
        {
            BossController boss = GetBoss();
            boss.DebugSetSecondPhaseFlag(!boss.IsInSecondPhase);
            Show("Phase 2 skill variants " + (boss.IsInSecondPhase ? "ON" : "OFF"));
        }

        public void ToggleSlowMotion()
        {
            _isSlowMotion = !_isSlowMotion;
            Time.timeScale = _isSlowMotion ? _slowMotionScale : 1f;
            Show("Slow motion " + (_isSlowMotion ? "ON" : "OFF"));
        }

        // The boss of the current fight; falls back to the first boss in scenes without a BossSelector
        private static BossController GetBoss() => GameManager.Instance.ActiveBoss ? GameManager.Instance.ActiveBoss : GameManager.Instance.firstBoss;

        private void ForceSecondBossAction(Action action, string message)
        {
            if (GetBoss().DebugIsBusy)
            {
                Show("Boss is busy");
                return;
            }

            action();
            Show(message);
        }

        private void Show(string message)
        {
            _message = message;
            _messageTimer = _messageDuration;
        }

        private string GetStatusText(BossController boss)
        {
            string onlyAttack = "n/a";
            if (boss is FirstBoss firstBoss)
                onlyAttack = firstBoss.DebugOnlyAttack.HasValue ? firstBoss.DebugOnlyAttack.Value.ToString() : "OFF";
            else if (boss is SecondBoss secondBoss)
                onlyAttack = secondBoss.DebugOnlyAttack.HasValue ? secondBoss.DebugOnlyAttack.Value.ToString() : "OFF";

            return "Boss: " + boss.name +
                   "\nAuto attacks: " + (boss.DebugAutoAttacksDisabled ? "OFF" : "ON") +
                   "   Invulnerable: " + (GameManager.Instance.player.DebugInvulnerable ? "ON" : "OFF") +
                   "   Phase 2 variants: " + (boss.IsInSecondPhase ? "ON" : "OFF") +
                   "   Slow motion: " + (_isSlowMotion ? "ON" : "OFF") +
                   "\nOnly attack: " + onlyAttack;
        }

        private static string BuildHelpText(BossController boss)
        {
            string text = "<b>Debug harness</b> (Tab hides)\n";
            if (boss is FirstBoss)
                for (int i = 0; i < Attacks.Length && i < AttackKeys.Length; i++)
                    text += AttackKeys[i].ToString().Replace("Alpha", "") + "  " + Attacks[i] + "\n";
            else if (boss is SecondBoss)
                text += "1  SpellCircles\n2  Teleport\n3  DiagonalLines\n4  Intermission\n5  OrbBarrage\n6  ClockStart\n7  Starfall\n8  TimedExplosions\n9  ColorIntermission\n";
            else
                text += "(no forced attacks for this boss yet)\n";

            text += "F1  Toggle boss auto attacks\nF2  Toggle player invulnerable\nF3  Toggle phase 2 skill variants\nF4  Toggle slow motion\nF5  Toggle only use one attack\nF6  Boss health just above the phase 2 line (next hit starts phase 2)\nF7  Boss health to 1 (next hit kills)";
            return text;
        }
    }
}
#endif
