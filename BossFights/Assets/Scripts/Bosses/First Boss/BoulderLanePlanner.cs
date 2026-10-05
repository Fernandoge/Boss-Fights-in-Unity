using System.Collections.Generic;
using UnityEngine;

namespace Bosses.First_Boss
{
    public struct BoulderLane
    {
        public Vector3 Start;
        public Vector3 Direction;
        public float MaxDistance;
        public float LaunchTime;
    }

    // Plans boulder lanes that start at the arena walls and never let two boulders meet
    public static class BoulderLanePlanner
    {
        private const int MaxAttempts = 30;
        private const int CandidatesPerLane = 30;
        private const int MinMixedWallLanes = 3;
        private const float SimulationStep = 0.1f;
        private const float CollisionMargin = 1.5f;
        private const float FallbackLaneGap = 4f;

        private static readonly Vector3[] Directions = { Vector3.right, Vector3.left, Vector3.forward, Vector3.back };

        public static BoulderLane[] Plan(Bounds arena, Vector3 playerPosition, int count, float radius, float speed,
            float firstLaunchTime, float launchInterval, float aimSpread, out bool usedFallback)
        {
            usedFallback = false;
            List<BoulderLane> best = new List<BoulderLane>();

            // Build the set one boulder at a time, so every new lane is checked against the ones already accepted
            for (int attempt = 0; attempt < MaxAttempts; attempt++)
            {
                List<BoulderLane> lanes = new List<BoulderLane>();
                for (int i = 0; i < count; i++)
                {
                    for (int candidateTry = 0; candidateTry < CandidatesPerLane; candidateTry++)
                    {
                        int wall = Random.Range(0, Directions.Length);
                        float lateral = GetPlayerLateral(playerPosition, wall) + Random.Range(-aimSpread, aimSpread);
                        BoulderLane candidate = CreateLane(arena, wall, lateral, radius, firstLaunchTime + i * launchInterval);

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

            // Prefer a mixed-wall set with a boulder or two fewer over parallel lanes from a single wall
            if (best.Count >= Mathf.Min(count, MinMixedWallLanes))
                return best.ToArray();

            usedFallback = true;
            return PlanParallelFallback(arena, playerPosition, count, radius, firstLaunchTime, launchInterval);
        }

        // Parallel lanes from one wall can never cross, so this always produces a valid (possibly smaller) set
        private static BoulderLane[] PlanParallelFallback(Bounds arena, Vector3 playerPosition, int count, float radius,
            float firstLaunchTime, float launchInterval)
        {
            int wall = Random.Range(0, Directions.Length);
            float pitch = radius * 2f + FallbackLaneGap;
            float min = GetLateralMin(arena, wall) + radius;
            float max = GetLateralMax(arena, wall) - radius;
            int fitting = Mathf.Max(1, Mathf.FloorToInt((max - min) / pitch) + 1);
            int laneCount = Mathf.Min(count, fitting);

            float center = Mathf.Clamp(GetPlayerLateral(playerPosition, wall), min + (laneCount - 1) * pitch / 2f, max - (laneCount - 1) * pitch / 2f);
            BoulderLane[] lanes = new BoulderLane[laneCount];
            for (int i = 0; i < laneCount; i++)
            {
                float lateral = center + (i - (laneCount - 1) / 2f) * pitch;
                lanes[i] = CreateLane(arena, wall, lateral, radius, firstLaunchTime + i * launchInterval);
            }
            return lanes;
        }

        private static BoulderLane CreateLane(Bounds arena, int wall, float lateral, float radius, float launchTime)
        {
            lateral = Mathf.Clamp(lateral, GetLateralMin(arena, wall) + radius, GetLateralMax(arena, wall) - radius);
            float groundY = arena.max.y + radius;

            Vector3 start;
            float span;
            switch (wall)
            {
                case 0: // from the min x wall, rolling +x
                    start = new Vector3(arena.min.x - radius, groundY, lateral);
                    span = arena.size.x;
                    break;
                case 1: // from the max x wall, rolling -x
                    start = new Vector3(arena.max.x + radius, groundY, lateral);
                    span = arena.size.x;
                    break;
                case 2: // from the min z wall, rolling +z
                    start = new Vector3(lateral, groundY, arena.min.z - radius);
                    span = arena.size.z;
                    break;
                default: // from the max z wall, rolling -z
                    start = new Vector3(lateral, groundY, arena.max.z + radius);
                    span = arena.size.z;
                    break;
            }

            return new BoulderLane
            {
                Start = start,
                Direction = Directions[wall],
                MaxDistance = span + radius * 2f,
                LaunchTime = launchTime
            };
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

        // Lanes from the x walls are spread along z, and lanes from the z walls are spread along x
        private static float GetPlayerLateral(Vector3 playerPosition, int wall) => wall < 2 ? playerPosition.z : playerPosition.x;

        private static float GetLateralMin(Bounds arena, int wall) => wall < 2 ? arena.min.z : arena.min.x;

        private static float GetLateralMax(Bounds arena, int wall) => wall < 2 ? arena.max.z : arena.max.x;
    }
}
