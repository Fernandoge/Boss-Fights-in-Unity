using System.Collections;
using Shared;
using UnityEngine;

namespace Bosses.Ulvane
{
    // Flurry: three quick dashes at the player, each with a short warning lane
    public class KittyBeast : UlvaneBeast
    {
        [Header("Flurry")]
        [SerializeField] private int _dashCount = 3;
        [SerializeField] private float _flankDistance = 12f;
        [SerializeField] private float _telegraphTime = 0.5f;
        [SerializeField] private float _dashSpeed = 22f;
        [SerializeField] private float _laneWidth = 1.2f;
        [SerializeField] private float _overshoot = 3f;
        [SerializeField] private float _hitPadding = 0.5f;
        [SerializeField] private float _pauseTime = 0.3f;
        [SerializeField] private int _damage = 1;

        public override int HitDamage => _damage;

        protected override Vector3 ChooseSpawnPosition() => owner.GetFlankPoint(_flankDistance);

        protected override IEnumerator Perform()
        {
            for (int i = 0; i < _dashCount; i++)
            {
                Vector3 start = transform.position;
                Vector3 aimPoint = owner.PlayerAimPoint;
                Vector3 direction = FlatDirectionTo(aimPoint);
                Vector3 flatToAim = aimPoint - start;
                flatToAim.y = 0f;
                float wanted = flatToAim.magnitude + _overshoot;
                float length = Mathf.Min(wanted, NavMeshUtility.DistanceToEdge(start, direction, wanted));

                ShowLine(start, direction, length, _laneWidth, _telegraphTime);
                yield return FaceFor(direction, _telegraphTime);
                yield return DashAlongLine(direction, length, _dashSpeed, _laneWidth * 0.5f + _hitPadding, _damage);
                yield return new WaitForSeconds(_pauseTime);
            }
        }
    }
}
