using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Sirenix.OdinInspector;

namespace TakoBoyStudios.Animation
{
    /// <summary>
    /// Represents an item in the animation list for inspector display.
    /// </summary>
    [Serializable]
    public class AnimationListItem
    {
        public string animationName;
        public SpriteAnimationData animation;
        public SpriteAnimationAsset file;
    }

    /// <summary>
    /// Component that plays sprite-based animations on SpriteRenderer or UI Image components.
    /// Supports frame-by-frame animation with customizable timing, looping modes, and callbacks.
    /// </summary>
    /// <remarks>
    /// This animator supports:
    /// - SpriteRenderer (3D/2D sprites)
    /// - UI Image (Canvas UI)
    /// - Multiple loop modes (none, loop to start, loop to frame)
    /// - Animation events via callbacks
    /// - Speed control and time scaling
    /// - Reverse playback
    /// - Editor preview support
    /// 
    /// Animations are stored in SpriteAnimationAsset ScriptableObjects and can be played by name.
    /// </remarks>
    [DisallowMultipleComponent]
    [ExecuteInEditMode]
    public partial class SpriteAnimation : MonoBehaviour
    {
        #region Serialized Fields

        [BoxGroup("Animation Asset")]
        [Tooltip("Current animation asset containing all animations")]
        [OnValueChanged("OnAnimationAssetChanged")]
        public SpriteAnimationAsset animationAsset;

        [BoxGroup("Playback Settings")]
        [Tooltip("Time mode for animation playback")]
        public SpriteAnimationTimeMode mode;

        [BoxGroup("Playback Settings")]
        [Tooltip("Speed multiplier for animation playback (1.0 = normal speed)")]
        [Range(0.1f, 3f)]
        public float speedRatio = 1f;

        [BoxGroup("Playback Settings")]
        [Tooltip("Frame index to start playing from when enabled")]
        [Min(0)]
        public int playFrom;

        [BoxGroup("Playback Settings")]
        [Tooltip("Override Unity's Time.timeScale with custom value")]
        public bool overrideTimeScale;

        [BoxGroup("Playback Settings")]
        [ShowIf("overrideTimeScale")]
        [Tooltip("Custom time scale value when overrideTimeScale is true")]
        [Range(0f, 3f)]
        public float timeScaleOverride = 1f;

        [SerializeField]
        [HideInInspector]
        public List<AnimationListItem> list;

        #endregion

        #region Public State Fields

        [HideInInspector] public bool playing;
        [HideInInspector] public bool paused;
        [HideInInspector] public float timer;
        [HideInInspector] public int animIdx = -1;
        [HideInInspector] public int currentIdx;

        #endregion

        #region Private Fields

        private bool m_reversed;
        private bool m_singleFrame;
        private int m_currentAnimId;
        private SpriteRenderer m_renderer;
        private Image m_image;
        private SpriteAnimationData m_currentAnimation;

        private Action m_animCompleteCallback;
        private Action<int> m_animFrameUpdateCallback;

        private Dictionary<string, SpriteAnimationData> m_animationsByName;
        private Dictionary<string, int> m_animationsById;

        #endregion

        #region Events

        /// <summary>
        /// Invoked when an animation completes (reaches end for non-looping, or loops for looping animations).
        /// </summary>
        public event Action AnimationComplete;

        /// <summary>
        /// Invoked every time the current frame changes. Passes the new frame index.
        /// </summary>
        public event Action<int> AnimationFrameUpdate;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the SpriteRenderer component (cached). Returns null if using UI Image.
        /// </summary>
        public new SpriteRenderer renderer
        {
            get
            {
                if (m_renderer == null)
                    m_renderer = GetComponent<SpriteRenderer>();
                return m_renderer;
            }
        }

        /// <summary>
        /// Gets the UI Image component (cached). Returns null if using SpriteRenderer.
        /// </summary>
        public Image image
        {
            get
            {
                if (m_image == null)
                    m_image = GetComponent<Image>();
                return m_image;
            }
        }

        /// <summary>
        /// Returns true if the animation is done playing (not playing, single frame, or at last frame).
        /// </summary>
        public bool IsDone => !playing || m_singleFrame || (CurrentFrame >= CurrentFrameCount - 1 && timer > 0);

        /// <summary>
        /// Returns true if the animation will be done on the next frame.
        /// </summary>
        public bool IsDoneNextFrame => !playing || m_singleFrame || CurrentFrame >= CurrentFrameCount - 1;

