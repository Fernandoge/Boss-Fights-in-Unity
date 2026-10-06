using UnityEngine;

namespace Cameras
{
    // Camera for single-screen arenas: shows a fixed 16:9 view of the arena and adds black bars when the window is not 16:9
    [ExecuteAlways]
    [RequireComponent(typeof(Camera))]
    public class FixedArenaCamera : MonoBehaviour
    {
        [SerializeField] private float _visibleWidth = 40f;
        [SerializeField] private Color _barColor = Color.black;

        private Camera _camera;
        private Camera _barsCamera;

        private const float TargetAspect = 16f / 9f;

        private void OnEnable()
        {
            _camera = GetComponent<Camera>();
            ApplySize();

            if (Application.isPlaying)
                CreateBarsCamera();
        }

        private void OnDisable()
        {
            if (_barsCamera)
                Destroy(_barsCamera.gameObject);
        }

        private void OnValidate()
        {
            _camera = GetComponent<Camera>();
            ApplySize();
        }

        private void Update()
        {
            if (Application.isPlaying)
                ApplyLetterbox();
        }

        // The view is as wide as the arena, so the world width shown never depends on the window size
        private void ApplySize()
        {
            _camera.orthographic = true;
            _camera.orthographicSize = _visibleWidth / TargetAspect * 0.5f;
        }

        private void CreateBarsCamera()
        {
            GameObject barsObject = new GameObject("Letterbox Bars");
            barsObject.transform.SetParent(transform, false);
            _barsCamera = barsObject.AddComponent<Camera>();
            _barsCamera.clearFlags = CameraClearFlags.SolidColor;
            _barsCamera.backgroundColor = _barColor;
            _barsCamera.cullingMask = 0;
            _barsCamera.depth = _camera.depth - 1f;
        }

        private void ApplyLetterbox()
        {
            float windowAspect = (float)Screen.width / Screen.height;
            if (windowAspect >= TargetAspect)
            {
                float width = TargetAspect / windowAspect;
                _camera.rect = new Rect((1f - width) * 0.5f, 0f, width, 1f);
            }
            else
            {
                float height = windowAspect / TargetAspect;
                _camera.rect = new Rect(0f, (1f - height) * 0.5f, 1f, height);
            }
        }
    }
}
