using UnityEngine;

namespace Characters
{
    // Reaches the right arm of a humanoid out to a target and strokes it back and forth, on top of whatever pose the animation plays (a two-bone solve in LateUpdate). Runs on real time, like PosePlayer; added in code by whoever needs it
    public class PetStroker : MonoBehaviour
    {
        private const float ReachBlendTime = 0.35f;

        private Transform _upperArm;
        private Transform _foreArm;
        private Transform _hand;
        private Transform _target;
        private Transform[] _fingers;
        private Transform _spine;
        private Transform _chest;
        private Transform _indexBase;
        private Transform _middleBase;
        private Transform _littleBase;
        private float _leanAngle;
        private float _height;
        private float _strokeLength;
        private float _strokeSpeed;
        private float _startDelay;
        private float _elapsed;
        private float _strokeTime;
        private float _weight;
        private bool _isEnding;

        // True once the arm has blended all the way to the animal
        public bool HasReached => _weight >= 0.95f;

        private void LateUpdate()
        {
            _elapsed += Time.unscaledDeltaTime;
            bool isReaching = !_isEnding && _elapsed >= _startDelay;
            _weight = Mathf.MoveTowards(_weight, isReaching ? 1f : 0f, Time.unscaledDeltaTime / ReachBlendTime);
            if (_isEnding && _weight <= 0f)
            {
                Destroy(this);
                return;
            }

            if (!_target || _weight <= 0f)
                return;

            Vector3 along = _target.forward;
            along.y = 0f;
            along.Normalize();
            // The hand stops stroking while it is lowered, so it never starts another stroke at the end
            if (!_isEnding)
                _strokeTime += Time.unscaledDeltaTime;

            float stroke = Mathf.Sin(_strokeTime * _strokeSpeed) * _strokeLength;
            float eased = Mathf.SmoothStep(0f, 1f, _weight);
            Lean(eased);
            SolveArm(_target.position + Vector3.up * _height + along * stroke, eased);
            TurnPalmDown(eased);
            OpenFingers(eased);
        }

        // height is how far above the target's pivot the hand rides; the hand starts reaching after startDelay, once the crouch is mostly there
        public static PetStroker Begin(Animator animator, Transform target, float height, float strokeLength, float strokeSpeed, float startDelay, bool openHand, float leanAngle)
        {
            Transform upperArm = animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
            Transform foreArm = animator.GetBoneTransform(HumanBodyBones.RightLowerArm);
            Transform hand = animator.GetBoneTransform(HumanBodyBones.RightHand);
            if (!upperArm || !foreArm || !hand)
                return null;

            PetStroker stroker = animator.gameObject.AddComponent<PetStroker>();
            stroker._upperArm = upperArm;
            stroker._foreArm = foreArm;
            stroker._hand = hand;
            stroker._target = target;
            stroker._height = height;
            stroker._strokeLength = strokeLength;
            stroker._strokeSpeed = strokeSpeed;
            stroker._startDelay = startDelay;
            stroker._leanAngle = leanAngle;
            stroker._spine = animator.GetBoneTransform(HumanBodyBones.Spine);
            stroker._chest = animator.GetBoneTransform(HumanBodyBones.Chest);
            stroker._indexBase = animator.GetBoneTransform(HumanBodyBones.RightIndexProximal);
            stroker._middleBase = animator.GetBoneTransform(HumanBodyBones.RightMiddleProximal);
            stroker._littleBase = animator.GetBoneTransform(HumanBodyBones.RightLittleProximal);
            if (openHand)
                stroker._fingers = GetFingerBones(animator);

            return stroker;
        }

        // Lowers the arm again, then removes itself
        public void End() => _isEnding = true;

        // Bends the back forward over the animal, so a tall crouch can reach something on the floor
        private void Lean(float weight)
        {
            if (!_spine || Mathf.Approximately(_leanAngle, 0f))
                return;

            Quaternion lean = Quaternion.AngleAxis(_leanAngle * 0.5f * weight, transform.right);
            _spine.rotation = lean * _spine.rotation;
            if (_chest)
                _chest.rotation = lean * _chest.rotation;
        }