        /// <summary>
        /// Returns true if the animation system needs to be initialized.
        /// </summary>
        public bool NeedsToInitialize => m_animationsByName == null || m_animationsById == null || list == null;

        /// <summary>
        /// Gets the total number of animations available.
        /// </summary>
        public int AnimationCount => list?.Count ?? 0;

        /// <summary>
        /// Gets the current animation data.
        /// </summary>
        protected SpriteAnimationData CurrentAnimation => m_currentAnimation;

        /// <summary>
        /// Gets the current animation ID.
        /// </summary>
        public int CurrentAnimationId => m_currentAnimId;

        /// <summary>
        /// Gets the name of the currently playing animation.
        /// </summary>
        public string CurrentAnimationName => CurrentAnimation != null ? CurrentAnimation.name : string.Empty;

        /// <summary>
        /// Gets the index of the current animation in the animation list.
        /// </summary>
        public int CurrentAnimationIdx => animIdx;

        /// <summary>
        /// Gets or sets the current frame index.
        /// </summary>
        public int CurrentFrame
        {
            get => currentIdx;
            set => currentIdx = value;
        }

        /// <summary>
        /// Gets the total number of frames in the current animation.
        /// </summary>
        public int CurrentFrameCount
        {
            get
            {
                if (m_animationsByName != null && m_animationsByName.ContainsKey(CurrentAnimationName))
                {
                    return m_animationsByName[CurrentAnimationName].frameDatas.Count;
                }
                return 0;
            }
        }

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (NeedsToInitialize)
            {
                UpdateAnimations();
            }
        }

        private void Start()
        {
            if (!Application.isPlaying)
                return;

            if (playing || animIdx < 0 || list == null || list.Count <= 0)
                return;

            if (animIdx >= list.Count || animIdx < 0)
            {
                animIdx = list.Count - 1;
            }

            Play(list[animIdx].animationName, playFrom);
        }

        private void Update()
        {
            if (!Application.isPlaying)
                return;

            OnUpdate(GetSpeedDelta());
        }

        private void OnDestroy()
        {
            RemoveCallbacks();
        }

        private void Reset()
        {
            UpdateAnimations();
        }

        #endregion

        #region Animation Playback

        /// <summary>
        /// Plays an animation by name with a completion callback.
        /// </summary>
        /// <param name="animName">Name of the animation to play</param>
        /// <param name="callback">Callback invoked when animation completes</param>
        public void Play(string animName, Action callback)
        {
            Play(animName);
            m_animCompleteCallback = callback;
            AnimationComplete += m_animCompleteCallback;
        }

        /// <summary>
        /// Plays an animation by name starting from a specific frame.
        /// </summary>
        /// <param name="animName">Name of the animation to play</param>
        /// <param name="startFrame">Frame index to start from</param>
        public bool Play(string animName, int startFrame = 0)
        {
            if (string.IsNullOrEmpty(animName) || !HasAnimation(animName))
                return false;
                
            SetCurrentAnimation(animName, true, startFrame);
            return true;
        }

        /// <summary>
        /// Plays an animation by name in reverse.
        /// </summary>
        /// <param name="animName">Name of the animation to play</param>
        public void PlayReverse(string animName)
        {
            m_reversed = true;
            SetCurrentAnimation(animName, true);
        }

        /// <summary>
        /// Plays an animation by ID.
        /// </summary>
        /// <param name="id">Animation ID</param>
        public void PlayById(int id)
        {
            var anim = GetAnimationData(id);
            if (anim != null)
            {
                Play(anim.name);
            }
        }

        /// <summary>
        /// Stops the current animation.
        /// </summary>
        public void Stop()
        {
            playing = false;
            paused = false;
            RemoveCallbacks();
        }

        /// <summary>
        /// Pauses or resumes the animation.
        /// </summary>
        /// <param name="isPaused">True to pause, false to resume</param>
        public void Pause(bool isPaused)
        {
            paused = isPaused;
        }

        /// <summary>
        /// Sets a single frame without playing animation.
        /// </summary>
        /// <param name="animName">Animation name</param>
        /// <param name="frameIdx">Frame index to display</param>
        public void SetFrame(string animName, int frameIdx)
        {
            m_singleFrame = true;
            SetCurrentAnimation(animName, false, frameIdx);
        }

