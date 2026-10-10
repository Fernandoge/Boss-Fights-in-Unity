using Manager.GameManager;
using UnityEngine;

namespace Bosses.Gorath
{
    public class FissureLine : MonoBehaviour
    {
        [Header("Spikes")]
        [SerializeField] private Material _spikeMaterial;
        [SerializeField] private float _spikeSpacing = 1.6f;
        [SerializeField] private Vector2 _spikeHeightRange = new Vector2(1.4f, 2.6f);
        [SerializeField] private Vector2 _spikeWidthRange = new Vector2(0.9f, 1.5f);
        [SerializeField] private float _maxLeanAngle = 12f;

        [Header("Timing")]
        [SerializeField] private float _riseTime = 0.12f;
        [SerializeField] private float _holdTime = 0.35f;
        [SerializeField] private float _sinkTime = 0.4f;

        [Header("Damage")]
        [SerializeField] private float _playerHitPadding = 0.4f;

        [Header("Dust")]
        [SerializeField] private GameObject _dustPrefab;
        [SerializeField] private float _dustSpacing = 4f;

        private static Mesh _spikeMesh;

        private Transform[] _spikes;
        private float[] _spikeHeights;
        private Vector3 _origin;
        private Vector3 _direction;
        private Vector3 _right;
        private float _length;
        private float _width;
        private float _elapsed;
        private bool _hasHitPlayer;
        private bool _isErupted;

        private void Update()
        {
            if (!_isErupted)
                return;

            _elapsed += Time.deltaTime;
            AnimateSpikes();

            // The line hurts from the moment it erupts until the spikes start sinking
            if (!_hasHitPlayer && _elapsed <= _riseTime + _holdTime)
                CheckPlayerHit();

            if (_elapsed >= _riseTime + _holdTime + _sinkTime)
                Destroy(gameObject);
        }

        public void Erupt(Vector3 origin, Vector3 direction, float length, float width)
        {
            _origin = origin;
            _direction = new Vector3(direction.x, 0f, direction.z).normalized;
            _right = Vector3.Cross(Vector3.up, _direction);
            _length = length;
            _width = width;

            SpawnSpikes();
            SpawnDust();
            _isErupted = true;
        }

        private void SpawnSpikes()
        {
            int spikeCount = Mathf.Max(1, Mathf.FloorToInt(_length / _spikeSpacing));
            _spikes = new Transform[spikeCount];
            _spikeHeights = new float[spikeCount];

            for (int i = 0; i < spikeCount; i++)
            {
                // Spikes start after the boss's feet and are jittered across the line width
                float distance = (i + 0.5f) * (_length / spikeCount);
                float lateral = Random.Range(-_width * 0.3f, _width * 0.3f);
                Vector3 position = _origin + _direction * distance + _right * lateral;

                float spikeWidth = Random.Range(_spikeWidthRange.x, _spikeWidthRange.y);
                float spikeHeight = Random.Range(_spikeHeightRange.x, _spikeHeightRange.y);

                GameObject spike = new GameObject("Spike");
                spike.transform.SetParent(transform);
                spike.transform.SetPositionAndRotation(
                    new Vector3(position.x, _origin.y - spikeHeight, position.z),
                    Quaternion.Euler(Random.Range(-_maxLeanAngle, _maxLeanAngle), Random.Range(0f, 360f), Random.Range(-_maxLeanAngle, _maxLeanAngle)));
                spike.transform.localScale = new Vector3(spikeWidth, spikeHeight, spikeWidth);
                spike.AddComponent<MeshFilter>().sharedMesh = GetSpikeMesh();
                spike.AddComponent<MeshRenderer>().sharedMaterial = _spikeMaterial;

                _spikes[i] = spike.transform;
                _spikeHeights[i] = spikeHeight;
            }
        }

        private void SpawnDust()
        {
            if (!_dustPrefab)
                return;

            for (float distance = 0f; distance <= _length; distance += _dustSpacing)
                Instantiate(_dustPrefab, _origin + _direction * distance, Quaternion.identity, transform);
        }

        // Spikes rise from below the ground, hold, then sink back down
        private void AnimateSpikes()
        {
            float height01;
            if (_elapsed < _riseTime)
                height01 = 1f - Mathf.Pow(1f - _elapsed / _riseTime, 3f);
            else if (_elapsed < _riseTime + _holdTime)
                height01 = 1f;
            else
                height01 = 1f - Mathf.Pow(Mathf.Clamp01((_elapsed - _riseTime - _holdTime) / _sinkTime), 2f);

            for (int i = 0; i < _spikes.Length; i++)
            {
                Vector3 position = _spikes[i].position;
                position.y = _origin.y - _spikeHeights[i] * (1f - height01);
                _spikes[i].position = position;
            }
        }

        private void CheckPlayerHit()
        {
            Vector3 toPlayer = GameManager.Instance.player.transform.position - _origin;
            float along = Vector3.Dot(toPlayer, _direction);
            float lateral = Mathf.Abs(Vector3.Dot(toPlayer, _right));

            if (along < 0f || along > _length || lateral > _width * 0.5f + _playerHitPadding)
                return;

            _hasHitPlayer = true;
            GameManager.Instance.player.DamagePlayer(GameManager.Instance.gorath.fissureDamage);
        }

        private static Mesh GetSpikeMesh()
        {
            if (_spikeMesh != null)
                return _spikeMesh;

            // Four-sided pyramid with its base at y = 0 and its tip at y = 1, flat shaded
            Vector3 tip = new Vector3(0f, 1f, 0f);
            Vector3 a = new Vector3(-0.5f, 0f, -0.5f);
            Vector3 b = new Vector3(0.5f, 0f, -0.5f);
            Vector3 c = new Vector3(0.5f, 0f, 0.5f);
            Vector3 d = new Vector3(-0.5f, 0f, 0.5f);

            Vector3[] vertices =
            {
                a, tip, b,
                b, tip, c,
                c, tip, d,
                d, tip, a
            };
            Vector2[] uvs =
            {
                new Vector2(0f, 0f), new Vector2(0.5f, 1f), new Vector2(1f, 0f),
                new Vector2(0f, 0f), new Vector2(0.5f, 1f), new Vector2(1f, 0f),
                new Vector2(0f, 0f), new Vector2(0.5f, 1f), new Vector2(1f, 0f),
                new Vector2(0f, 0f), new Vector2(0.5f, 1f), new Vector2(1f, 0f)
            };

            _spikeMesh = new Mesh { name = "FissureSpike" };
            _spikeMesh.vertices = vertices;
            _spikeMesh.uv = uvs;
            _spikeMesh.triangles = new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11 };
            _spikeMesh.RecalculateNormals();
            _spikeMesh.RecalculateBounds();
            return _spikeMesh;
        }
    }
}
