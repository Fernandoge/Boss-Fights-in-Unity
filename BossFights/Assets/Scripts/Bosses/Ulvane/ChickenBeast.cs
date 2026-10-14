using System.Collections;
using Shared;
using UnityEngine;

namespace Bosses.Ulvane
{
    // Egg Bombs: runs circles around the player and lays eggs that blow up a moment later, in a tight ring
    public class ChickenBeast : UlvaneBeast
    {
        [Header("Egg Bombs")]
        [SerializeField] private GameObject _eggPrefab;
        [SerializeField] private int _eggCount = 5;
        [SerializeField] private float _eggInterval = 0.45f;
        [SerializeField] private float _orbitRadius = 3.4f;
        [SerializeField] private float _approachSpeed = 11f;
        [SerializeField] private float _runSpeed = 9f;
        [SerializeField] private float _eggRadius = 1.6f;
        [SerializeField] private float _eggFuseTime = 1.3f;
        [SerializeField] private float _afterEggsTime = 1.2f;
        [SerializeField] private int _damage = 1;

        [Header("Sky Fall Combo")]
        [SerializeField] private int _rainEggCount = 16;
        [SerializeField] private float _rainInterval = 0.22f;
        [SerializeField] private float _rainFuseTime = 0.9f;
        [SerializeField] private float _rainEggRadius = 1.4f;
        [SerializeField] private float _rainScatter = 6f;
        [SerializeField] private float _rainAimedScatter = 1.2f;
        [SerializeField] private int _rainAimedEvery = 3;
        [SerializeField] private float _rainEggFallHeight = 25f;
        [SerializeField] private float _glideHeight = 30f;
        [SerializeField] private float _glideTime = 2.2f;
        [SerializeField] private float _glideSway = 20f;
        [SerializeField] private int _comboDamage = 2;

        public override int HitDamage => _damage;

        // Combo: from the sky the chicken drops eggs around the player, quickly one after another; every third one lands almost on where the player is heading, the rest are scattered
        public IEnumerator ComboEggRain()
        {
            for (int i = 0; i < _rainEggCount; i++)
            {
                bool isAimed = i % _rainAimedEvery == 0;
                Vector3 aim = owner.PlayerAimPoint + owner.PlayerAimVelocity * _rainFuseTime;
                Vector2 offset = Random.insideUnitCircle * (isAimed ? _rainAimedScatter : _rainScatter);
                Vector3 center = NavMeshUtility.Snap(aim + new Vector3(offset.x, 0f, offset.y));
                SpawnBlast(center, _rainEggRadius, _rainFuseTime, _comboDamage, _eggPrefab, _rainEggFallHeight);
                yield return new WaitForSeconds(_rainInterval);
            }

            yield return new WaitForSeconds(_rainFuseTime);
        }

        // Combo: the chicken flaps its way down from the sky; it hurts nobody, it is only going home
        public IEnumerator ComboGlideDown(Vector3 landing, Vector3 facing) =>
            ComboFallDown(landing, facing, _glideHeight, _glideTime, 1f, _glideSway, 0f);

        protected override IEnumerator Approach() => ApproachPlayer(_orbitRadius * 1.3f, _approachSpeed);

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
