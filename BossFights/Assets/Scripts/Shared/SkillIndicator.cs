using UnityEngine;

namespace Shared
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class SkillIndicator : MonoBehaviour
    {
        [SerializeField] private float _groundOffset = 0.05f;
        [SerializeField] private float _lingerAfterFill = 0.1f;
        [SerializeField] private float _maxArrowLaneLifetime = 20f;

        private static Mesh _lineMesh;
        private static readonly int Fill_Amount = Shader.PropertyToID("_Fill");
        private static readonly int Indicator_Size = Shader.PropertyToID("_Size");
        private static readonly int Pattern_Mode = Shader.PropertyToID("_Pattern");

        private MeshRenderer _meshRenderer;
        private MaterialPropertyBlock _propertyBlock;
        private float _duration;
        private float _elapsed;
        private bool _isArrowLane;
        private bool _hideWhenFilled = true;

        private void Awake()
        {
            _meshRenderer = GetComponent<MeshRenderer>();
            _propertyBlock = new MaterialPropertyBlock();
            GetComponent<MeshFilter>().sharedMesh = GetLineMesh();
        }

        private void Update()
        {
            _elapsed += Time.deltaTime;

            // Arrow lanes (and lines that stay after filling) wait for Hide() from their owner; the lifetime is only a safety net
            if (_isArrowLane || !_hideWhenFilled)
            {
                if (!_isArrowLane)
                    ApplyFill(Mathf.Clamp01(_elapsed / _duration));

                if (_elapsed >= _maxArrowLaneLifetime)
                    Hide();
                return;
            }

            ApplyFill(Mathf.Clamp01(_elapsed / _duration));

            if (_elapsed >= _duration + _lingerAfterFill)
                Hide();
        }

        // The line starts at the origin and extends along the direction; the fill sweeps from the origin outward
        public void ShowLine(Vector3 origin, Vector3 direction, float length, float width, float duration, bool hideWhenFilled = true)
        {
            _duration = Mathf.Max(duration, 0.01f);
            _isArrowLane = false;
            _hideWhenFilled = hideWhenFilled;
            Place(origin, direction, length, width);
            _propertyBlock.SetFloat(Pattern_Mode, 0f);
            ApplyFill(0f);
        }

        // A lane of scrolling arrows pointing along the direction that stays until Hide() is called
        public void ShowArrowLane(Vector3 origin, Vector3 direction, float length, float width)
        {
            _isArrowLane = true;
            Place(origin, direction, length, width);
            _propertyBlock.SetFloat(Pattern_Mode, 1f);
            ApplyFill(0f);
        }

        public void Hide() => Destroy(gameObject);

        private void Place(Vector3 origin, Vector3 direction, float length, float width)
        {
            _elapsed = 0f;

            direction.y = 0f;
            transform.SetPositionAndRotation(
                new Vector3(origin.x, origin.y + _groundOffset, origin.z),
                Quaternion.LookRotation(direction.normalized));
            transform.localScale = new Vector3(width, 1f, length);

            _meshRenderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetVector(Indicator_Size, new Vector4(width, length, 0f, 0f));
        }

        private void ApplyFill(float fill)
        {
            _propertyBlock.SetFloat(Fill_Amount, fill);
            _meshRenderer.SetPropertyBlock(_propertyBlock);
        }

        private static Mesh GetLineMesh()
        {
            if (_lineMesh != null)
                return _lineMesh;

            _lineMesh = new Mesh { name = "SkillIndicatorLine" };
            _lineMesh.vertices = new[]
            {
                new Vector3(-0.5f, 0f, 0f), new Vector3(0.5f, 0f, 0f),
                new Vector3(-0.5f, 0f, 1f), new Vector3(0.5f, 0f, 1f)
            };
            _lineMesh.uv = new[] { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f) };
            _lineMesh.normals = new[] { Vector3.up, Vector3.up, Vector3.up, Vector3.up };
            _lineMesh.triangles = new[] { 0, 2, 1, 1, 2, 3 };
            _lineMesh.RecalculateBounds();
            return _lineMesh;
        }
    }
}