        // The palm normal comes from the knuckles (fingers along the hand, thumb side across it; for a right hand their cross product points out of the palm), then the hand turns the shortest way to face the floor
        private void TurnPalmDown(float weight)
        {
            if (!_indexBase || !_middleBase || !_littleBase)
                return;

            Vector3 fingers = _middleBase.position - _hand.position;
            Vector3 thumbSide = _indexBase.position - _littleBase.position;
            Vector3 palmNormal = Vector3.Cross(fingers, thumbSide);
            if (palmNormal.sqrMagnitude < 0.000001f)
                return;

            Quaternion turn = Quaternion.FromToRotation(palmNormal, Vector3.down);
            _hand.rotation = Quaternion.Slerp(_hand.rotation, turn * _hand.rotation, weight);
        }

        // A hand that held an axe is curled round nothing; straight fingers (the rig's rest pose) read as an open hand
        private void OpenFingers(float weight)
        {
            if (_fingers == null)
                return;

            foreach (Transform finger in _fingers)
                finger.localRotation = Quaternion.Slerp(finger.localRotation, Quaternion.identity, weight);
        }

        private static Transform[] GetFingerBones(Animator animator)
        {
            HumanBodyBones[] bones =
            {
                HumanBodyBones.RightIndexProximal, HumanBodyBones.RightIndexIntermediate, HumanBodyBones.RightIndexDistal,
                HumanBodyBones.RightMiddleProximal, HumanBodyBones.RightMiddleIntermediate, HumanBodyBones.RightMiddleDistal,
                HumanBodyBones.RightRingProximal, HumanBodyBones.RightRingIntermediate, HumanBodyBones.RightRingDistal,
                HumanBodyBones.RightLittleProximal, HumanBodyBones.RightLittleIntermediate, HumanBodyBones.RightLittleDistal,
            };

            System.Collections.Generic.List<Transform> fingers = new System.Collections.Generic.List<Transform>();
            foreach (HumanBodyBones bone in bones)
            {
                Transform finger = animator.GetBoneTransform(bone);
                if (finger)
                    fingers.Add(finger);
            }

            return fingers.ToArray();
        }

        private void SolveArm(Vector3 goal, float weight)
        {
            Vector3 shoulder = _upperArm.position;
            Vector3 elbow = _foreArm.position;
            Vector3 wrist = _hand.position;
            float upperLength = (elbow - shoulder).magnitude;
            float lowerLength = (wrist - elbow).magnitude;

            Vector3 toGoal = goal - shoulder;
            float distance = Mathf.Clamp(toGoal.magnitude, Mathf.Abs(upperLength - lowerLength) + 0.01f, (upperLength + lowerLength) * 0.999f);
            Vector3 direction = toGoal.normalized;

            // The elbow keeps the side it has in the animation
            Vector3 pole = elbow - shoulder;
            pole -= Vector3.Project(pole, direction);
            pole = pole.sqrMagnitude > 0.000001f ? pole.normalized : Vector3.down;

            float along = (distance * distance + upperLength * upperLength - lowerLength * lowerLength) / (2f * distance);
            float bend = Mathf.Sqrt(Mathf.Max(0f, upperLength * upperLength - along * along));
            Vector3 newElbow = shoulder + direction * along + pole * bend;
            Vector3 newWrist = shoulder + direction * distance;

            Quaternion upperTurn = Quaternion.FromToRotation(elbow - shoulder, newElbow - shoulder);
            Quaternion lowerTurn = Quaternion.FromToRotation(upperTurn * (wrist - elbow), newWrist - newElbow) * upperTurn;
            Quaternion upperRotation = upperTurn * _upperArm.rotation;
            Quaternion lowerRotation = lowerTurn * _foreArm.rotation;
            _upperArm.rotation = Quaternion.Slerp(_upperArm.rotation, upperRotation, weight);
            _foreArm.rotation = Quaternion.Slerp(_foreArm.rotation, lowerRotation, weight);
        }
    }
}
