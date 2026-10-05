using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace Bosses.First_Boss
{
    public struct BoulderLane
    {
        public Vector3 Start;
        public Vector3 Direction;
        public float MaxDistance;
        public float LaunchTime;
    }

    // Plans boulder lanes that run horizontally or vertically on screen, start inside the real walls of the map and never let two boulders meet
    public static class BoulderLanePlanner
    {
        private const int MaxAttempts = 30;
        private const int AimWideningSteps = 4;
        private const int CandidatesPerLane = 30;
        private const int MinMixedWallLanes = 3;
        private const float SimulationStep = 0.1f;
        private const float CollisionMargin = 1.5f;
        private const float FallbackLaneGap = 4f;
        private const float WalkableScanStep = 1.5f;
        private const float WalkableSampleRadius = 2.5f;
        private const float WalkableHorizontalTolerance = 0.75f;
        private const float LateralKeyResolution = 1f;
        private const float MinLaneLength = 25f;

        // Four lane directions along the screen axes: 0 = right, 1 = left, 2 = up the screen, 3 = down the screen
        private const int DirectionCount = 4;

        public static Bounds GetNavMeshBounds()
        {
            NavMeshTriangulation triangulation = NavMesh.CalculateTriangulation();
            Bounds bounds = new Bounds(triangulation.vertices[0], Vector3.zero);
            foreach (Vector3 vertex in triangulation.vertices)
                bounds.Encapsulate(vertex);
            return bounds;
        }

        public static BoulderLane[] Plan(Bounds map, Vector3 playerPosition, int count, float radius, float speed,
            float firstLaunchTime, float launchInterval, float aimSpread, out bool usedFallback)
        {
            usedFallback = false;
            GetScreenAxes(out Vector3 screenRight, out Vector3 screenForward);
            Dictionary<int, Vector2> walkableCache = new Dictionary<int, Vector2>();
            List<BoulderLane> best = new List<BoulderLane>();

            // Build the set one boulder at a time, so every new lane is checked against the ones already accepted.
            // If no full set is found, aim wider and try again (the player may be standing in a corner)
            for (int attempt = 0; attempt < MaxAttempts * AimWideningSteps; attempt++)
            {
                float spread = aimSpread * (1 + attempt / MaxAttempts);
                List<BoulderLane> lanes = new List<BoulderLane>();
                for (int i = 0; i < count; i++)
                {
                    for (int candidateTry = 0; candidateTry < CandidatesPerLane; candidateTry++)
                    {
                        int wall = Random.Range(0, DirectionCount);
                        float lateral = GetPlayerLateral(playerPosition, wall, screenRight, screenForward) + Random.Range(-spread, spread);
                        if (!TryCreateLane(map, walkableCache, screenRight, screenForward, playerPosition.y, wall, lateral, radius,
                                firstLaunchTime + i * launchInterval, out BoulderLane candidate))
                            continue;

                        if (!CollidesWithAny(candidate, lanes, radius, speed))
                        {
                            lanes.Add(candidate);
                            break;
                        }
                    }

                    // No lane fits among the accepted ones; stop this attempt (boulders launch in order, so the set stays valid)
                    if (lanes.Count <= i)
                        break;
                }

                if (lanes.Count == count)
                    return lanes.ToArray();
                if (lanes.Count > best.Count)
                    best = lanes;
            }

            // Prefer a mixed-direction set with a boulder or two fewer over parallel lanes from a single wall
            if (best.Count >= Mathf.Min(count, MinMixedWallLanes))
                return best.ToArray();

            usedFallback = true;
            return PlanParallelFallback(map, walkableCache, screenRight, screenForward, playerPosition, count, radius, firstLaunchTime, launchInterval);
        }

        // Parallel lanes from one wall can never cross, so this always produces a valid (possibly smaller) set
        private static BoulderLane[] PlanParallelFallback(Bounds map, Dictionary<int, Vector2> walkableCache, Vector3 screenRight,
            Vector3 screenForward, Vector3 playerPosition, int count, float radius, float firstLaunchTime, float launchInterval)
        {
            int wall = Random.Range(0, DirectionCount);
            float pitch = radius * 2f + FallbackLaneGap;
            float playerLateral = GetPlayerLateral(playerPosition, wall, screenRight, screenForward);

            List<BoulderLane> lanes = new List<BoulderLane>();
            for (int i = 0; i < count; i++)
            {
                float lateral = playerLateral + (i - (count - 1) / 2f) * pitch;
                if (TryCreateLane(map, walkableCache, screenRight, screenForward, playerPosition.y, wall, lateral, radius,
                        firstLaunchTime + i * launchInterval, out BoulderLane lane))
                    lanes.Add(lane);
            }

            // Always return at least the lane through the player
            if (lanes.Count == 0 && TryCreateLane(map, walkableCache, screenRight, screenForward, playerPosition.y, wall, playerLateral,
                    radius, firstLaunchTime, out BoulderLane center))
                lanes.Add(center);
            return lanes.ToArray();
        }

        // The boulder starts one radius inside the wall, where the walkable ground begins along the lane, and ends past the far wall
        private static bool TryCreateLane(Bounds map, Dictionary<int, Vector2> walkableCache, Vector3 screenRight, Vector3 screenForward,
            float groundY, int wall, float lateral, float radius, float launchTime, out BoulderLane lane)
        {
            lane = default;
            GetWallAxes(wall, screenRight, screenForward, out Vector3 travelAxis, out Vector3 lateralAxis, out bool isPositive);

            if (!TryGetWalkableRange(map, walkableCache, travelAxis, lateralAxis, groundY, wall, lateral, out float low, out float high) ||
                high - low < MinLaneLength)
                return false;

            float axisStart = isPositive ? low - radius : high + radius;
            Vector3 start = travelAxis * axisStart + lateralAxis * lateral;
            start.y = groundY + radius;

            lane = new BoulderLane
            {
                Start = start,
                Direction = isPositive ? travelAxis : -travelAxis,
                MaxDistance = high - low + radius * 2f,
                LaunchTime = launchTime
            };
            return true;
        }

        // Scans the NavMesh along the lane to find where walkable ground starts and ends; results are cached per lane
        private static bool TryGetWalkableRange(Bounds map, Dictionary<int, Vector2> walkableCache, Vector3 travelAxis, Vector3 lateralAxis,
            float groundY, int wall, float lateral, out float low, out float high)
        {
            int key = wall * 100000 + Mathf.RoundToInt(lateral / LateralKeyResolution);
            if (walkableCache.TryGetValue(key, out Vector2 cached))
            {
                low = cached.x;
                high = cached.y;
                return low <= high;
            }

            // The map is a box in world space, so project its corners onto the travel axis to get the scan range
            float axisMin = float.MaxValue;
            float axisMax = float.MinValue;
            for (int corner = 0; corner < 4; corner++)
            {
                float x = (corner & 1) == 0 ? map.min.x : map.max.x;
                float z = (corner & 2) == 0 ? map.min.z : map.max.z;
                float projected = Vector3.Dot(new Vector3(x, 0f, z), travelAxis);
                axisMin = Mathf.Min(axisMin, projected);
                axisMax = Mathf.Max(axisMax, projected);
            }

            low = float.MaxValue;
            high = float.MinValue;
            for (float axis = axisMin; axis <= axisMax; axis += WalkableScanStep)
            {
                Vector3 point = travelAxis * axis + lateralAxis * lateral;
                point.y = groundY;
                if (!IsWalkable(point))
                    continue;

                low = Mathf.Min(low, axis);
                high = Mathf.Max(high, axis);
            }

            walkableCache[key] = new Vector2(low, high);
            return low <= high;
        }

        // Only ground-level walkable terrain counts, not raised patches on top of the walls
        private static bool IsWalkable(Vector3 point)
        {
            if (!NavMesh.SamplePosition(point, out NavMeshHit hit, WalkableSampleRadius, NavMesh.AllAreas))
                return false;

            float dx = hit.position.x - point.x;
            float dz = hit.position.z - point.z;
            return dx * dx + dz * dz < WalkableHorizontalTolerance * WalkableHorizontalTolerance;
        }

        // Simulates both boulders and reports whether their centers ever get closer than touching distance
        private static bool CollidesWithAny(BoulderLane candidate, List<BoulderLane> accepted, float radius, float speed)
        {
            float minDistance = radius * 2f + CollisionMargin;
            foreach (BoulderLane other in accepted)
            {
                float from = Mathf.Max(candidate.LaunchTime, other.LaunchTime);
                float to = Mathf.Min(candidate.LaunchTime + candidate.MaxDistance / speed, other.LaunchTime + other.MaxDistance / speed);
                for (float time = from; time <= to; time += SimulationStep)
                {
                    Vector3 a = candidate.Start + candidate.Direction * (speed * (time - candidate.LaunchTime));
                    Vector3 b = other.Start + other.Direction * (speed * (time - other.LaunchTime));
                    if ((a - b).sqrMagnitude < minDistance * minDistance)
                        return true;
                }
            }
            return false;
        }

        // Horizontal on screen is the camera's right axis and vertical is its forward axis, both flattened onto the ground
        private static void GetScreenAxes(out Vector3 screenRight, out Vector3 screenForward)
        {
            screenRight = Vector3.right;
            Camera camera = Camera.main;
            if (camera)
            {
                Vector3 flatRight = new Vector3(camera.transform.right.x, 0f, camera.transform.right.z);
                if (flatRight.sqrMagnitude > 0.0001f)
                    screenRight = flatRight.normalized;
            }

            screenForward = Vector3.Cross(screenRight, Vector3.up);
        }

        // Walls 0 and 1 travel along the screen's horizontal axis, walls 2 and 3 along its vertical axis
        private static void GetWallAxes(int wall, Vector3 screenRight, Vector3 screenForward, out Vector3 travelAxis,
            out Vector3 lateralAxis, out bool isPositive)
        {
            travelAxis = wall < 2 ? screenRight : screenForward;
            lateralAxis = wall < 2 ? screenForward : screenRight;
            isPositive = wall % 2 == 0;
        }

        private static float GetPlayerLateral(Vector3 playerPosition, int wall, Vector3 screenRight, Vector3 screenForward) =>
            Vector3.Dot(playerPosition, wall < 2 ? screenForward : screenRight);
    }
}
