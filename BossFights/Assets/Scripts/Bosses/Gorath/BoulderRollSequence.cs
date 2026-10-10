using Shared;
using UnityEngine;

namespace Bosses.Gorath
{
    public class BoulderRollSequence : MonoBehaviour
    {
        private BoulderLane[] _lanes;
        private SkillIndicator[] _indicators;
        private bool[] _isLaunched;
        private SkillIndicator _indicatorPrefab;
        private BoulderProjectile _boulderPrefab;
        private float _radius;
        private float _speed;
        private int _damage;
        private float _arrowInterval;
        private float _elapsed;
        private int _launchedCount;

        private void Update()
        {
            _elapsed += Time.deltaTime;

            for (int i = 0; i < _lanes.Length; i++)
            {
                if (_indicators[i] == null && !_isLaunched[i] && _elapsed >= i * _arrowInterval)
                    ShowArrows(i);

                if (!_isLaunched[i] && _elapsed >= _lanes[i].LaunchTime)
                    Launch(i);
            }

            if (_launchedCount >= _lanes.Length)
                Destroy(gameObject);
        }

        public void Begin(BoulderLane[] lanes, SkillIndicator indicatorPrefab, BoulderProjectile boulderPrefab, float radius,
            float speed, int damage, float arrowInterval)
        {
            _lanes = lanes;
            _indicatorPrefab = indicatorPrefab;
            _boulderPrefab = boulderPrefab;
            _radius = radius;
            _speed = speed;
            _damage = damage;
            _arrowInterval = arrowInterval;
            _indicators = new SkillIndicator[lanes.Length];
            _isLaunched = new bool[lanes.Length];
        }

        // The arrows start at the arena edge, where the boulder becomes visible, and are as wide as the boulder
        private void ShowArrows(int index)
        {
            BoulderLane lane = _lanes[index];
            Vector3 edge = lane.Start + lane.Direction * _radius;
            edge.y = lane.Start.y - _radius;

            _indicators[index] = Instantiate(_indicatorPrefab);
            _indicators[index].ShowArrowLane(edge, lane.Direction, lane.MaxDistance - _radius * 2f, _radius * 2f);
        }

        private void Launch(int index)
        {
            _isLaunched[index] = true;
            _launchedCount++;

            if (_indicators[index])
                _indicators[index].Hide();

            BoulderLane lane = _lanes[index];
            BoulderProjectile boulder = Instantiate(_boulderPrefab, lane.Start, Quaternion.identity);
            boulder.Launch(lane.Direction, _speed, _radius, _damage, _radius * 2f + 2f, lane.MaxDistance);
        }
    }
}
