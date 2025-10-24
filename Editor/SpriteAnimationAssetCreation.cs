using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.ProjectWindowCallback;
using UnityEngine;

namespace TakoBoyStudios.Animation.Editor
{
    /// <summary>
    /// Editor utility for creating sprite animation assets and animations from sprite sheets.
    /// </summary>
    /// <remarks>
    /// This tool uses a specific naming convention to automatically create animations:
    /// 
    /// Format: AssetName@AnimationName_WxH.png
    /// 
    /// Where:
    /// - AssetName: Name of the SpriteAnimationAsset to create or update
    /// - AnimationName: Name of the animation (use "loop" keyword for auto-looping)
    /// - WxH: Width x Height of each frame for slicing (optional if already sliced)
    /// 
    /// Example:
    /// - Character@idle_loop_32x32.png → Creates/updates "Character" asset with "idle_loop" animation
    /// - Enemy@attack_64x64.png → Creates/updates "Enemy" asset with "attack" animation
    /// 
    /// The tool also supports JSON frame timing files with the same base name as the sprite.
    /// </remarks>
    public static class SpriteAnimationAssetCreation
    {
        /// <summary>
        /// Default frame time in seconds if no JSON timing is provided.
        /// </summary>
        public const float DEFAULT_FRAME_TIME = 0.04f;

        /// <summary>
        /// Default path where animation assets are saved.
        /// Can be configured via EditorPrefs key "TakoBoy_AnimationSavePath".
        /// </summary>
        public const string DEFAULT_ANIMATION_SAVE_PATH = "Assets/Animations/";

        private const string SAVE_PATH_PREF_KEY = "TakoBoy_AnimationSavePath";

        /// <summary>
        /// Creates a new empty SpriteAnimationAsset.
        /// </summary>
        [MenuItem("Assets/Create/TakoBoy Studios/Animation/Create Animation Asset", priority = 8)]
        public static void CreateSpriteAnimationAsset()
        {
            var asset = ScriptableObject.CreateInstance<SpriteAnimationAsset>();
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(
                asset.GetInstanceID(),
                ScriptableObject.CreateInstance<EndSpriteAnimationAssetNameEdit>(),
                "SpriteAnimationAsset.asset",
                AssetPreview.GetMiniThumbnail(asset),
                null
            );
        }

        /// <summary>
        /// Creates sprite animations from selected sprite textures using the naming convention.
        /// </summary>
        /// <remarks>
        /// Select one or more textures in the Project window and use this menu item.
        /// The textures must follow the naming convention: AssetName@AnimationName_WxH.png
        /// 
        /// If a JSON file with matching name exists (AssetName@AnimationName.json), it will be used for frame timing.
        /// JSON format: {"frames": [100, 100, 150, 100]} where values are milliseconds per frame.
        /// </remarks>
        [MenuItem("Assets/TakoBoy Studios/Animation/Create Animation from Selected", priority = 10)]
        public static void CreateSpriteAnimationFromSelected()
        {
            string[] guids = Selection.assetGUIDs;

            if (guids == null || guids.Length == 0)
            {
                EditorUtility.DisplayDialog(
                    "No Selection",
                    "Please select one or more sprite textures to create animations from.",
                    "OK"
                );
                return;
            }

            var animationAssetsToReimport = new List<SpriteAnimationAsset>();
            int successCount = 0;
            int failCount = 0;

            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);

                if (texture == null)
                    continue;

                string textureName = texture.name.Split('_')[0];

                // Check if name contains the required '@' separator
                if (!textureName.Contains("@"))
                {
                    Debug.LogWarning(
                        $"[SpriteAnimationCreator] Skipping '{texture.name}' - " +
                        "name doesn't follow convention: AssetName@AnimationName_WxH.png"
                    );
                    failCount++;
                    continue;
                }

                // Parse the naming convention
                string[] nameParts = textureName.Split('@');
                string animationAssetName = nameParts[0];
                string animationName = nameParts[1];

                // Get or create the animation asset
                string animationAssetPath = GetAnimationAssetSavePath() + animationAssetName + ".asset";
                SpriteAnimationAsset animationAsset = AssetDatabase.LoadAssetAtPath<SpriteAnimationAsset>(animationAssetPath);

                if (animationAsset == null)
                {
                    animationAsset = ScriptableObject.CreateInstance<SpriteAnimationAsset>();
                    animationAsset.name = animationAssetName;
                    animationAsset.animations = new List<SpriteAnimationData>();
                    AssetDatabase.CreateAsset(animationAsset, AssetDatabase.GenerateUniqueAssetPath(animationAssetPath));
                    Debug.Log($"[SpriteAnimationCreator] Created new animation asset: {animationAssetName}");
                }

                // Get or create the animation data
                SpriteAnimationData animationData = animationAsset.animations.Find(a => a.name == animationName);

                if (animationData == null)
                {
                    animationData = new SpriteAnimationData();
                    animationData.name = animationName;
                    // Auto-detect loop mode from name
                    animationData.loop = animationName.ToLower().Contains("loop")
                        ? SpriteAnimationLoopMode.LOOPTOSTART
                        : SpriteAnimationLoopMode.NOLOOP;
                    animationAsset.animations.Add(animationData);
                }

                // Clear existing frames
                animationData.frameDatas = new List<SpriteAnimationFrameData>();

                // Load all sprites from the texture
                Sprite[] sprites = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToArray();

                if (sprites.Length == 0)
                {
                    Debug.LogWarning($"[SpriteAnimationCreator] No sprites found in '{texture.name}'");
                    failCount++;
                    continue;
                }

                // Try to load frame timing from JSON
                GifFrameTiming frameTimingData = TryLoadFrameTiming(path);

