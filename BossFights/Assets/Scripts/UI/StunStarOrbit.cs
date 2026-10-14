using UnityEngine;

namespace UI
{
    // Moves the stars of StunStars round a flattened ring: the ones at the back are smaller and drawn behind, the ones in front bigger and drawn on top
    public class StunStarOrbit : MonoBehaviour
    {
        private const int BackSortingOrder = 99;
        private const int FrontSortingOrder = 101;
        private const float DepthScale = 0.25f;
        private const float SpinFactor = 2f;

        private SpriteRenderer[] _stars;
        private float[] _baseSizes;
        private float _radius;
        private float _flatten;
        private float _speed;
        private float _angle;

        private void LateUpdate()
        {
            _angle += _speed * Time.deltaTime;
            for (int i = 0; i < _stars.Length; i++)
            {
                float radians = (_angle + 360f * i / _stars.Length) * Mathf.Deg2Rad;
                float depth = Mathf.Sin(radians);
                Transform star = _stars[i].transform;
                star.localPosition = new Vector3(Mathf.Cos(radians) * _radius, depth * _radius * _flatten, 0f);
                star.localScale = Vector3.one * (_baseSizes[i] * (1f - DepthScale * depth));
                star.localRotation = Quaternion.Euler(0f, 0f, _angle * SpinFactor + 72f * i);
                _stars[i].sortingOrder = depth > 0f ? BackSortingOrder : FrontSortingOrder;
            }
        }

        public void Setup(float radius, float flatten, float speed)
        {
            _radius = radius;
            _flatten = flatten;
            _speed = speed;
            _stars = GetComponentsInChildren<SpriteRenderer>();
            _baseSizes = new float[_stars.Length];
            for (int i = 0; i < _stars.Length; i++)
                _baseSizes[i] = _stars[i].transform.localScale.x;
        }
    }
}
