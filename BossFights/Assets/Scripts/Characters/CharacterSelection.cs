namespace Characters
{
    // Carries the character chosen on the start screen into the fight scene; read once by CharacterSelector
    public static class CharacterSelection
    {
        private static int _pendingIndex = -1;

        // The character of the fight in the current scene, so the game over screen can restart with the same one
        public static int LastIndex { get; set; }

        // True once a character was picked this session, so the start screen can go straight to the bosses when coming back from a fight
        public static bool HasChosen { get; private set; }

        public static void Choose(int index)
        {
            _pendingIndex = index;
            LastIndex = index;
            HasChosen = true;
        }

        public static bool TryConsume(out int index)
        {
            index = _pendingIndex;
            _pendingIndex = -1;
            return index >= 0;
        }
    }
}
