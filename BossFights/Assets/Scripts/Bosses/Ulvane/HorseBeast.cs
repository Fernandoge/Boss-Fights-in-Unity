using System.Collections;
using Shared;
using UnityEngine;

namespace Bosses.Ulvane
{
    // Trample: charges straight from where it patrols: a wide lane fills towards the player and it gallops down the whole lane, then lines up on the player again from where it stopped for a second, quicker charge
    public class HorseBeast : UlvaneBeast
    {
        [Header("Trample")]
        [SerializeField] private float _laneWidth = 3.2f;
        [SerializeField] private float _maxLaneLength = 45f;
        [SerializeField] private float _telegraphTime = 1.2f;
        [SerializeField] private float _secondTelegraphTime = 0.9f;
        [SerializeField] private float _speed = 24f;
        [SerializeField] private float _hitPadding = 0.5f;
        [SerializeField] private float _recoverTime = 0.4f;
        [SerializeField] private int _damage = 1;

        public override int HitDamage => _damage;

        protected override IEnumerator Perform()
        {
            yield return Charge(_telegraphTime);
            yield return Charge(_secondTelegraphTime);
            yield return new WaitForSeconds(_recoverTime);
        }

        private IEnumerator Charge(float telegraphTime)
        {
            Vector3 start = transform.position;
            Vector3 direction = FlatDirectionTo(owner.PlayerAimPoint);
            float length = NavMeshUtility.DistanceToEdge(start, direction, _maxLaneLength);
            ShowLine(start, direction, length, _laneWidth, telegraphTime);
            yield return FaceFor(direction, telegraphTime);

            yield return DashAlongLine(direction, length, _speed, _laneWidth * 0.5f + _hitPadding, _damage);
        }
    }
}
