using System;
using System.Collections;
using Shared;
using UI;
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
        [SerializeField] private float _snowballSpeed = 21.84f;
        [SerializeField] private float _snowballInterval = 0.07f;
        [SerializeField] private float _muzzleHeight = 0.8f;
        [SerializeField] private float _recoverTime = 0.5f;
        [SerializeField] private int _damage = 1;

        [Header("Snow Tornado Combo")]
        [SerializeField] private float _feedSpeed = 16f;
        [SerializeField] private float _feedAimTime = 0.25f;
        [SerializeField] private Vector3 _feedTargetOffset = new Vector3(0f, 1.2f, 0f);

        [Header("Hairball Combo")]
        [SerializeField] private float _wonderTime = 1.4f;
        [SerializeField] private float _takeBallTime = 0.4f;
        [SerializeField] private Vector3 _holdOffset = new Vector3(0f, 1.8f, 0.3f);
        [SerializeField] private float _throwWindUpTime = 0.6f;
        [SerializeField] private float _hairballLeadTime = 0.5f;
        [SerializeField] private float _pathMaxLength = 80f;
        [SerializeField] private float _firstBurstDistance = 5f;
        [SerializeField] private float _burstSpacing = 6f;
        [SerializeField] private int _minBurstCount = 5;
        [SerializeField] private float _starLeadTime = 1f;
        [SerializeField] private float _bigSpeed = 8f;
        [SerializeField] private float _bigHitRadius = 0.7f;
        [SerializeField] private float _bigLaneWidth = 1.2f;
        [SerializeField] private float _flightHeight = 1f;
        [SerializeField] private int _smallCount = 8;
        [SerializeField] private float _smallScale = 0.9f;
        [SerializeField] private float _smallSpeed = 17.5f;
        [SerializeField] private float _smallHitRadius = 0.4f;
        [SerializeField] private float _smallLaneWidth = 0.8f;
        [SerializeField] private float _smallRange = 80f;
        [SerializeField] private float _groundHalfSize = 45f;
        [SerializeField] private float _lanesLingerTime = 0.25f;
        [SerializeField] private float _throwRecoverTime = 0.8f;
        [SerializeField] private int _comboDamage = 2;

        public override int HitDamage => _damage;

        // Combo: a hairball lies on the ice. The penguin does not understand: a question mark shows (and the warning sign goes away for it), it looks at the ball
        public IEnumerator ComboWonder(Transform ball)
        {
            HideDanger();
            TargetMarker questionMark = QuestionMark.Show(GetHeightTarget());
            yield return FaceFor(FlatDirectionTo(ball.position), _wonderTime);
            if (questionMark)
                questionMark.Hide();

            ShowDanger();
        }

        // Combo: picks the hairball up over its head
        public IEnumerator ComboTakeBall(UlvaneProjectile ball)
        {
            ball.transform.SetParent(transform, true);
            Vector3 start = ball.transform.localPosition;
            for (float elapsed = 0f; elapsed < _takeBallTime; elapsed += Time.deltaTime)
            {
                ball.transform.localPosition = Vector3.Lerp(start, _holdOffset, Mathf.SmoothStep(0f, 1f, elapsed / _takeBallTime));
                yield return null;
            }

            ball.transform.localPosition = _holdOffset;
        }

        // Combo: throws the big slow hairball towards the player. It does not stop until it reaches the trees at the end of the floor, and on the way it bursts again and again (five times at least) into small hairballs flying out in a star. Every lane is shown before it fills, each star a moment before its burst
        public IEnumerator ComboHairballThrow(UlvaneProjectile ball, UlvaneProjectile smallPrefab)
        {
            Vector3 origin = transform.position;
            Vector3 target = owner.PlayerAimPoint + owner.PlayerAimVelocity * _hairballLeadTime;
            Vector3 direction = FlatDirectionTo(target);
            float pathLength = GetFlightLength(origin, direction, _pathMaxLength);

            // Bursts every few metres along the path; a short path packs them closer so there are always enough
            float firstBurst = Mathf.Min(_firstBurstDistance, pathLength * 0.4f);
            float lastBurst = Mathf.Max(firstBurst, pathLength - 1f);
            int burstCount = Mathf.Max(_minBurstCount, Mathf.FloorToInt((lastBurst - firstBurst) / _burstSpacing) + 1);
            float spacing = burstCount > 1 ? (lastBurst - firstBurst) / (burstCount - 1) : 0f;

            ShowLine(origin, direction, pathLength, _bigLaneWidth, _throwWindUpTime + pathLength / _bigSpeed);
            float lastBurstTime = 0f;
            for (int i = 0; i < burstCount; i++)
            {
                float distance = firstBurst + spacing * i;
                float burstTime = _throwWindUpTime + distance / _bigSpeed;
                float lanesStart = Mathf.Max(0f, burstTime - _starLeadTime);
                Vector3 burstPoint = origin + direction * distance;

                // Every other star is turned half a step, so the lanes do not line up from one burst to the next
                StartCoroutine(StarBurst(burstPoint, direction, i % 2 == 0, lanesStart, burstTime, smallPrefab));
                lastBurstTime = burstTime;
            }

            yield return FaceFor(direction, _throwWindUpTime);

            ball.transform.SetParent(null, true);
            ball.transform.position = new Vector3(origin.x, GroundY + _flightHeight, origin.z);
            ball.SetHitRadius(_bigHitRadius);
            ball.SetPiercing(true);
            ball.SetFliesToScenery(true);
            ball.Launch(direction, _bigSpeed, pathLength / _bigSpeed + 0.5f, _comboDamage);

            yield return new WaitForSeconds(lastBurstTime - _throwWindUpTime + _lanesLingerTime);
            if (ball)
                Destroy(ball.gameObject);

            yield return new WaitForSeconds(_throwRecoverTime);
        }

        // How far a hairball flies from a point: to the first tree or rock, or to the edge of the ground where the trees leave a gap
        private float GetFlightLength(Vector3 from, Vector3 direction, float maxLength)
        {
            direction.y = 0f;
            direction.Normalize();
            float length = Mathf.Min(maxLength, NavMeshUtility.DistanceToScenery(from, direction, _flightHeight, maxLength));
            Vector3 center = owner.ArenaCenter;
            if (Mathf.Abs(direction.x) > 0.0001f)
                length = Mathf.Min(length, ((direction.x > 0f ? center.x + _groundHalfSize : center.x - _groundHalfSize) - from.x) / direction.x);

            if (Mathf.Abs(direction.z) > 0.0001f)
                length = Mathf.Min(length, ((direction.z > 0f ? center.z + _groundHalfSize : center.z - _groundHalfSize) - from.z) / direction.z);

            return Mathf.Max(length, 0f);
        }

        // One star: the lanes appear and fill until the big hairball gets there, then it bursts into small hairballs that fly down them
        private IEnumerator StarBurst(Vector3 point, Vector3 throwDirection, bool isHalfStep, float lanesStart, float burstTime, UlvaneProjectile smallPrefab)
        {
            yield return new WaitForSeconds(lanesStart);

            Vector3[] directions = new Vector3[_smallCount];
            float[] lengths = new float[_smallCount];
            SkillIndicator[] lanes = new SkillIndicator[_smallCount];
            for (int i = 0; i < _smallCount; i++)
            {
                float angle = 360f / _smallCount * (i + (isHalfStep ? 0.5f : 0f));
                directions[i] = Quaternion.Euler(0f, angle, 0f) * throwDirection;
                lengths[i] = GetFlightLength(point, directions[i], _smallRange);
                lanes[i] = Instantiate(owner.IndicatorPrefab);
                lanes[i].ShowLine(point, directions[i], lengths[i], _smallLaneWidth, burstTime - lanesStart, false);
            }

            yield return new WaitForSeconds(burstTime - lanesStart);

            if (owner.ImpactEffectPrefab)
                Destroy(Instantiate(owner.ImpactEffectPrefab, point + Vector3.up * _flightHeight, Quaternion.identity), 3f);

            for (int i = 0; i < _smallCount; i++)
            {
                UlvaneProjectile small = Instantiate(smallPrefab, new Vector3(point.x, GroundY + _flightHeight, point.z), Quaternion.identity);
                small.transform.localScale = Vector3.one * _smallScale;
                small.SetHitRadius(_smallHitRadius);
                small.SetFliesToScenery(true);
                small.Launch(directions[i], _smallSpeed, lengths[i] / _smallSpeed + 0.2f, _comboDamage);
            }

            yield return new WaitForSeconds(_lanesLingerTime);
            foreach (SkillIndicator lane in lanes)
                if (lane)
                    lane.Hide();
        }

        // Combo: turns to a partner and lobs one harmless snowball at it; onArrive gets the ball when it lands
        public IEnumerator ComboFeed(Transform partner, Action<UlvaneProjectile> onArrive)
        {
            yield return FaceFor(FlatDirectionTo(partner.position), _feedAimTime);
            UlvaneProjectile snowball = Instantiate(_snowballPrefab, transform.position + Vector3.up * _muzzleHeight, Quaternion.identity);
            snowball.LaunchAtTarget(partner, _feedTargetOffset, _feedSpeed, onArrive);
        }

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
