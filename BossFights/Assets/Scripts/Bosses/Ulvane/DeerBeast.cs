using System.Collections;
using UnityEngine;

namespace Bosses.Ulvane
{
    // Antler Stomp: runs up to the player, then a ring fills around it and it stomps; twice
    public class DeerBeast : UlvaneBeast
    {
        [Header("Antler Stomp")]
        [SerializeField] private int _stompCount = 2;
        [SerializeField] private float _approachSpeed = 9f;
        [SerializeField] private float _approachDistance = 2.5f;
        [SerializeField] private float _approachTimeout = 2.5f;
        [SerializeField] private float _stompRadius = 3.4f;
        [SerializeField] private float _stompTelegraphTime = 0.8f;
        [SerializeField] private float _afterStompTime = 0.25f;
        [SerializeField] private int _damage = 1;

        public override int HitDamage => _damage;

        protected override IEnumerator Perform()
        {
            for (int i = 0; i < _stompCount; i++)
            {
                Gallop();
                for (float elapsed = 0f; elapsed < _approachTimeout && !IsPlayerNear(_approachDistance); elapsed += Time.deltaTime)
                {
                    Vector3 direction = FlatDirectionTo(owner.PlayerAimPoint);
                    FaceDirection(direction, Time.deltaTime);
                    MoveFlat(direction * (_approachSpeed * Time.deltaTime));
                    yield return null;
                }

                Idle();
                SpawnBlast(transform.position, _stompRadius, _stompTelegraphTime, _damage);
                yield return FaceFor(FlatDirectionTo(owner.PlayerAimPoint), _stompTelegraphTime + _afterStompTime);
            }
        }
    }
}
