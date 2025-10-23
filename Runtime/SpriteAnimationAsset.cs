using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TakoBoyStudios.Animation
{
    /// <summary>
    /// Time mode for sprite animation playback.
    /// </summary>
    public enum SpriteAnimationTimeMode
    {
        /// <summary>
        /// Uses Time.deltaTime (affected by Time.timeScale).
        /// </summary>
        NORMAL = 0,

        /// <summary>
        /// Uses Time.unscaledDeltaTime (not affected by Time.timeScale).
        /// </summary>
        TIMESCALEINDEPENDENT = 1
    }

    /// <summary>
    /// Loop mode for sprite animations.
    /// </summary>
    public enum SpriteAnimationLoopMode
    {
        /// <summary>
        /// Play once and stop at the last frame.
        /// </summary>
        NOLOOP = 0,

        /// <summary>
        /// Loop back to the first frame when reaching the end.
        /// </summary>
        LOOPTOSTART = 1,

        /// <summary>
        /// Loop back to a specific frame when reaching the end.
        /// </summary>
        LOOPTOFRAME = 2
    }

    /// <summary>
    /// Data for a single frame in a sprite animation.
    /// </summary>
    [System.Serializable]
    public class SpriteAnimationFrameData
    {
        /// <summary>
        /// The sprite to display for this frame.
        /// </summary>
        [Tooltip("Sprite to display for this frame")]
        public Sprite sprite;

        /// <summary>
        /// Duration in seconds to display this frame.
        /// </summary>
        [Tooltip("Duration in seconds to display this frame")]
        public float time;
    }

    /// <summary>
    /// Complete animation data including all frames and playback settings.
    /// </summary>
    [System.Serializable]
    public class SpriteAnimationData
    {
        /// <summary>
        /// Name of the animation.
        /// </summary>
        [Tooltip("Name of this animation")]
        public string name;

        /// <summary>
        /// Speed multiplier for playback (1.0 = normal speed).
        /// </summary>
        [Range(0.001f, 10f)]
        [Tooltip("Speed multiplier for playback (1.0 = normal speed)")]
        public float speedRatio = 1f;

        /// <summary>
        /// Loop mode for this animation.
        /// </summary>
        [Tooltip("How the animation should loop")]
        [SerializeField]
        public SpriteAnimationLoopMode loop;

        /// <summary>
        /// Frame index to loop back to when using LOOPTOFRAME mode.
        /// </summary>
        [Tooltip("Frame to loop back to (only used with LOOPTOFRAME mode)")]
        public int frameToLoop = 0;

        /// <summary>
        /// List of all frames in this animation.
        /// </summary>
        [Tooltip("All frames in this animation")]
        [SerializeField]
        public List<SpriteAnimationFrameData> frameDatas = new List<SpriteAnimationFrameData>();

        /// <summary>
        /// Currently selected frame index in the editor.
        /// </summary>
        [HideInInspector]
        public int selectedIndex = 0;

        /// <summary>
        /// Default time for new frames added in the editor.
        /// </summary>
        [HideInInspector]
        public float newFramesTime = 0.05f;

        /// <summary>
        /// Time to set for multiple frames at once in the editor.
        /// </summary>
        [HideInInspector]
        public float setFramesTime = 0.1f;

        /// <summary>
        /// Validates that this animation data is usable.
        /// </summary>
        /// <returns>True if the animation has valid settings and at least one frame</returns>
        public bool Valid()
        {
            return speedRatio > 0 && frameToLoop >= 0 && frameDatas != null && frameDatas.Count > 0;
        }
    }

    /// <summary>
    /// ScriptableObject that stores a collection of sprite animations.
    /// </summary>
    /// <remarks>
    /// This asset stores multiple animations that can be referenced by SpriteAnimation components.
    /// Animations can be created using the sprite slicing workflow and naming convention:
    /// AssetName@AnimationName_WxH.png
    /// 
    /// Where:
    /// - AssetName: Name of the SpriteAnimationAsset to create/update
    /// - AnimationName: Name of the animation (supports "loop" keyword for auto-looping)
    /// - WxH: Width and height of each frame for slicing
    /// </remarks>
    [CreateAssetMenu(fileName = "SpriteAnimationAsset", menuName = "TakoBoy Studios/Animation/Sprite Animation Asset")]
    public class SpriteAnimationAsset : ScriptableObject
    {
        /// <summary>
        /// Whether to show confirmation dialogs in the editor when making changes.
        /// </summary>
        [Tooltip("Show confirmation dialogs when making changes in editor")]
        public bool editorConfirmations = true;

        /// <summary>
        /// List of all animations stored in this asset.
        /// </summary>
        [Tooltip("All animations in this asset")]
        public List<SpriteAnimationData> animations = new List<SpriteAnimationData>();

        /// <summary>
        /// Sorts all animations alphabetically by name.
        /// </summary>
        public void SortAnimationsAlphabetically()
        {
            if (animations != null && animations.Count > 0)
            {
                animations = animations.OrderBy(x => x.name).ToList();
            }
        }

        /// <summary>
        /// Gets an animation by name.
        /// </summary>
        /// <param name="animationName">Name of the animation to find</param>
        /// <returns>The animation data, or null if not found</returns>
        public SpriteAnimationData GetAnimation(string animationName)
        {
            if (string.IsNullOrEmpty(animationName) || animations == null)
                return null;

            return animations.FirstOrDefault(a => a.name == animationName);
        }

        /// <summary>
        /// Checks if an animation with the given name exists.
        /// </summary>
        /// <param name="animationName">Name to check</param>
        /// <returns>True if an animation with this name exists</returns>
        public bool HasAnimation(string animationName)
        {
            return GetAnimation(animationName) != null;
        }

        /// <summary>
        /// Adds a new animation to the asset.
        /// </summary>
        /// <param name="animation">Animation data to add</param>
        /// <returns>True if added successfully, false if null or duplicate name</returns>
        public bool AddAnimation(SpriteAnimationData animation)
        {
            if (animation == null || HasAnimation(animation.name))
                return false;

            animations.Add(animation);
            return true;
        }

        /// <summary>
        /// Removes an animation by name.
        /// </summary>
        /// <param name="animationName">Name of animation to remove</param>
        /// <returns>True if removed successfully</returns>
        public bool RemoveAnimation(string animationName)
        {
            SpriteAnimationData animation = GetAnimation(animationName);
            if (animation == null)
                return false;

            return animations.Remove(animation);
        }
    }
}
