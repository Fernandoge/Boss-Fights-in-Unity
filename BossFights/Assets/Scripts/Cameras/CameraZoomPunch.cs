using UnityEngine;

namespace Cameras
{
    // A quick zoom-in on the camera that eases back out, on real time, for hits that need weight; it only touches the camera size while a punch is running
    public class CameraZoomPunch : MonoBehaviour
    {
        private Camera _camera;
        private float _baseValue;
        private float _startZoom;
        private float _currentZoom = 1f;
        private float _factor;
        private float _inTime;
        private float _holdTime;
        private float _outTime;
        private float _timer;
        private bool _isActive;

        /// *** Unity Events *** ///

        private void LateUpdate()
        {
            if (!_isActive)
                return;

            _timer += Time.unscaledDeltaTime;
            float zoom;
            if (_timer < _inTime)
                zoom = Mathf.SmoothStep(_startZoom, _factor, _timer / _inTime);
            else if (_timer < _inTime + _holdTime)
                zoom = _factor;
            else
                zoom = Mathf.SmoothStep(_factor, 1f, (_timer - _inTime - _holdTime) / _outTime);

            if (_timer >= _inTime + _holdTime + _outTime)
            {
                zoom = 1f;
                _isActive = false;
            }

            _currentZoom = zoom;
            Apply(zoom);
        }

        /// *** Public Methods *** ///

        // A punch that starts while another one is running continues from the zoom the camera has now, so chained punches never jump
        public void Punch(float factor, float inTime, float holdTime, float outTime)
        {
            if (!_camera)
                _camera = GetComponent<Camera>();

            if (!_isActive)
            {
                _baseValue = _camera.orthographic ? _camera.orthographicSize : _camera.fieldOfView;
                _currentZoom = 1f;
            }

            _startZoom = _currentZoom;
            _factor = factor;
            _inTime = Mathf.Max(0.001f, inTime);
            _holdTime = holdTime;
            _outTime = Mathf.Max(0.001f, outTime);
            _timer = 0f;
            _isActive = true;
        }

        /// *** Private Methods *** ///

        private void Apply(float zoom)
        {
            if (_camera.orthographic)
                _camera.orthographicSize = _baseValue / zoom;
            else
                _camera.fieldOfView = _baseValue / zoom;
        }
    }
}
