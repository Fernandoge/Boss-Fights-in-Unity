using UnityEditor;
using UnityEngine;

namespace EditorTools
{
    // Sets up every new FBX dropped into the Mixamo Animation Library as a Humanoid clip on the ninja's rig, named after its file and looping when the name says it should.
    // It only touches an FBX the first time it is imported, so later changes in the Inspector are kept.
    public class MixamoAnimationImporter : AssetPostprocessor
    {
        private const string LibraryFolder = "Assets/Asset Packs/Mixamo/Animation Library/";
        private const string AvatarSourcePath = "Assets/Asset Packs/Mixamo/Ninja Kachujin/Kachujin G Rosales.fbx";
        // The Brute's skeleton has eye bones that the ninja's avatar names differently, so its clips cannot copy the ninja's avatar
        private const string BruteAvatarSourcePath = "Assets/Asset Packs/Axeman/Model/Brute.fbx";
        private static readonly string[] BruteClipPrefixes = { "Axe ", "Unarmed " };
        private static readonly string[] LoopWords = { "idle", "run", "walk", "strafe", "loop" };
        private static readonly string[] OneShotWords = { "stop", "turn", "land to", "to run" };

        private void OnPreprocessModel()
        {
            if (!IsNewLibraryFile())
                return;

            ModelImporter importer = (ModelImporter)assetImporter;
            importer.animationType = ModelImporterAnimationType.Human;
            importer.materialImportMode = ModelImporterMaterialImportMode.None;

            Avatar avatar = AssetDatabase.LoadAssetAtPath<Avatar>(UsesBruteRig() ? BruteAvatarSourcePath : AvatarSourcePath);
            if (avatar != null)
            {
                importer.avatarSetup = ModelImporterAvatarSetup.CopyFromOther;
                importer.sourceAvatar = avatar;
            }
            else
                importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
        }

        private void OnPreprocessAnimation()
        {
            if (!IsNewLibraryFile())
                return;

            ModelImporter importer = (ModelImporter)assetImporter;
            ModelImporterClipAnimation[] clips = importer.defaultClipAnimations;
            if (clips.Length == 0)
                return;

            string clipName = System.IO.Path.GetFileNameWithoutExtension(assetPath);
            bool repeats = ShouldLoop(clipName.ToLowerInvariant());
            clips[0].name = clipName;
            clips[0].loopTime = repeats;
            clips[0].loopPose = repeats;
            importer.clipAnimations = clips;
        }

        private bool UsesBruteRig()
        {
            string fileName = System.IO.Path.GetFileName(assetPath);
            foreach (string prefix in BruteClipPrefixes)
                if (fileName.StartsWith(prefix))
                    return true;

            return false;
        }

        private bool IsNewLibraryFile() => assetPath.StartsWith(LibraryFolder) && assetImporter.importSettingsMissing;

        private static bool ShouldLoop(string lowerName)
        {
            bool repeats = false;
            foreach (string word in LoopWords)
                repeats |= lowerName.Contains(word);

            foreach (string word in OneShotWords)
                if (lowerName.Contains(word))
                    repeats = false;

            return repeats;
        }
    }
}
