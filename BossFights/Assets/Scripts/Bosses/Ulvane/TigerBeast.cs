using System.Collections;
using Shared;
using UnityEngine;

namespace Bosses.Ulvane
{
    // Pounce: crouches while a circle fills where the player is heading, then leaps and lands on it
    public class TigerBeast : UlvaneBeast
    {
        [Header("Pounce")]
        [SerializeField] private float _approachRange = 18.2f;
        [SerializeField] private float _approachSpeed = 12f;
        [SerializeField] private float _telegraphTime = 0.9f;
        [SerializeField] private float _leapTime = 0.85f;
        [SerializeField] private float _leapHeight = 3f;
        [SerializeField] private float _blastRadius = 2.2f;
        [SerializeField] private float _leadTime = 0.5f;
        [SerializeField] private float _recoverTime = 0.6f;
        [SerializeField] private int _damage = 1;

        public override int HitDamage => _damage;

        protected override IEnumerator Approach() => ApproachPlayer(_approachRange, _approachSpeed);

        protected override IEnumerator Perform()
        {
            Vector3 target = NavMeshUtility.Snap(owner.PlayerAimPoint + owner.PlayerAimVelocity * _leadTime);
            SpawnBlast(target, _blastRadius, _telegraphTime + _leapTime, _damage);
            yield return FaceFor(FlatDirectionTo(target), _telegraphTime);

            Vector3 start = transform.position;
            Gallop();
            for (float elapsed = 0f; elapsed < _leapTime; elapsed += Time.deltaTime)
            {
                float progress = elapsed / _leapTime;
                Vector3 position = Vector3.Lerp(start, target, progress);
                position.y = start.y + Mathf.Sin(progress * Mathf.PI) * _leapHeight;
                transform.position = position;
                FaceDirection(target - start, Time.deltaTime);
                yield return null;
            }

            transform.position = new Vector3(target.x, start.y, target.z);
            Idle();
            yield return new WaitForSeconds(_recoverTime);
        }
    }
}
