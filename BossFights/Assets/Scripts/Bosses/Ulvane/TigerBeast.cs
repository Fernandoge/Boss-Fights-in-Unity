using System.Collections;
using Manager.GameManager;
using Shared;
using UnityEngine;
using UnityEngine.AI;

namespace Bosses.Ulvane
{
    // Pounce: crouches while a circle fills where the player is heading, then leaps and lands on it
    public class TigerBeast : UlvaneBeast
    {
        [Header("Pounce")]
        [SerializeField] private float _approachRange = 18.2f;
        [SerializeField] private float _approachSpeed = 24f;
        [SerializeField] private float _telegraphTime = 0.375f;
        [SerializeField] private float _leapTime = 0.425f;
        [SerializeField] private float _leapHeight = 3f;
        [SerializeField] private float _blastRadius = 2.2f;
        [SerializeField] private float _leadTime = 0.8f;
        [SerializeField] private float _recoverTime = 0.6f;
        [SerializeField] private int _damage = 1;

        [Header("Sky Fall Combo")]
        [SerializeField] private float _slamRadius = 14f;
        [SerializeField] private float _slamSafeAngle = 60f;
        [SerializeField] private float _slamLandDistance = 4.5f;
        [SerializeField] private float _slamTelegraphTime = 1.2f;
        [SerializeField] private float _slamWedgeCheckNear = 3f;
        [SerializeField] private float _slamWedgeCheckFar = 6f;
        [SerializeField] private float _slamFallTime = 0.12f;
        [SerializeField] private float _slamFallHeight = 40f;
        [SerializeField] private float _slamHitPadding = 0.2f;
        [SerializeField] private float _slamRecoverTime = 0.8f;
        [SerializeField] private float _slamSplashTime = 0.6f;
        [SerializeField] private Color _slamSplashColor = new Color(1f, 0.55f, 0.15f, 0.8f);
        [SerializeField] private int _comboDamage = 2;

        private NavMeshPath _wedgePath;

        public override int HitDamage => _damage;

        // Combo: the tiger drops from the sky a few metres from the player, facing them, and the whole circle around it hurts except a wedge behind it. The circle is shown from the start and fills; when it is full it disappears and the tiger drops
        public IEnumerator ComboSlam()
        {
            Vector3 playerPoint = owner.PlayerAimPoint;
            Vector3 landing = FindLandingPoint(playerPoint);
            Vector3 facing = playerPoint - landing;
            facing.y = 0f;
            facing = facing.sqrMagnitude > 0.0001f ? facing.normalized : Vector3.forward;
            Vector3 safeDirection = -facing;

            SkillIndicator indicator = Instantiate(owner.IndicatorPrefab);
            indicator.ShowCircleWithSafeWedge(landing, safeDirection, _slamRadius, _slamSafeAngle, _slamTelegraphTime, false);

            float startTime = Time.time;
            while (Time.time - startTime < _slamTelegraphTime)
                yield return null;

            // The circle is gone before the tiger drops
            indicator.Hide();
            yield return ComboFallDown(landing, facing, _slamFallHeight, _slamFallTime, 2f, 0f, 0f);

            Impact(landing, safeDirection);
            yield return new WaitForSeconds(_slamRecoverTime);
        }

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

        // A point the set distance from the player on the walkable floor, turning round the player until the safe wedge behind it can be walked to
        private Vector3 FindLandingPoint(Vector3 playerPoint)
        {
            float startAngle = Random.Range(0f, 360f);
            Vector3 fallback = NavMeshUtility.Snap(playerPoint);
            bool hasFallback = false;
            for (int i = 0; i < 16; i++)
            {
                Vector3 direction = Quaternion.Euler(0f, startAngle + 22.5f * i, 0f) * Vector3.forward;
                Vector3 candidate = playerPoint + direction * _slamLandDistance;
                Vector3 snapped = NavMeshUtility.Snap(candidate, 1f);
                if ((snapped - candidate).sqrMagnitude >= 1f)
                    continue;

                if (!hasFallback)
                {
                    fallback = snapped;
                    hasFallback = true;
                }

                if (IsSafeWedgeReachable(snapped, direction, playerPoint))
                    return snapped;
            }

            return fallback;
        }

        // The wedge is behind the tiger, away from the player: its middle and its sides must be on the floor (not in the trees) and the player must be able to walk there
        private bool IsSafeWedgeReachable(Vector3 landing, Vector3 safeDirection, Vector3 playerPoint)
        {
            if (!IsWalkable(landing + safeDirection * _slamWedgeCheckNear) || !IsWalkable(landing + safeDirection * _slamWedgeCheckFar))
                return false;

            float sideAngle = _slamSafeAngle * 0.4f;
            Vector3 left = landing + Quaternion.Euler(0f, -sideAngle, 0f) * safeDirection * _slamWedgeCheckFar;
            Vector3 right = landing + Quaternion.Euler(0f, sideAngle, 0f) * safeDirection * _slamWedgeCheckFar;
            if (!IsWalkable(left) || !IsWalkable(right))
                return false;

            if (_wedgePath == null)
                _wedgePath = new NavMeshPath();

            Vector3 target = landing + safeDirection * (_slamWedgeCheckNear + _slamWedgeCheckFar) * 0.5f;
            return NavMesh.CalculatePath(NavMeshUtility.Snap(playerPoint), target, NavMesh.AllAreas, _wedgePath) && _wedgePath.status == NavMeshPathStatus.PathComplete;
        }

        private static bool IsWalkable(Vector3 point) => NavMesh.SamplePosition(point, out NavMeshHit _, 0.6f, NavMesh.AllAreas);

        private void Impact(Vector3 landing, Vector3 safeDirection)
        {
            if (owner.ImpactEffectPrefab)
                Destroy(Instantiate(owner.ImpactEffectPrefab, landing, Quaternion.identity), 3f);

            GroundSplash.Show(new Vector3(landing.x, GroundY, landing.z), _slamRadius, _slamSplashColor, _slamSplashTime);

            Vector3 toPlayer = GameManager.Instance.player.transform.position - landing;
            toPlayer.y = 0f;
            float reach = _slamRadius + _slamHitPadding;
            if (toPlayer.sqrMagnitude > reach * reach)
                return;

            // The wedge is safe unless the player stands right at the tiger's feet
            if (toPlayer.sqrMagnitude < 0.01f || Vector3.Angle(safeDirection, toPlayer) > _slamSafeAngle * 0.5f)
                DamagePlayer(_comboDamage);
        }
    }
}
