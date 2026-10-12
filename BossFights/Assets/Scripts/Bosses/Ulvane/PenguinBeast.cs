using System.Collections;
using Shared;
using UnityEngine;

namespace Bosses.Ulvane
{
    // Snowball Volley: several lanes fill in a fan towards the player, then a snowball flies down each one
    public class PenguinBeast : UlvaneBeast
    {
        [Header("Snowball Volley")]
        [SerializeField] private UlvaneProjectile _snowballPrefab;
        [SerializeField] private int _laneCount = 5;
        [SerializeField] private float _fanAngle = 50f;
        [SerializeField] private float _laneWidth = 0.8f;
        [SerializeField] private float _maxLaneLength = 30f;
        [SerializeField] private float _telegraphTime = 0.9f;
        [SerializeField] private float _snowballSpeed = 14f;
        [SerializeField] private float _snowballInterval = 0.07f;
        [SerializeField] private float _muzzleHeight = 0.8f;
        [SerializeField] private float _recoverTime = 0.5f;
        [SerializeField] private int _damage = 1;

        public override int HitDamage => _damage;

        protected override IEnumerator Perform()
        {
            Vector3 origin = transform.position;
            Vector3 baseDirection = FlatDirectionTo(owner.PlayerAimPoint);
            Vector3[] directions = new Vector3[_laneCount];
            float[] lengths = new float[_laneCount];
            for (int i = 0; i < _laneCount; i++)
            {
                float angle = _laneCount > 1 ? Mathf.Lerp(-_fanAngle * 0.5f, _fanAngle * 0.5f, i / (float)(_laneCount - 1)) : 0f;
                directions[i] = Quaternion.Euler(0f, angle, 0f) * baseDirection;
                lengths[i] = NavMeshUtility.DistanceToEdge(origin, directions[i], _maxLaneLength);
                ShowLine(origin, directions[i], lengths[i], _laneWidth, _telegraphTime);
            }

            yield return FaceFor(baseDirection, _telegraphTime);

            for (int i = 0; i < _laneCount; i++)
            {
                UlvaneProjectile snowball = Instantiate(_snowballPrefab, origin + Vector3.up * _muzzleHeight, Quaternion.identity);
                snowball.Launch(directions[i], _snowballSpeed, lengths[i] / _snowballSpeed + 0.2f, _damage);
                yield return new WaitForSeconds(_snowballInterval);
            }

            yield return new WaitForSeconds(_recoverTime);
        }
    }
}