        /// <summary>
        /// Sets the current animation without auto-playing.
        /// </summary>
        /// <param name="animName">Name of the animation</param>
        /// <param name="play">Whether to start playing immediately</param>
        /// <param name="startFrame">Frame index to start from</param>
        public void SetCurrentAnimation(string animName, bool play = false, int startFrame = 0)
        {
            if (NeedsToInitialize)
                UpdateAnimations();

            if (string.IsNullOrEmpty(animName))
            {
                animIdx = -1;
                m_currentAnimation = null;
                m_currentAnimId = -1;
                return;
            }

            SpriteAnimationData newData = GetAnimationData(animName);

            if (newData == null)
            {
                Debug.LogWarning($"[SpriteAnimation] Animation '{animName}' not found on {name}");
                return;
            }

            m_currentAnimation = newData;
            m_currentAnimId = GetAnimationId(animName);
            animIdx = m_currentAnimId;
            CurrentFrame = Mathf.Clamp(startFrame, 0, CurrentFrameCount - 1);
            timer = 0;

            if (play)
            {
                PlayCurrentAnim();
            }
            else
            {
                SetCurrentFrame();
            }
        }

        #endregion

        #region Animation Management

        /// <summary>
        /// Updates the internal animation dictionaries from the asset.
        /// Call this when animation assets change.
        /// </summary>
        public void UpdateAnimations()
        {
            m_animationsByName = new Dictionary<string, SpriteAnimationData>();
            m_animationsById = new Dictionary<string, int>();
            list = new List<AnimationListItem>();

            if (animationAsset == null || animationAsset.animations == null)
                return;

            int id = 0;
            foreach (var data in animationAsset.animations.Where(a => a.Valid()))
            {
                var item = new AnimationListItem
                {
                    animationName = data.name,
                    animation = data,
                    file = animationAsset
                };

                list.Add(item);
                m_animationsByName[data.name] = data;
                m_animationsById[data.name] = id++;
            }
        }

        /// <summary>
        /// Gets animation data by name.
        /// </summary>
        /// <param name="animName">Name of the animation</param>
        /// <returns>Animation data, or null if not found</returns>
        public SpriteAnimationData GetAnimationData(string animName)
        {
            if (NeedsToInitialize)
                UpdateAnimations();

            if (m_animationsByName == null)
                return null;

            return m_animationsByName.ContainsKey(animName) && m_animationsByName[animName].Valid()
                ? m_animationsByName[animName]
                : null;
        }

        /// <summary>
        /// Gets the animation ID by name.
        /// </summary>
        /// <param name="animName">Name of the animation</param>
        /// <returns>Animation ID, or -1 if not found</returns>
        public int GetAnimationId(string animName)
        {
            if (NeedsToInitialize)
                UpdateAnimations();

            if (m_animationsById == null)
                return -1;

            return m_animationsById.GetValueOrDefault(animName, -1);
        }

        /// <summary>
        /// Gets animation data by index.
        /// </summary>
        /// <param name="idx">Index in the animation list</param>
        /// <returns>Animation data, or null if invalid index</returns>
        protected SpriteAnimationData GetAnimationData(int idx)
        {
            if (NeedsToInitialize)
                UpdateAnimations();

            if (list == null)
                return null;

            return list.Count > idx && list[idx].animation.Valid() ? list[idx].animation : null;
        }

        /// <summary>
        /// Checks if an animation with the given name exists.
        /// </summary>
        /// <param name="animName">Animation name to check</param>
        /// <returns>True if the animation exists</returns>
        public bool HasAnimation(string animName)
        {
            return !string.IsNullOrEmpty(animName) && m_animationsByName != null && m_animationsByName.ContainsKey(animName);
        }

        /// <summary>
        /// Sorts animations alphabetically by name.
        /// </summary>
        public void SortAnimations()
        {
            if (list != null && list.Count > 0)
            {
                list = list.OrderBy(x => x.animationName).ToList();
            }
        }

        #endregion

        #region Timing & Frames

        /// <summary>
        /// Gets the time duration of the current frame.
        /// </summary>
        /// <returns>Frame time in seconds</returns>
        public float GetCurrentFrameTime()
        {
            if (CurrentAnimation != null && CurrentAnimation.frameDatas != null &&
                CurrentAnimation.frameDatas.Count > CurrentFrame)
            {
                return m_currentAnimation.frameDatas[CurrentFrame].time;
            }
            return 0;
        }

        /// <summary>
        /// Gets the speed-adjusted delta time for animation updates.
        /// </summary>
        /// <returns>Delta time multiplied by speed ratio</returns>
        public float GetSpeedDelta()
        {
            float baseTime = mode == SpriteAnimationTimeMode.TIMESCALEINDEPENDENT
                ? Time.unscaledDeltaTime
                : Time.deltaTime;

            float multiplier = overrideTimeScale ? timeScaleOverride : speedRatio;
            return baseTime * multiplier;
        }

