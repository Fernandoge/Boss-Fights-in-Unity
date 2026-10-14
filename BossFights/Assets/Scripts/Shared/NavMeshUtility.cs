using UnityEngine;
using UnityEngine.AI;

namespace Shared
{
    public static class NavMeshUtility
    {
        // The closest point on the NavMesh, or the point itself when none is near
        public static Vector3 Snap(Vector3 point, float maxDistance = 6f) =>
            NavMesh.SamplePosition(point, out NavMeshHit hit, maxDistance, NavMesh.AllAreas) ? hit.position : point;

        // The farthest point of the straight line from one point towards another that is still walkable
        public static Vector3 ClampLine(Vector3 from, Vector3 to)
        {
            from = Snap(from);
            return NavMesh.Raycast(from, to, out NavMeshHit hit, NavMesh.AllAreas) ? hit.position : to;
        }

        // How far a straight line can go before it meets an obstacle or the edge of the arena
        public static float DistanceToEdge(Vector3 from, Vector3 direction, float maxLength)
        {
            direction.y = 0f;
            Vector3 end = ClampLine(from, from + direction.normalized * maxLength);
            end.y = from.y;
            return Vector3.Distance(from, end);
        }

        // How far a ball flying at the given height above the point can go before it meets a tree or a rock. The walkable floor ends earlier than the trees (where the camera stops seeing the player), so this reads the scenery itself; triggers and the Ignore Raycast layer are not scenery
        public static float DistanceToScenery(Vector3 from, Vector3 direction, float height, float maxLength)
        {
            direction.y = 0f;
            Vector3 start = new Vector3(from.x, from.y + height, from.z);
            return Physics.Raycast(start, direction.normalized, out RaycastHit hit, maxLength, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore) ? hit.distance : maxLength;
        }
    }
}
