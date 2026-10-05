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

    // Plans boulder lanes along the screen's horizontal and vertical axes. Lanes in the same direction are kept apart by a guaranteed gap,
    // so arrows never share a row, crossing lanes are simulated so two boulders never meet, and every lane is swept with the boulder's
    // size so it only hits a wall at its far end. Each boulder starts inside the real wall at the end of its lane.
    public static class BoulderLanePlanner
    {
        private const int MaxMixedAttempts = 300;
        private const float SimulationStep = 0.1f;
        private const float CollisionMargin = 1.5f;
        private const float WalkableScanStep = 1.5f;
        private const float WalkableSampleRadius = 2.5f;
        private const float WalkableHorizontalTolerance = 0.75f;
        private const float LateralScanStep = 2f;
        private const float LateralKeyResolution = 1f;
        private const float MinLaneLength = 30f;
        private const float MinLaneGapFloor = 2f;
        private const float WallClearanceMargin = 1f;
        private const float EndZoneLength = 2f;

        public static Bounds GetNavMeshBounds()
        {
            NavMeshTriangulation triangulation = NavMesh.CalculateTriangulation();
            Bounds bounds = new Bounds(triangulation.vertices[0], Vector3.zero);
            foreach (Vector3 vertex in triangulation.vertices)
                bounds.Encapsulate(vertex);
            return bounds;
        }

        public static BoulderLane[] Plan(Bounds map, float groundY, int count, float radius, float speed, float minLaneGap,
            float firstLaunchTime, float launchInterval)
        {
            GetScreenAxes(out Vector3 screenRight, out Vector3 screenForward);
            LaneScanner scanner = new LaneScanner(map, screenRight, screenForward, groundY, radius);

            bool hasHorizontalRun = scanner.TryFindLateralRun(0, out float horizontalStart, out float horizontalEnd);
            bool hasVerticalRun = scanner.TryFindLateralRun(1, out float verticalStart, out float verticalEnd);

            // Mix horizontal and vertical lanes: random split, random positions and a random order, until no two boulders can collide
            if (count >= 2 && hasHorizontalRun && hasVerticalRun)
            {
                for (int attempt = 0; attempt < MaxMixedAttempts; attempt++)
                {
                    // Prefer a balanced split (2 + 3 for five boulders); lopsided splits only if balanced ones keep colliding
                    int horizontalCount = attempt < MaxMixedAttempts / 2 ? Random.Range(count / 2, (count + 1) / 2 + 1) : Random.Range(1, count);
                    List<BoulderLane> lanes = new List<BoulderLane>();
                    if (!TryPlaceParallelLanes(scanner, 0, horizontalStart, horizontalEnd, horizontalCount, radius, minLaneGap, lanes) ||
                        !TryPlaceParallelLanes(scanner, 1, verticalStart, verticalEnd, count - horizontalCount, radius, minLaneGap, lanes))
                        continue;

                    BoulderLane[] mixed = lanes.ToArray();
                    AssignRandomLaunchOrder(mixed, firstLaunchTime, launchInterval);
                    if (!HasCollision(mixed, radius, speed))
                        return mixed;
                }
            }

            // Fallback: parallel lanes in one direction cannot collide
            int firstOrientation = Random.Range(0, 2);
            BoulderLane[] best = new BoulderLane[0];
            for (int attempt = 0; attempt < 2; attempt++)
            {
                int orientation = (firstOrientation + attempt) % 2;
                bool hasRun = orientation == 0 ? hasHorizontalRun : hasVerticalRun;
                if (!hasRun)
                    continue;

                float runStart = orientation == 0 ? horizontalStart : verticalStart;
                float runEnd = orientation == 0 ? horizontalEnd : verticalEnd;
                BoulderLane[] lanes = PlanSingleOrientation(scanner, orientation, runStart, runEnd, count, radius, minLaneGap);
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

        // Spreads the lanes randomly over the widest usable stretch of the map, shrinking the gap to a floor and then using fewer lanes if needed
        private static BoulderLane[] PlanSingleOrientation(LaneScanner scanner, int orientation, float runStart, float runEnd, int count,
            float radius, float minLaneGap)
        {
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

            List<BoulderLane> lanes = new List<BoulderLane>();
            PlaceLanes(scanner, orientation, runStart, runEnd, laneCount, pitch, lanes);
            return lanes.ToArray();
        }

        // All-or-nothing: places count lanes with at least minLaneGap between their edges, or reports that they do not fit
        private static bool TryPlaceParallelLanes(LaneScanner scanner, int orientation, float runStart, float runEnd, int count, float radius,
            float minLaneGap, List<BoulderLane> lanes)
        {
            float pitch = radius * 2f + minLaneGap;
            if (runEnd - runStart < (count - 1) * pitch)
                return false;

            int before = lanes.Count;
            PlaceLanes(scanner, orientation, runStart, runEnd, count, pitch, lanes);
            return lanes.Count - before == count;
        }

        // Gives the leftover width to the gaps before, between and after the lanes at random; each lane gets a random travel direction
        private static void PlaceLanes(LaneScanner scanner, int orientation, float runStart, float runEnd, int count, float pitch,
            List<BoulderLane> lanes)
        {
            float slack = Mathf.Max(0f, runEnd - runStart - (count - 1) * pitch);
            float[] shares = new float[count + 1];
            float total = 0f;
            for (int i = 0; i < shares.Length; i++)
            {
                shares[i] = Random.value;
                total += shares[i];
            }

            float lateral = runStart;
            for (int i = 0; i < count; i++)
            {
                lateral += slack * shares[i] / total;
                if (scanner.TryCreateLane(orientation, lateral, Random.value < 0.5f, out BoulderLane lane))
                    lanes.Add(lane);
                lateral += pitch;
            }
        }

        // Simulates every pair of boulders and reports whether their centers ever get closer than touching distance
        private static bool HasCollision(BoulderLane[] lanes, float radius, float speed)
        {
            float minDistance = radius * 2f + CollisionMargin;
            for (int i = 0; i < lanes.Length; i++)
            {
                for (int j = i + 1; j < lanes.Length; j++)
                {
                    float from = Mathf.Max(lanes[i].LaunchTime, lanes[j].LaunchTime);
                    float to = Mathf.Min(lanes[i].LaunchTime + lanes[i].MaxDistance / speed, lanes[j].LaunchTime + lanes[j].MaxDistance / speed);
                    for (float time = from; time <= to; time += SimulationStep)
                    {
                        Vector3 a = lanes[i].Start + lanes[i].Direction * (speed * (time - lanes[i].LaunchTime));
                        Vector3 b = lanes[j].Start + lanes[j].Direction * (speed * (time - lanes[j].LaunchTime));
                        if ((a - b).sqrMagnitude < minDistance * minDistance)
                            return true;
                    }
                }
            }
            return false;
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

        // Finds usable lanes: long enough, with the boulder's whole path clear of walls and rocks until its far end.
        // Orientation 0 is horizontal on screen (lanes travel along the camera's right axis), orientation 1 is vertical.
        private sealed class LaneScanner
        {
            private readonly Bounds _map;
            private readonly Vector3 _screenRight;
            private readonly Vector3 _screenForward;
            private readonly float _groundY;
            private readonly float _radius;
            private readonly Dictionary<int, Vector2> _walkableCache = new Dictionary<int, Vector2>();
            private readonly Dictionary<int, bool> _clearCache = new Dictionary<int, bool>();

            public LaneScanner(Bounds map, Vector3 screenRight, Vector3 screenForward, float groundY, float radius)
            {
                _map = map;
                _screenRight = screenRight;
                _screenForward = screenForward;
                _groundY = groundY;
                _radius = radius;
            }

            // Finds the longest continuous stretch of lateral positions where a lane is usable
            public bool TryFindLateralRun(int orientation, out float runStart, out float runEnd)
            {
                GetProjectedRange(GetLateralAxis(orientation), out float lateralMin, out float lateralMax);

                runStart = 0f;
                runEnd = 0f;
                bool isFound = false;
                float currentStart = 0f;
                bool isInRun = false;
                for (float lateral = lateralMin; lateral <= lateralMax; lateral += LateralScanStep)
                {
                    bool isValid = IsUsable(orientation, lateral, out float low, out float high);
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
            public bool TryCreateLane(int orientation, float lateral, bool isPositive, out BoulderLane lane)
            {
                lane = default;
                if (!IsUsable(orientation, lateral, out float low, out float high))
                    return false;

                Vector3 travelAxis = GetTravelAxis(orientation);
                float axisStart = isPositive ? low - _radius : high + _radius;
                Vector3 start = travelAxis * axisStart + GetLateralAxis(orientation) * lateral;
                start.y = _groundY + _radius;

                lane = new BoulderLane
                {
                    Start = start,
                    Direction = isPositive ? travelAxis : -travelAxis,
                    MaxDistance = high - low + _radius * 2f
                };
                return true;
            }

            private bool IsUsable(int orientation, float lateral, out float low, out float high) =>
                TryGetWalkableRange(orientation, lateral, out low, out high) && high - low >= MinLaneLength &&
                IsPathClear(orientation, lateral, low, high);

            // Scans the NavMesh along the lane to find where walkable ground starts and ends; results are cached per lane
            private bool TryGetWalkableRange(int orientation, float lateral, out float low, out float high)
            {
                int key = orientation * 100000 + Mathf.RoundToInt(lateral / LateralKeyResolution);
                if (_walkableCache.TryGetValue(key, out Vector2 cached))
                {
                    low = cached.x;
                    high = cached.y;
                    return low <= high;
                }

                Vector3 travelAxis = GetTravelAxis(orientation);
                Vector3 lateralAxis = GetLateralAxis(orientation);
                GetProjectedRange(travelAxis, out float axisMin, out float axisMax);
                low = float.MaxValue;
                high = float.MinValue;
                for (float axis = axisMin; axis <= axisMax; axis += WalkableScanStep)
                {
                    Vector3 point = travelAxis * axis + lateralAxis * lateral;
                    point.y = _groundY;
                    if (!IsWalkable(point))
                        continue;

                    low = Mathf.Min(low, axis);
                    high = Mathf.Max(high, axis);
                }

                _walkableCache[key] = new Vector2(low, high);
                return low <= high;
            }

            // Sweeps the boulder (plus a margin) along the middle of the lane. The stretches next to both end walls are skipped: the boulder
            // starts inside the near wall and is supposed to break on the far wall. Anything hit in between is a wall clipping the lane.
            private bool IsPathClear(int orientation, float lateral, float low, float high)
            {
                int key = orientation * 100000 + Mathf.RoundToInt(lateral / LateralKeyResolution);
                if (_clearCache.TryGetValue(key, out bool cached))
                    return cached;

                float endMargin = _radius + EndZoneLength;
                float from = low + endMargin;
                float to = high - endMargin;
                bool isClear = true;
                if (to > from)
                {
                    Vector3 travelAxis = GetTravelAxis(orientation);
                    Vector3 origin = travelAxis * from + GetLateralAxis(orientation) * lateral;
                    origin.y = _groundY + _radius;
                    float sweepRadius = _radius + WallClearanceMargin;

                    foreach (Collider collider in Physics.OverlapSphere(origin, sweepRadius, ~0, QueryTriggerInteraction.Ignore))
                        if (collider.GetComponent<NavMeshObstacle>())
                            isClear = false;

                    foreach (RaycastHit hit in Physics.SphereCastAll(origin, sweepRadius, travelAxis, to - from, ~0, QueryTriggerInteraction.Ignore))
                        if (hit.collider.GetComponent<NavMeshObstacle>())
                            isClear = false;
                }

                _clearCache[key] = isClear;
                return isClear;
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
            private void GetProjectedRange(Vector3 axis, out float min, out float max)
            {
                min = float.MaxValue;
                max = float.MinValue;
                for (int corner = 0; corner < 4; corner++)
                {
                    float x = (corner & 1) == 0 ? _map.min.x : _map.max.x;
                    float z = (corner & 2) == 0 ? _map.min.z : _map.max.z;
                    float projected = Vector3.Dot(new Vector3(x, 0f, z), axis);
                    min = Mathf.Min(min, projected);
                    max = Mathf.Max(max, projected);
                }
            }

            private Vector3 GetTravelAxis(int orientation) => orientation == 0 ? _screenRight : _screenForward;

            private Vector3 GetLateralAxis(int orientation) => orientation == 0 ? _screenForward : _screenRight;
        }
    }
}
