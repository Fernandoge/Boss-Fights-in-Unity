using System.Collections.Generic;
using Cameras;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

namespace EditorTools
{
    // Makes the walkable floor of a forest level end where the follow camera stops seeing the character: every cell of the floor where the character's feet, chest and head are not all clear of the canopy becomes Not Walkable.
    // Needs one NavMeshSurface, a Camera.main with a CameraController (its player is the start position) and trees with a LODGroup. See docs/level-design.md.
    public static class VisibleFloorBaker
    {
        public const string BlockersName = "Visibility Blockers";
        private const float CellSize = 1f;
        private const float ProbeHeightOffset = 0.08f;
        private static readonly float[] CharacterHeights = { 0.5f, 1.2f, 1.9f };

        [MenuItem("Tools/Level/Bake Visible Floor")]
        private static void RunFromMenu() => EditorUtility.DisplayDialog("Bake Visible Floor", Run(), "OK");

        public static string Run()
        {
            NavMeshSurface surface = Object.FindFirstObjectByType<NavMeshSurface>();
            CameraController cameraController = Camera.main ? Camera.main.GetComponent<CameraController>() : null;
            if (!surface || !cameraController || !cameraController.player)
                return "Needs a NavMeshSurface and a Camera.main with a CameraController that has a player.";

            Transform oldBlockers = surface.transform.Find(BlockersName);
            if (oldBlockers)
                Object.DestroyImmediate(oldBlockers.gameObject);

            string navMeshPath = AssetDatabase.GetAssetPath(surface.navMeshData);
            surface.BuildNavMesh();
            Vector3 cameraOffset = Camera.main.transform.position - cameraController.player.position;
            List<Collider> canopyColliders = AddCanopyColliders();
            HashSet<Collider> canopy = new HashSet<Collider>(canopyColliders);
            Physics.SyncTransforms();

            NavMeshTriangulation triangulation = NavMesh.CalculateTriangulation();
            Bounds bounds = new Bounds(triangulation.vertices[0], Vector3.zero);
            foreach (Vector3 vertex in triangulation.vertices)
                bounds.Encapsulate(vertex);

            int width = Mathf.CeilToInt(bounds.size.x / CellSize);
            int depth = Mathf.CeilToInt(bounds.size.z / CellSize);
            bool[,] onFloor = new bool[width, depth];
            bool[,] isVisible = new bool[width, depth];
            for (int z = 0; z < depth; z++)
            {
                for (int x = 0; x < width; x++)
                {
                    Vector3 point = CellCenter(bounds, x, z);
                    if (!NavMesh.SamplePosition(point, out NavMeshHit hit, 0.3f, NavMesh.AllAreas))
                        continue;

                    onFloor[x, z] = true;
                    isVisible[x, z] = IsCharacterVisible(point, cameraOffset, canopy);
                }
            }

            foreach (Collider collider in canopyColliders)
                Object.DestroyImmediate(collider);

            bool[,] reachable = FloodFill(isVisible, width, depth, bounds, cameraController.player.position);
            int blockerCount = CreateBlockers(surface.transform, bounds, onFloor, reachable, width, depth);
            surface.BuildNavMesh();
            SaveNavMeshData(surface, navMeshPath);
            EditorUtility.SetDirty(surface);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(surface.gameObject.scene);
            return "Done: " + blockerCount + " blockers. Save the scene.";
        }

        private static Vector3 CellCenter(Bounds bounds, int x, int z) =>
            new Vector3(bounds.min.x + (x + 0.5f) * CellSize, ProbeHeightOffset, bounds.min.z + (z + 0.5f) * CellSize);

        // The trees only have a trunk collider, so the canopy gets a temporary mesh collider to ray against
        private static List<Collider> AddCanopyColliders()
        {
            List<Collider> colliders = new List<Collider>();
            foreach (LODGroup lodGroup in Object.FindObjectsByType<LODGroup>(FindObjectsSortMode.None))
            {
                foreach (Renderer treeRenderer in lodGroup.GetLODs()[0].renderers)
                {
                    MeshFilter meshFilter = treeRenderer ? treeRenderer.GetComponent<MeshFilter>() : null;
                    if (!meshFilter || !meshFilter.sharedMesh)
                        continue;

                    MeshCollider meshCollider = treeRenderer.gameObject.AddComponent<MeshCollider>();
                    meshCollider.sharedMesh = meshFilter.sharedMesh;
                    colliders.Add(meshCollider);
                }
            }

            return colliders;
        }

