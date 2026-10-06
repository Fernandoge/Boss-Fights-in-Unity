namespace Bosses
{
    // Carries the boss chosen on the start screen into the fight scene; read once by BossSelector
    public static class BossSelection
    {
        private static int _pendingIndex = -1;

        public static void Choose(int index) => _pendingIndex = index;

        public static bool TryConsume(out int index)
        {
            index = _pendingIndex;
            _pendingIndex = -1;
            return index >= 0;
        }
    }
}
