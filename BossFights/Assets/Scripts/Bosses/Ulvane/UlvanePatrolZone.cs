using UnityEngine;
using Random = UnityEngine.Random;

namespace Bosses.Ulvane
{
    // A round area an animal never leaves while it patrols (the penguin's ice floor); the animal is found by the attack it owns
    public class UlvanePatrolZone : MonoBehaviour
    {
        [SerializeField] private UlvaneAttack _beast = UlvaneAttack.PenguinSnowballs;
        [SerializeField] private float _radius = 5f;

        public UlvaneAttack Beast => _beast;

        // The radius follows the object's scale, so resizing the zone in the scene resizes the patrol area
        private float WorldRadius => _radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.z);

        public bool Contains(Vector3 point, float margin = 0f)
        {
            Vector3 offset = point - transform.position;
            offset.y = 0f;
            float radius = WorldRadius + margin;
            return offset.sqrMagnitude <= radius * radius;
        }

        public Vector3 RandomPoint()
        {
            Vector2 circle = Random.insideUnitCircle * (WorldRadius * 0.85f);
            return transform.position + new Vector3(circle.x, 0f, circle.y);
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0f, 1f, 1f, 0.6f);
            Gizmos.DrawWireSphere(transform.position, WorldRadius);
        }
    }
}