        private static bool IsCharacterVisible(Vector3 point, Vector3 cameraOffset, HashSet<Collider> canopy)
        {
            foreach (float height in CharacterHeights)
            {
                Vector3 target = point + Vector3.up * height;
                Vector3 origin = target + cameraOffset;
                Vector3 direction = target - origin;
                float distance = direction.magnitude;
                foreach (RaycastHit hit in Physics.RaycastAll(origin, direction / distance, distance - 0.05f, ~0, QueryTriggerInteraction.Ignore))
                    if (canopy.Contains(hit.collider))
                        return false;
            }

            return true;
        }

        // Only the visible cells connected to the start stay walkable, so no pocket of floor is left behind the trees
        private static bool[,] FloodFill(bool[,] isVisible, int width, int depth, Bounds bounds, Vector3 start)
        {
            bool[,] reachable = new bool[width, depth];
            int startX = Mathf.Clamp(Mathf.FloorToInt((start.x - bounds.min.x) / CellSize), 0, width - 1);
            int startZ = Mathf.Clamp(Mathf.FloorToInt((start.z - bounds.min.z) / CellSize), 0, depth - 1);
            if (!isVisible[startX, startZ])
                return reachable;

            Queue<Vector2Int> queue = new Queue<Vector2Int>();
            queue.Enqueue(new Vector2Int(startX, startZ));
            reachable[startX, startZ] = true;
            Vector2Int[] steps = { Vector2Int.right, Vector2Int.left, Vector2Int.up, Vector2Int.down };
            while (queue.Count > 0)
            {
                Vector2Int cell = queue.Dequeue();
                foreach (Vector2Int step in steps)
                {
                    Vector2Int next = cell + step;
                    if (next.x < 0 || next.y < 0 || next.x >= width || next.y >= depth || reachable[next.x, next.y] || !isVisible[next.x, next.y])
                        continue;

                    reachable[next.x, next.y] = true;
                    queue.Enqueue(next);
                }
            }

            return reachable;
        }

        // Greedy rectangles over the blocked cells, so the scene gets a few hundred boxes and not a few thousand
        private static int CreateBlockers(Transform parent, Bounds bounds, bool[,] onFloor, bool[,] reachable, int width, int depth)
        {
            GameObject root = new GameObject(BlockersName);
            root.transform.SetParent(parent, false);
            bool[,] used = new bool[width, depth];
            int count = 0;
            for (int z = 0; z < depth; z++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (!IsBlocked(x, z, onFloor, reachable, used))
                        continue;

                    int runWidth = 1;
                    while (x + runWidth < width && IsBlocked(x + runWidth, z, onFloor, reachable, used))
                        runWidth++;

                    int runDepth = 1;
                    while (z + runDepth < depth && RowIsBlocked(x, z + runDepth, runWidth, onFloor, reachable, used))
                        runDepth++;

                    for (int dz = 0; dz < runDepth; dz++)
                        for (int dx = 0; dx < runWidth; dx++)
                            used[x + dx, z + dz] = true;

                    GameObject blocker = new GameObject("Blocker " + count);
                    blocker.transform.SetParent(root.transform, false);
                    blocker.transform.position = new Vector3(bounds.min.x + (x + runWidth * 0.5f) * CellSize, 0f, bounds.min.z + (z + runDepth * 0.5f) * CellSize);
                    NavMeshModifierVolume volume = blocker.AddComponent<NavMeshModifierVolume>();
                    volume.size = new Vector3(runWidth * CellSize + 0.1f, 10f, runDepth * CellSize + 0.1f);
                    volume.center = Vector3.zero;
                    volume.area = 1;
                    count++;
                }
            }

            return count;
        }

        private static bool IsBlocked(int x, int z, bool[,] onFloor, bool[,] reachable, bool[,] used) =>
            onFloor[x, z] && !reachable[x, z] && !used[x, z];

        private static bool RowIsBlocked(int startX, int z, int length, bool[,] onFloor, bool[,] reachable, bool[,] used)
        {
            for (int i = 0; i < length; i++)
                if (!IsBlocked(startX + i, z, onFloor, reachable, used))
                    return false;

            return true;
        }

        // BuildNavMesh makes a new in-memory NavMeshData, so it is saved over the asset the surface used before the bake
        private static void SaveNavMeshData(NavMeshSurface surface, string path)
        {
            if (string.IsNullOrEmpty(path))
                path = "Assets/Scenes/" + surface.gameObject.scene.name + "/NavMesh-" + surface.name + ".asset";

            AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(surface.navMeshData, path);
            AssetDatabase.SaveAssets();
        }
    }
}
