using System.Collections;
using UnityEngine;

namespace Bosses.Ulvane
{
    // Hunt: after a short warning it chases the player for a few seconds, turning slowly, and bites whoever it catches
    public class DogBeast : UlvaneBeast
    {
        [Header("Hunt")]
        [SerializeField] private float _windUpTime = 0.7f;
        [SerializeField] private float _chaseTime = 4.5f;
        [SerializeField] private float _speed = 6.5f;
        [SerializeField] private float _turnRate = 140f;
        [SerializeField] private float _biteRadius = 1.2f;
        [SerializeField] private float _biteCooldown = 0.9f;
        [SerializeField] private float _warningRadius = 1.6f;
        [SerializeField] private int _biteDamage = 1;

        public override int HitDamage => _biteDamage;

        protected override IEnumerator Perform()
        {
            ShowCircle(transform.position, _warningRadius, _windUpTime);
            yield return FaceFor(FlatDirectionTo(owner.PlayerAimPoint), _windUpTime);

            Gallop();
            float biteTimer = 0f;
            for (float elapsed = 0f; elapsed < _chaseTime; elapsed += Time.deltaTime)
            {
                TurnTowards(_turnRate, FlatDirectionTo(owner.PlayerAimPoint), Time.deltaTime);
                MoveFlat(transform.forward * (_speed * Time.deltaTime));

                biteTimer -= Time.deltaTime;
                if (biteTimer <= 0f && IsPlayerNear(_biteRadius))
                {
                    DamagePlayer(_biteDamage);
                    biteTimer = _biteCooldown;
                }

                yield return null;
            }

            Idle();
        }
    }
}
