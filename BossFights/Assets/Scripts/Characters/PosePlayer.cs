using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace Characters
{
    // Holds a character in one clip on top of its Animator Controller, blending in and out. It runs on real time, so it still plays while the game is frozen. Added in code by whoever needs the pose
    public class PosePlayer : MonoBehaviour
    {
        private PlayableGraph _graph;
        private AnimationMixerPlayable _mixer;
        private AnimationClipPlayable _clipPlayable;
        private Animator _animator;
        private AnimationClip _clip;
        private AnimatorUpdateMode _previousUpdateMode;
        private float _blendTime;
        private float _weight;
        private float _clipTime;
        private bool _isEnding;

        private void Update()
        {
            _clipTime += Time.unscaledDeltaTime;
            _clipPlayable.SetTime(_clip.isLooping ? _clipTime % _clip.length : Mathf.Min(_clipTime, _clip.length));

            _weight = Mathf.MoveTowards(_weight, _isEnding ? 0f : 1f, Time.unscaledDeltaTime / _blendTime);
            float eased = Mathf.SmoothStep(0f, 1f, _weight);
            _mixer.SetInputWeight(0, 1f - eased);
            _mixer.SetInputWeight(1, eased);

            if (_isEnding && _weight <= 0f)
                Destroy(this);
        }

        private void OnDestroy()
        {
            if (_graph.IsValid())
                _graph.Destroy();

            if (_animator)
                _animator.updateMode = _previousUpdateMode;
        }

        public static PosePlayer Begin(Animator animator, AnimationClip clip, float blendTime)
        {
            PosePlayer pose = animator.gameObject.AddComponent<PosePlayer>();
            pose._animator = animator;
            pose._clip = clip;
            pose._blendTime = Mathf.Max(blendTime, 0.01f);
            pose._previousUpdateMode = animator.updateMode;
            animator.updateMode = AnimatorUpdateMode.UnscaledTime;
            pose.Build();
            return pose;
        }

        // Blends back to the controller, then removes itself
        public void End() => _isEnding = true;

        private void Build()
        {
            _graph = PlayableGraph.Create("Pose");
            _graph.SetTimeUpdateMode(DirectorUpdateMode.UnscaledGameTime);
            AnimationPlayableOutput output = AnimationPlayableOutput.Create(_graph, "Pose", _animator);
            _mixer = AnimationMixerPlayable.Create(_graph, 2);
            AnimatorControllerPlayable controller = AnimatorControllerPlayable.Create(_graph, _animator.runtimeAnimatorController);
            _clipPlayable = AnimationClipPlayable.Create(_graph, _clip);
            _clipPlayable.SetSpeed(0f);
            _graph.Connect(controller, 0, _mixer, 0);
            _graph.Connect(_clipPlayable, 0, _mixer, 1);
            _mixer.SetInputWeight(0, 1f);
            output.SetSourcePlayable(_mixer);
            _graph.Play();
        }
    }
}
