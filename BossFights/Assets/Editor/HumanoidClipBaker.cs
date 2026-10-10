using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace EditorTools
{
    // Bakes a Humanoid clip onto a Generic character (Gorath): the prefab is posed with the clip, frame by frame, through a Humanoid avatar of the same skeleton, and the local transform of every bone is written into a new path-based clip that a Generic Animator can play.
    // Run it from the Unity CLI: eval `EditorTools.HumanoidClipBaker.Bake(clipFbxPath, prefabPath, humanAvatarFbxPath, outputPath);`
    public static class HumanoidClipBaker
    {
        private const float FrameRate = 30f;
        private const float PositionTolerance = 0.0001f;
        private const float RotationToleranceDegrees = 0.01f;
        private const float KeyTolerance = 0.002f;

        private class BoneRecording
        {
            public string path;
            public Vector3 startPosition;
            public Quaternion startRotation;
            public readonly List<Vector3> positions = new List<Vector3>();
            public readonly List<Quaternion> rotations = new List<Quaternion>();
        }

        public static AnimationClip Bake(string humanoidClipAssetPath, string prefabPath, string humanAvatarAssetPath, string outputPath)
        {
            AnimationClip source = LoadClip(humanoidClipAssetPath);
            Avatar avatar = AssetDatabase.LoadAssetAtPath<Avatar>(humanAvatarAssetPath);
            if (source == null || avatar == null || !avatar.isHuman)
                return null;

            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                Animator animator = root.GetComponent<Animator>();
                animator.runtimeAnimatorController = null;
                animator.avatar = avatar;
                animator.applyRootMotion = false;
                animator.Rebind();

                List<BoneRecording> recordings = CreateRecordings(root.transform);
                List<float> times = Sample(source, root.transform, recordings);
                return SaveClip(CreateClip(source, recordings, times), outputPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static AnimationClip LoadClip(string assetPath)
        {
            foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(assetPath))
            {
                AnimationClip clip = asset as AnimationClip;
                if (clip != null && !clip.name.StartsWith("__preview"))
                    return clip;
            }

            return null;
        }

        private static List<BoneRecording> CreateRecordings(Transform root)
        {
            List<BoneRecording> recordings = new List<BoneRecording>();
            foreach (Transform bone in root.GetComponentsInChildren<Transform>(true))
            {
                if (bone == root)
                    continue;

                recordings.Add(new BoneRecording
                {
                    path = AnimationUtility.CalculateTransformPath(bone, root),
                    startPosition = bone.localPosition,
                    startRotation = bone.localRotation
                });
            }

            return recordings;
        }

        private static List<float> Sample(AnimationClip source, Transform root, List<BoneRecording> recordings)
        {
            Dictionary<string, Transform> bonesByPath = new Dictionary<string, Transform>();
            foreach (BoneRecording recording in recordings)
                bonesByPath[recording.path] = root.Find(recording.path);

            // A Humanoid clip moves the character's root as well; the boss root belongs to its NavMeshAgent, so that movement is written into the bones that hang from the root, relative to where the root started
            Matrix4x4 worldToRoot = root.worldToLocalMatrix;
            List<float> times = new List<float>();
            int frames = Mathf.CeilToInt(source.length * FrameRate);
            for (int frame = 0; frame <= frames; frame++)
            {
                float time = Mathf.Min(frame / FrameRate, source.length);
                source.SampleAnimation(root.gameObject, time);
                times.Add(time);

                foreach (BoneRecording recording in recordings)
                {
                    Transform bone = bonesByPath[recording.path];
                    if (bone.parent == root)
                    {
                        Matrix4x4 relative = worldToRoot * bone.localToWorldMatrix;
                        recording.positions.Add(relative.GetPosition());
                        recording.rotations.Add(relative.rotation);
                    }
                    else
                    {
                        recording.positions.Add(bone.localPosition);
                        recording.rotations.Add(bone.localRotation);
                    }
                }
            }

            return times;
        }

        // Only bones that move away from their pose in the prefab get curves; the others stay where the Animator's default pose puts them
        private static AnimationClip CreateClip(AnimationClip source, List<BoneRecording> recordings, List<float> times)
        {
            AnimationClip clip = new AnimationClip { frameRate = FrameRate };
            foreach (BoneRecording recording in recordings)
            {
                if (HasMoved(recording.positions, recording.startPosition))
                {
                    SetCurve(clip, recording.path, "m_LocalPosition.x", times, recording.positions, position => position.x);
                    SetCurve(clip, recording.path, "m_LocalPosition.y", times, recording.positions, position => position.y);
                    SetCurve(clip, recording.path, "m_LocalPosition.z", times, recording.positions, position => position.z);
                }

                if (HasRotated(recording.rotations, recording.startRotation))
                {
                    // q and -q are the same rotation, but the curves must not jump between them
                    for (int i = 1; i < recording.rotations.Count; i++)
                    {
                        if (Quaternion.Dot(recording.rotations[i - 1], recording.rotations[i]) < 0f)
                        {
                            Quaternion flipped = recording.rotations[i];
                            recording.rotations[i] = new Quaternion(-flipped.x, -flipped.y, -flipped.z, -flipped.w);
                        }
                    }

                    SetCurve(clip, recording.path, "m_LocalRotation.x", times, recording.rotations, rotation => rotation.x);
                    SetCurve(clip, recording.path, "m_LocalRotation.y", times, recording.rotations, rotation => rotation.y);
                    SetCurve(clip, recording.path, "m_LocalRotation.z", times, recording.rotations, rotation => rotation.z);
                    SetCurve(clip, recording.path, "m_LocalRotation.w", times, recording.rotations, rotation => rotation.w);
                }
            }

            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = false;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            clip.name = source.name;
            return clip;
        }

        private static bool HasMoved(List<Vector3> positions, Vector3 start)
        {
            foreach (Vector3 position in positions)
            {
                if ((position - start).sqrMagnitude > PositionTolerance * PositionTolerance)
                    return true;
            }

            return false;
        }

        private static bool HasRotated(List<Quaternion> rotations, Quaternion start)
        {
            foreach (Quaternion rotation in rotations)
            {
                if (Quaternion.Angle(rotation, start) > RotationToleranceDegrees)
                    return true;
            }

            return false;
        }

        private static void SetCurve<T>(AnimationClip clip, string path, string property, List<float> times, List<T> values, System.Func<T, float> select)
        {
            List<float> samples = new List<float>();
            foreach (T value in values)
                samples.Add(select(value));

            AnimationCurve curve = new AnimationCurve();
            foreach (int index in Simplify(samples))
                curve.AddKey(new Keyframe(times[index], samples[index]));

            for (int i = 0; i < curve.length; i++)
            {
                AnimationUtility.SetKeyLeftTangentMode(curve, i, AnimationUtility.TangentMode.Linear);
                AnimationUtility.SetKeyRightTangentMode(curve, i, AnimationUtility.TangentMode.Linear);
            }

            AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(path, typeof(Transform), property), curve);
        }

        // Keeps only the samples a straight line between the kept neighbours could not stand in for (Ramer-Douglas-Peucker), so smooth motion needs few keys
        private static List<int> Simplify(List<float> samples)
        {
            SortedSet<int> kept = new SortedSet<int> { 0, samples.Count - 1 };
            Stack<(int, int)> segments = new Stack<(int, int)>();
            segments.Push((0, samples.Count - 1));
            while (segments.Count > 0)
            {
                (int first, int last) = segments.Pop();
                int worst = -1;
                float worstError = KeyTolerance;
                for (int i = first + 1; i < last; i++)
                {
                    float expected = Mathf.Lerp(samples[first], samples[last], (i - first) / (float)(last - first));
                    float error = Mathf.Abs(samples[i] - expected);
                    if (error > worstError)
                    {
                        worst = i;
                        worstError = error;
                    }
                }

                if (worst < 0)
                    continue;

                kept.Add(worst);
                segments.Push((first, worst));
                segments.Push((worst, last));
            }

            return new List<int>(kept);
        }

        // An existing clip is overwritten in place, so the states that use it keep their reference
        private static AnimationClip SaveClip(AnimationClip clip, string outputPath)
        {
            AnimationClip existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(outputPath);
            if (existing != null)
            {
                EditorUtility.CopySerialized(clip, existing);
                clip = existing;
            }
            else
            {
                AssetDatabase.CreateAsset(clip, outputPath);
            }

            AssetDatabase.SaveAssets();
            return clip;
        }
    }
}
