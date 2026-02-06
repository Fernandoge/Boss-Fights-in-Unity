namespace Interfaces
{
    /// <summary>
    /// Interface for entities that can be countered/kicked by the player
    /// Implement this on any GameObject that should respond to the player's counter attack (kick)
    /// </summary>
    public interface ICounterable
    {
        /// <summary>
        /// Called when the player successfully counters/kicks this entity
        /// </summary>
        void TriggerCounter();
    }
}
