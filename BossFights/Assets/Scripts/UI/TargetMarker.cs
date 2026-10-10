using TMPro;
using UnityEngine;

namespace UI
{
    // A small sign that floats above a collider, faces the camera, bobs and pulses, and pops in and out; CounterPrompt and DangerIcon build their shapes onto one. It is made in code, so nothing needs to be wired in a scene
    public class TargetMarker : MonoBehaviour
    {
        private const float PopTime = 0.12f;

        private Collider _target;
        private float _heightAboveTarget;
        private float _bobHeight;
        private float _bobSpeed;
        private float _pulseAmount;
        private float _pulseSpeedFactor;
        private float _elapsed;
        private float _hideElapsed;
        private bool _isHiding;

        private void LateUpdate()
        {
            if (!_target || !_target.gameObject.activeInHierarchy)
            {
                Destroy(gameObject);
                return;
            }

            _elapsed += Time.deltaTime;
            float popScale = Mathf.Clamp01(_elapsed / PopTime);
            if (_isHiding)
            {
                _hideElapsed += Time.deltaTime;
                popScale = Mathf.Min(popScale, 1f - _hideElapsed / PopTime);
                if (popScale <= 0f)
                {
                    Destroy(gameObject);
                    return;
                }
            }

            Bounds bounds = _target.bounds;
            Vector3 position = new Vector3(bounds.center.x, bounds.max.y + _heightAboveTarget, bounds.center.z);
            position.y += Mathf.Sin(_elapsed * _bobSpeed) * _bobHeight;
            transform.position = position;

            Camera mainCamera = Camera.main;
            if (mainCamera)
                transform.rotation = mainCamera.transform.rotation;

            float pulse = 1f + Mathf.Sin(_elapsed * _bobSpeed * _pulseSpeedFactor) * _pulseAmount;
            transform.localScale = Vector3.one * (pulse * popScale);
        }

        public void Hide() => _isHiding = true;

        // target is the collider of whatever the sign is about; the sign sits above its bounds and follows it
        public static TargetMarker Create(string markerName, Collider target, float heightAboveTarget, float bobHeight, float bobSpeed, float pulseAmount, float pulseSpeedFactor)
        {
            GameObject markerObject = new GameObject(markerName);
            TargetMarker marker = markerObject.AddComponent<TargetMarker>();
            marker._target = target;
            marker._heightAboveTarget = heightAboveTarget;
            marker._bobHeight = bobHeight;
            marker._bobSpeed = bobSpeed;
            marker._pulseAmount = pulseAmount;
            marker._pulseSpeedFactor = pulseSpeedFactor;
            markerObject.transform.localScale = Vector3.zero;
            return marker;
        }

        public void AddShape(string shapeName, Sprite sprite, Color color, float size, int sortingOrder, Vector3 localPosition = default)
        {
            GameObject shapeObject = new GameObject(shapeName);
            shapeObject.transform.SetParent(transform, false);
            shapeObject.transform.localPosition = localPosition;
            shapeObject.transform.localScale = Vector3.one * size;

            SpriteRenderer shape = shapeObject.AddComponent<SpriteRenderer>();
            shape.sprite = sprite;
            shape.color = color;
            shape.sortingOrder = sortingOrder;
        }

        public void AddLetter(string content, float fontSize, Color color, int sortingOrder, Vector3 localPosition = default)
        {
            GameObject letterObject = new GameObject("Letter");
            letterObject.transform.SetParent(transform, false);
            letterObject.transform.localPosition = localPosition;

            TextMeshPro letter = letterObject.AddComponent<TextMeshPro>();
            letter.text = content;
            letter.fontSize = fontSize;
            letter.fontStyle = FontStyles.Bold;
            letter.alignment = TextAlignmentOptions.Center;
            letter.textWrappingMode = TextWrappingModes.NoWrap;
            letter.color = color;
            letter.sortingOrder = sortingOrder;
        }
    }
}
