using System.Collections;
using Shared;
using UnityEngine;

namespace Bosses.Ulvane
{
    // Trample: appears at the edge of the fight, a wide lane fills towards the player, then it gallops down the whole lane
    public class HorseBeast : UlvaneBeast
    {
        [Header("Trample")]
        [SerializeField] private float _flankDistance = 20f;
        [SerializeField] private float _laneWidth = 3.2f;
        [SerializeField] private float _maxLaneLength = 45f;
        [SerializeField] private float _telegraphTime = 1.2f;
        [SerializeField] private float _speed = 24f;
        [SerializeField] private float _hitPadding = 0.5f;
        [SerializeField] private float _recoverTime = 0.4f;
        [SerializeField] private int _damage = 2;

        public override int HitDamage => _damage;

        protected override Vector3 ChooseSpawnPosition() => owner.GetFlankPoint(_flankDistance);

        protected override IEnumerator Perform()
        {
            Vector3 start = transform.position;
            Vector3 direction = FlatDirectionTo(owner.PlayerAimPoint);
            float length = NavMeshUtility.DistanceToEdge(start, direction, _maxLaneLength);
            ShowLine(start, direction, length, _laneWidth, _telegraphTime);
            yield return FaceFor(direction, _telegraphTime);

            yield return DashAlongLine(direction, length, _speed, _laneWidth * 0.5f + _hitPadding, _damage);
            yield return new WaitForSeconds(_recoverTime);
        }
    }
}