        /// <summary>
        /// Gets the total number of frames in the current animation.
        /// </summary>
        /// <returns>Frame count</returns>
        public int GetFrameCount()
        {
            return CurrentFrameCount;
        }

        /// <summary>
        /// Gets the total length of the current animation in seconds.
        /// </summary>
        /// <returns>Animation length in seconds</returns>
        public float GetCurrentAnimationLength()
        {
            float length = 0f;

            if (m_animationsByName != null && m_animationsByName.ContainsKey(CurrentAnimationName))
            {
                for (int i = 0; i < CurrentAnimation.frameDatas.Count; i++)
                {
                    length += CurrentAnimation.frameDatas[i].time;
                }
            }

            return length;
        }

        #endregion

        #region Internal Methods

        /// <summary>
        /// Internal update method that advances the animation.
        /// Called from Update() or EditorUpdate().
        /// </summary>
        /// <param name="deltaTime">Time since last update</param>
        private void OnUpdate(float deltaTime)
        {
            if (!playing || paused || m_singleFrame || CurrentAnimation == null)
                return;

            timer += deltaTime;

            if (CurrentFrame >= CurrentAnimation.frameDatas.Count ||
                timer < CurrentAnimation.frameDatas[CurrentFrame].time)
                return;

            timer = 0;
            CurrentFrame = m_reversed ? CurrentFrame - 1 : CurrentFrame + 1;

            // Check if animation completed
            if (CurrentFrame >= CurrentAnimation.frameDatas.Count || CurrentFrame < 0)
            {
                // Invoke completion callback
                if (m_animCompleteCallback != null)
                {
                    AnimationComplete?.Invoke();
                }

                // Handle looping
                switch (CurrentAnimation.loop)
                {
                    case SpriteAnimationLoopMode.NOLOOP:
                        Stop();
                        return;
                    case SpriteAnimationLoopMode.LOOPTOSTART:
                        CurrentFrame = m_reversed ? CurrentFrameCount - 1 : 0;
                        break;
                    case SpriteAnimationLoopMode.LOOPTOFRAME:
                        CurrentFrame = CurrentAnimation.frameToLoop;
                        break;
                }
            }

            SetCurrentFrame();

            // Invoke frame update callback
            if (m_animFrameUpdateCallback != null)
            {
                AnimationFrameUpdate?.Invoke(CurrentFrame);
            }
        }

        /// <summary>
        /// Starts playing the current animation.
        /// </summary>
        protected void PlayCurrentAnim()
        {
            timer = 0;
            playing = true;
            SetCurrentFrame();
        }

        /// <summary>
        /// Sets the sprite to the current frame.
        /// </summary>
        protected void SetCurrentFrame()
        {
            if (currentIdx >= CurrentAnimation.frameDatas.Count)
            {
                Debug.LogWarning(
                    $"[SpriteAnimation] Trying to show {CurrentAnimation.name} at frame {currentIdx} " +
                    $"but only {CurrentAnimation.frameDatas.Count} frame(s) exist on {(transform.parent == null ? name : transform.parent.name)}"
                );
                return;
            }

            Sprite sprite = CurrentAnimation.frameDatas[currentIdx].sprite;

            if (renderer != null)
            {
                renderer.sprite = sprite;
            }
            else if (image != null)
            {
                // Use overrideSprite for UI Image to work properly with sprite atlases
                image.overrideSprite = sprite;
                image.enabled = sprite != null;
            }
        }

        /// <summary>
        /// Removes all callbacks to prevent memory leaks.
        /// </summary>
        private void RemoveCallbacks()
        {
            if (m_animCompleteCallback != null)
            {
                AnimationComplete -= m_animCompleteCallback;
                m_animCompleteCallback = null;
            }

            if (m_animFrameUpdateCallback != null)
            {
                AnimationFrameUpdate -= m_animFrameUpdateCallback;
                m_animFrameUpdateCallback = null;
            }
        }

        #endregion

        #region Odin Callbacks

        private void OnAnimationAssetChanged()
        {
            UpdateAnimations();
#if UNITY_EDITOR
            // Reset selected animation if it doesn't exist in new asset
            if (!string.IsNullOrEmpty(m_selectedAnimation) && !HasAnimation(m_selectedAnimation))
            {
                m_selectedAnimation = string.Empty;
            }
#endif
        }

        #endregion
    }
}