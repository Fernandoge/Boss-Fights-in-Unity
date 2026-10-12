using System.Collections;
using Shared;
using UnityEngine;

namespace Bosses.Ulvane
{
    // Egg Bombs: runs circles around the player and lays eggs that blow up a moment later
    public class ChickenBeast : UlvaneBeast
    {
        [Header("Egg Bombs")]
        [SerializeField] private GameObject _eggPrefab;
        [SerializeField] private int _eggCount = 5;
        [SerializeField] private float _eggInterval = 0.45f;
        [SerializeField] private float _orbitRadius = 5f;
        [SerializeField] private float _runSpeed = 9f;
        [SerializeField] private float _eggRadius = 1.5f;
        [SerializeField] private float _eggFuseTime = 1.3f;
        [SerializeField] private float _afterEggsTime = 1.2f;
        [SerializeField] private int _damage = 1;

        public override int HitDamage => _damage;

        protected override IEnumerator Perform()
        {
            Vector3 fromCenter = transform.position - owner.PlayerAimPoint;
            fromCenter.y = 0f;
            float angle = Mathf.Atan2(fromCenter.z, fromCenter.x);
            float angularSpeed = _runSpeed / _orbitRadius;
            float eggTimer = 0f;
            int eggsLaid = 0;

            Gallop();
            while (eggsLaid < _eggCount)
            {
                angle += angularSpeed * Time.deltaTime;
                Vector3 center = owner.PlayerAimPoint;
                Vector3 desired = center + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * _orbitRadius;
                Vector3 direction = FlatDirectionTo(desired);
                FaceDirection(direction, Time.deltaTime);
                MoveFlat(direction * (_runSpeed * 1.2f * Time.deltaTime));

                eggTimer -= Time.deltaTime;
                if (eggTimer <= 0f)
                {
                    SpawnBlast(NavMeshUtility.Snap(transform.position), _eggRadius, _eggFuseTime, _damage, _eggPrefab);
                    eggsLaid++;
                    eggTimer = _eggInterval;
                }

                yield return null;
            }

            Idle();
            yield return new WaitForSeconds(_afterEggsTime);
        }
    }
}