                // Create frame data for each sprite
                for (int j = 0; j < sprites.Length; j++)
                {
                    Sprite sprite = sprites[j];
                    SpriteAnimationFrameData frameData = new SpriteAnimationFrameData();

                    // Use JSON timing if available, otherwise use default
                    if (frameTimingData != null && frameTimingData.frames.Count > j)
                    {
                        frameData.time = frameTimingData.frames[j] / 1000f; // Convert ms to seconds
                    }
                    else
                    {
                        frameData.time = DEFAULT_FRAME_TIME;
                    }

                    frameData.sprite = sprite;
                    animationData.frameDatas.Add(frameData);
                }

                if (!animationAssetsToReimport.Contains(animationAsset))
                {
                    animationAssetsToReimport.Add(animationAsset);
                }

                successCount++;
                Debug.Log(
                    $"[SpriteAnimationCreator] Created animation '{animationName}' with {sprites.Length} frames " +
                    $"in asset '{animationAssetName}'"
                );
            }

            // Save all modified assets
            for (int i = 0; i < animationAssetsToReimport.Count; i++)
            {
                EditorUtility.SetDirty(animationAssetsToReimport[i]);
                AssetDatabase.ImportAsset(
                    AssetDatabase.GetAssetPath(animationAssetsToReimport[i]),
                    ImportAssetOptions.ForceUpdate
                );
            }

            AssetDatabase.SaveAssets();

            // Show result dialog
            string message = $"Animation Creation Complete!\n\n" +
                           $"Successfully created: {successCount}\n" +
                           $"Failed: {failCount}";

            EditorUtility.DisplayDialog("Animation Creation", message, "OK");
        }

        /// <summary>
        /// Validates if the Create Animation menu item should be enabled.
        /// </summary>
        [MenuItem("Assets/TakoBoy Studios/Animation/Create Animation from Selected", true)]
        private static bool ValidateCreateSpriteAnimation()
        {
            // Only enable if at least one texture is selected
            foreach (Object obj in Selection.objects)
            {
                if (obj is Texture2D)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Opens preferences to configure the animation save path.
        /// </summary>
        [MenuItem("Assets/TakoBoy Studios/Animation/Configure Save Path", priority = 200)]
        public static void ConfigureSavePath()
        {
            string currentPath = GetAnimationAssetSavePath();
            string newPath = EditorUtility.OpenFolderPanel("Select Animation Asset Save Location", "Assets", "");

            if (!string.IsNullOrEmpty(newPath))
            {
                // Convert absolute path to relative
                if (newPath.StartsWith(Application.dataPath))
                {
                    newPath = "Assets" + newPath.Substring(Application.dataPath.Length);
                }

                // Ensure path ends with /
                if (!newPath.EndsWith("/"))
                    newPath += "/";

                EditorPrefs.SetString(SAVE_PATH_PREF_KEY, newPath);
                Debug.Log($"[SpriteAnimationCreator] Animation save path set to: {newPath}");
            }
        }

        #region Private Helper Methods

        /// <summary>
        /// Gets the configured animation asset save path from EditorPrefs.
        /// </summary>
        private static string GetAnimationAssetSavePath()
        {
            string path = EditorPrefs.GetString(SAVE_PATH_PREF_KEY, DEFAULT_ANIMATION_SAVE_PATH);

            // Ensure the directory exists
            if (!AssetDatabase.IsValidFolder(path.TrimEnd('/')))
            {
                // Try to create the folders
                string[] folders = path.Split('/');
                string currentPath = folders[0];

                for (int i = 1; i < folders.Length; i++)
                {
                    if (string.IsNullOrEmpty(folders[i]))
                        continue;

                    string newPath = currentPath + "/" + folders[i];
                    if (!AssetDatabase.IsValidFolder(newPath))
                    {
                        AssetDatabase.CreateFolder(currentPath, folders[i]);
                    }
                    currentPath = newPath;
                }
            }

            return path;
        }

        /// <summary>
        /// Attempts to load frame timing data from a JSON file.
        /// </summary>
        private static GifFrameTiming TryLoadFrameTiming(string spritePath)
        {
            // Build JSON path from sprite path
            string[] fileNameSplit = Path.GetFileNameWithoutExtension(spritePath).Split('_');
            string fileName = fileNameSplit[0] + ".json";
            string jsonPath = Path.GetDirectoryName(spritePath) + "/" + fileName;

            TextAsset frameAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(jsonPath);

            if (frameAsset != null)
            {
                try
                {
                    GifFrameTiming timing = JsonUtility.FromJson<GifFrameTiming>(frameAsset.text);
                    if (timing != null && timing.frames != null && timing.frames.Count > 0)
                    {
                        Debug.Log($"[SpriteAnimationCreator] Loaded frame timing from: {jsonPath}");
                        return timing;
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"[SpriteAnimationCreator] Failed to parse JSON timing file '{jsonPath}': {e.Message}");
                }
            }

            return null;
        }

        #endregion

        #region Helper Classes

        /// <summary>
        /// JSON format for frame timing data.
        /// </summary>
        [System.Serializable]
        private class GifFrameTiming
        {
            /// <summary>
            /// List of frame durations in milliseconds.
            /// </summary>
            public List<float> frames;
        }

        #endregion
    }

    /// <summary>
    /// Helper class for asset creation workflow.
    /// </summary>
    internal class EndSpriteAnimationAssetNameEdit : EndNameEditAction
    {
        public override void Action(int instanceId, string pathName, string resourceFile)
        {
            AssetDatabase.CreateAsset(
                EditorUtility.InstanceIDToObject(instanceId),
                AssetDatabase.GenerateUniqueAssetPath(pathName)
            );
        }
    }
}
