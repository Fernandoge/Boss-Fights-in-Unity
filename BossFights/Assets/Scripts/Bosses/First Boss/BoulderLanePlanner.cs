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

    // Plans parallel boulder lanes along the screen's horizontal or vertical axis. Lanes are spread across the map with a guaranteed gap
    // between them, so arrows never overlap and boulders can never meet. Each boulder starts inside the real wall at the end of its lane.
    public static class BoulderLanePlanner
    {
        private const float WalkableScanStep = 1.5f;
        private const float WalkableSampleRadius = 2.5f;
        private const float WalkableHorizontalTolerance = 0.75f;
        private const float LateralScanStep = 2f;
        private const float LateralKeyResolution = 1f;
        private const float MinLaneLength = 30f;
        private const float MinLaneGapFloor = 2f;

        public static Bounds GetNavMeshBounds()
        {
            NavMeshTriangulation triangulation = NavMesh.CalculateTriangulation();
            Bounds bounds = new Bounds(triangulation.vertices[0], Vector3.zero);
            foreach (Vector3 vertex in triangulation.vertices)
                bounds.Encapsulate(vertex);
            return bounds;
        }

        public static BoulderLane[] Plan(Bounds map, float groundY, int count, float radius, float minLaneGap, float firstLaunchTime,
            float launchInterval)
        {
            GetScreenAxes(out Vector3 screenRight, out Vector3 screenForward);
            Dictionary<int, Vector2> walkableCache = new Dictionary<int, Vector2>();

            // Pick horizontal or vertical lanes at random; if one orientation cannot fit all lanes, try the other and keep the best
            int firstOrientation = Random.Range(0, 2);
            BoulderLane[] best = new BoulderLane[0];
            for (int attempt = 0; attempt < 2; attempt++)
            {
                int orientation = (firstOrientation + attempt) % 2;
                BoulderLane[] lanes = PlanOrientation(map, walkableCache, screenRight, screenForward, orientation, groundY, count, radius,
                    minLaneGap);
                if (lanes.Length == count)
                {
                    best = lanes;
                    break;
                }
                if (lanes.Length > best.Length)
                    best = lanes;
            }

            AssignRandomLaunchOrder(best, firstLaunchTime, launchInterval);
            return best;
        }

        // Spreads the lanes randomly over the widest stretch of the map where lanes are long enough, keeping at least minLaneGap between them
        private static BoulderLane[] PlanOrientation(Bounds map, Dictionary<int, Vector2> walkableCache, Vector3 screenRight,
            Vector3 screenForward, int orientation, float groundY, int count, float radius, float minLaneGap)
        {
            Vector3 travelAxis = orientation == 0 ? screenRight : screenForward;
            Vector3 lateralAxis = orientation == 0 ? screenForward : screenRight;

            if (!TryFindLateralRun(map, walkableCache, travelAxis, lateralAxis, orientation, groundY, out float runStart, out float runEnd))
                return new BoulderLane[0];

            // If the stretch is too narrow for the full gap, shrink the gap down to a floor, then use fewer lanes
            float span = runEnd - runStart;
            float pitch = radius * 2f + minLaneGap;
            int laneCount = count;
            if (laneCount > 1 && span < (laneCount - 1) * pitch)
            {
                float shrunkPitch = span / (laneCount - 1);
                if (shrunkPitch >= radius * 2f + MinLaneGapFloor)
                    pitch = shrunkPitch;
                else
                {
                    pitch = radius * 2f + MinLaneGapFloor;
                    laneCount = Mathf.Max(1, Mathf.FloorToInt(span / pitch) + 1);
                }
            }

            // Give the leftover width to the gaps before, between and after the lanes at random
            float slack = Mathf.Max(0f, span - (laneCount - 1) * pitch);
            float[] shares = new float[laneCount + 1];
            float total = 0f;
            for (int i = 0; i < shares.Length; i++)
            {
                shares[i] = Random.value;
                total += shares[i];
            }

            List<BoulderLane> lanes = new List<BoulderLane>();
            float lateral = runStart;
            for (int i = 0; i < laneCount; i++)
            {
                lateral += slack * shares[i] / total;
                bool isPositive = Random.value < 0.5f;
                if (TryCreateLane(map, walkableCache, travelAxis, lateralAxis, orientation, groundY, lateral, isPositive, radius, out BoulderLane lane))
                    lanes.Add(lane);
                lateral += pitch;
            }
            return lanes.ToArray();
        }

        // Finds the longest continuous stretch of lateral positions where a lane would be long enough
        private static bool TryFindLateralRun(Bounds map, Dictionary<int, Vector2> walkableCache, Vector3 travelAxis, Vector3 lateralAxis,
            int orientation, float groundY, out float runStart, out float runEnd)
        {
            GetProjectedRange(map, lateralAxis, out float lateralMin, out float lateralMax);

            runStart = 0f;
            runEnd = 0f;
            bool isFound = false;
            float currentStart = 0f;
            bool isInRun = false;
            for (float lateral = lateralMin; lateral <= lateralMax; lateral += LateralScanStep)
            {
                bool isValid = TryGetWalkableRange(map, walkableCache, travelAxis, lateralAxis, orientation, groundY, lateral, out float low, out float high) &&
                               high - low >= MinLaneLength;
                if (isValid && !isInRun)
                {
                    isInRun = true;
                    currentStart = lateral;
                }

                // A run ends at the last valid lateral position before the first invalid one (or at the end of the scan)
                bool isRunEnding = isInRun && (!isValid || lateral + LateralScanStep > lateralMax);
                if (!isRunEnding)
                    continue;

                float currentEnd = isValid ? lateral : lateral - LateralScanStep;
                isInRun = false;
                if (!isFound || currentEnd - currentStart > runEnd - runStart)
                {
                    runStart = currentStart;
                    runEnd = currentEnd;
                    isFound = true;
                }
            }
            return isFound;
        }

        // The boulder starts one radius inside the wall, where the walkable ground begins along the lane, and ends past the far wall
        private static bool TryCreateLane(Bounds map, Dictionary<int, Vector2> walkableCache, Vector3 travelAxis, Vector3 lateralAxis,
            int orientation, float groundY, float lateral, bool isPositive, float radius, out BoulderLane lane)
        {
            lane = default;
            if (!TryGetWalkableRange(map, walkableCache, travelAxis, lateralAxis, orientation, groundY, lateral, out float low, out float high) ||
                high - low < MinLaneLength)
                return false;

            float axisStart = isPositive ? low - radius : high + radius;
            Vector3 start = travelAxis * axisStart + lateralAxis * lateral;
            start.y = groundY + radius;

            lane = new BoulderLane
            {
                Start = start,
                Direction = isPositive ? travelAxis : -travelAxis,
                MaxDistance = high - low + radius * 2f
            };
            return true;
        }

        // Scans the NavMesh along the lane to find where walkable ground starts and ends; results are cached per lane
        private static bool TryGetWalkableRange(Bounds map, Dictionary<int, Vector2> walkableCache, Vector3 travelAxis, Vector3 lateralAxis,
            int orientation, float groundY, float lateral, out float low, out float high)
        {
            int key = orientation * 100000 + Mathf.RoundToInt(lateral / LateralKeyResolution);
            if (walkableCache.TryGetValue(key, out Vector2 cached))
            {
                low = cached.x;
                high = cached.y;
                return low <= high;
            }

            GetProjectedRange(map, travelAxis, out float axisMin, out float axisMax);
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

        // The map is a box in world space, so project its corners onto an axis to get the range along it
        private static void GetProjectedRange(Bounds map, Vector3 axis, out float min, out float max)
        {
            min = float.MaxValue;
            max = float.MinValue;
            for (int corner = 0; corner < 4; corner++)
            {
                float x = (corner & 1) == 0 ? map.min.x : map.max.x;
                float z = (corner & 2) == 0 ? map.min.z : map.max.z;
                float projected = Vector3.Dot(new Vector3(x, 0f, z), axis);
                min = Mathf.Min(min, projected);
                max = Mathf.Max(max, projected);
            }
        }

        // Shuffles the lanes so the order the arrows appear in (and the boulders spawn in) is random
        private static void AssignRandomLaunchOrder(BoulderLane[] lanes, float firstLaunchTime, float launchInterval)
        {
            for (int i = lanes.Length - 1; i > 0; i--)
            {
                int swapIndex = Random.Range(0, i + 1);
                (lanes[i], lanes[swapIndex]) = (lanes[swapIndex], lanes[i]);
            }

            for (int i = 0; i < lanes.Length; i++)
                lanes[i].LaunchTime = firstLaunchTime + i * launchInterval;
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
    }
}
