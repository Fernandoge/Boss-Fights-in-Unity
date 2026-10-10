using System;
using System.Collections.Generic;
using Manager.GameManager;
using Shared;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Bosses.Vexara
{
    [Serializable]
    public class LineBlastSettings
    {
        public Vector2Int layerCountRange = new Vector2Int(2, 3);
        public Vector2 spacingRange = new Vector2(2.4f, 3.4f);
        public Vector2 widthRange = new Vector2(0.55f, 0.95f);
        [Range(0f, 1f)] public float dropChance = 0.1f;
        [Range(0f, 1f)] public float perpendicularChance = 0.35f;
        // World X is horizontal on screen and Z is vertical; the camera flattens Z, so lines need more room from horizontal
        [Range(0f, 45f)] public float minAngleFromHorizontal = 30f;
        [Range(0f, 45f)] public float minAngleFromVertical = 15f;
        public float telegraphTime = 1.4f;
        public int damage = 1;

        public LineBlastSettings WithTelegraphTime(float time)
        {
            LineBlastSettings copy = (LineBlastSettings)MemberwiseClone();
            copy.telegraphTime = time;
            return copy;
        }
    }

    // Several sets of parallel diagonal lines across the arena (sets cross and overlap): every line is telegraphed at once and they all explode together
    public class DiagonalLineBlast : MonoBehaviour
    {
        [SerializeField] private SkillIndicator _telegraphPrefab;
        [SerializeField] private SkillIndicator _flashPrefab;
        [SerializeField] private float _playerHitPadding = 0.25f;
        [SerializeField] private float _lifetimeAfterBlast = 0.5f;

        private readonly List<LineLayer> _layers = new List<LineLayer>();
        private readonly List<LineSegment> _segments = new List<LineSegment>();
        private readonly List<SkillIndicator> _telegraphs = new List<SkillIndicator>();
        private Vector3 _center;
        private float _telegraphTime;
        private float _elapsed;
        private int _damage;
        private bool _hasBlasted;

        private void Update()
        {
            _elapsed += Time.deltaTime;

            if (!_hasBlasted && _elapsed >= _telegraphTime)
                Blast();

            if (_hasBlasted && _elapsed >= _telegraphTime + _lifetimeAfterBlast)
                Destroy(gameObject);
        }

        // arenaRect is (minX, minZ, maxX, maxZ); the lines are clipped to it
        public void Begin(Vector4 arenaRect, float groundY, LineBlastSettings settings)
        {
            _telegraphTime = settings.telegraphTime;
            _damage = settings.damage;

            float halfX = (arenaRect.z - arenaRect.x) * 0.5f;
            float halfZ = (arenaRect.w - arenaRect.y) * 0.5f;
            _center = new Vector3((arenaRect.x + arenaRect.z) * 0.5f, groundY, (arenaRect.y + arenaRect.w) * 0.5f);

            int layerCount = Random.Range(settings.layerCountRange.x, settings.layerCountRange.y + 1);
            float angle = 0f;
            for (int i = 0; i < layerCount; i++)
            {
                // Later layers are sparser so stacking them never closes every gap
                float sparseScale = 1f + 0.35f * i;

                // Each layer has its own angle, never close to horizontal or vertical; sometimes it crosses the previous one at exactly 90 degrees
                if (i > 0 && Random.value < settings.perpendicularChance && IsAngleAllowed(angle + 90f, settings))
                    angle += 90f;
                else
                    angle = GetRandomAngle(settings);

                Vector3 direction = new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad), 0f, Mathf.Sin(angle * Mathf.Deg2Rad));
                BuildLayer(direction, Random.Range(settings.spacingRange.x, settings.spacingRange.y) * sparseScale,
                    Random.Range(settings.widthRange.x, settings.widthRange.y), settings.dropChance * (1f + i), halfX, halfZ);
            }
        }

        public bool DebugIsPositionHit(Vector3 position) => IsInsideAnyLine(position);

        // Share of the arena that is safe, sampled on a grid (for tuning)
        public float DebugGetSafeFraction(Vector4 arenaRect)
        {
            int safe = 0;
            int total = 0;
            for (float x = arenaRect.x + 0.5f; x < arenaRect.z - 0.5f; x += 0.4f)
                for (float z = arenaRect.y + 0.5f; z < arenaRect.w - 0.5f; z += 0.4f)
                {
                    total++;
                    if (!IsInsideAnyLine(new Vector3(x, _center.y, z)))
                        safe++;
                }

            return total == 0 ? 0f : (float)safe / total;
        }

        private static float GetRandomAngle(LineBlastSettings settings)
        {
            float angle = Random.Range(settings.minAngleFromHorizontal, 90f - settings.minAngleFromVertical);
            return Random.value < 0.5f ? angle : 180f - angle;
        }

        private static bool IsAngleAllowed(float angle, LineBlastSettings settings)
        {
            float wrapped = Mathf.Repeat(angle, 180f);
            return wrapped >= settings.minAngleFromHorizontal && wrapped <= 180f - settings.minAngleFromHorizontal
                   && Mathf.Abs(wrapped - 90f) >= settings.minAngleFromVertical;
        }

        private void BuildLayer(Vector3 direction, float spacing, float width, float dropChance, float halfX, float halfZ)
        {
            LineLayer layer = new LineLayer
            {
                direction = new Vector3(direction.x, 0f, direction.z).normalized,
                spacing = spacing,
                width = width
            };
            layer.normal = new Vector3(-layer.direction.z, 0f, layer.direction.x);

            // Lines are spread across the arena along the normal; the random phase moves where the safe gaps are
            float reach = Mathf.Abs(layer.normal.x) * halfX + Mathf.Abs(layer.normal.z) * halfZ;
            float phase = Random.Range(0f, spacing);
            layer.firstOffset = -reach + phase;
            int lineCount = Mathf.FloorToInt((2f * reach - phase) / spacing) + 1;
            layer.present = new bool[lineCount];

            for (int i = 0; i < lineCount; i++)
            {
                if (Random.value < dropChance)
                    continue;

                if (!TryClipLine(layer, layer.firstOffset + i * spacing, halfX, halfZ, out Vector3 start, out float length))
                    continue;

                layer.present[i] = true;
                SkillIndicator telegraph = Instantiate(_telegraphPrefab);
                telegraph.ShowLine(start, layer.direction, length, width, _telegraphTime, false);
                _telegraphs.Add(telegraph);
                _segments.Add(new LineSegment { start = start, direction = layer.direction, length = length, width = width });
            }

            _layers.Add(layer);
        }

        private bool TryClipLine(LineLayer layer, float offset, float halfX, float halfZ, out Vector3 start, out float length)
        {
            Vector3 point = _center + layer.normal * offset;
            float tMin = float.NegativeInfinity;
            float tMax = float.PositiveInfinity;
            start = point;
            length = 0f;

            if (!ClipAxis(point.x - _center.x, layer.direction.x, halfX, ref tMin, ref tMax) || !ClipAxis(point.z - _center.z, layer.direction.z, halfZ, ref tMin, ref tMax))
                return false;

            if (tMax - tMin < 0.5f)
                return false;

            start = point + layer.direction * tMin;
            length = tMax - tMin;
            return true;
        }

        private static bool ClipAxis(float position, float direction, float half, ref float tMin, ref float tMax)
        {
            if (Mathf.Abs(direction) < 0.00001f)
                return Mathf.Abs(position) <= half;

            float t1 = (-half - position) / direction;
            float t2 = (half - position) / direction;
            tMin = Mathf.Max(tMin, Mathf.Min(t1, t2));
            tMax = Mathf.Min(tMax, Mathf.Max(t1, t2));
            return true;
        }

        private void Blast()
        {
            _hasBlasted = true;

            foreach (SkillIndicator telegraph in _telegraphs)
                if (telegraph)
                    telegraph.Hide();

            foreach (LineSegment segment in _segments)
                Instantiate(_flashPrefab).ShowLine(segment.start, segment.direction, segment.length, segment.width * 1.2f, 0.01f);

            if (IsInsideAnyLine(GameManager.Instance.player.transform.position))
                GameManager.Instance.player.DamagePlayer(_damage);
        }

        private bool IsInsideAnyLine(Vector3 position)
        {
            foreach (LineLayer layer in _layers)
                if (IsInsideLayer(layer, position))
                    return true;

            return false;
        }

        // Lines in a layer are evenly spaced, so only the nearest one needs checking
        private bool IsInsideLayer(LineLayer layer, Vector3 position)
        {
            float offset = Vector3.Dot(position - _center, layer.normal);
            int nearest = Mathf.Clamp(Mathf.RoundToInt((offset - layer.firstOffset) / layer.spacing), 0, layer.present.Length - 1);
            return layer.present[nearest] && Mathf.Abs(offset - (layer.firstOffset + nearest * layer.spacing)) <= layer.width * 0.5f + _playerHitPadding;
        }

        private class LineLayer
        {
            public Vector3 direction;
            public Vector3 normal;
            public float firstOffset;
            public float spacing;
            public float width;
            public bool[] present;
        }

        private struct LineSegment
        {
            public Vector3 start;
            public Vector3 direction;
            public float length;
            public float width;
        }
    }
}
