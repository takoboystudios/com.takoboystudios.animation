using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

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
    public class SpriteAnimation : MonoBehaviour
    {
        #region Serialized Fields

        [Tooltip("Current animation asset containing all animations")]
        public SpriteAnimationAsset animationAsset;

        [Tooltip("Time mode for animation playback")]
        public SpriteAnimationTimeMode mode;

        [Tooltip("Speed multiplier for animation playback (1.0 = normal speed)")]
        public float speedRatio = 1f;

        [Tooltip("Frame index to start playing from when enabled")]
        public int playFrom;

        [Tooltip("Override Unity's Time.timeScale with custom value")]
        public bool overrideTimeScale;

        [Tooltip("Custom time scale value when overrideTimeScale is true")]
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

        private void OnEnable()
        {
            if (NeedsToInitialize)
            {
                UpdateAnimations();
            }

            if (playing || animIdx < 0 || list == null || list.Count <= 0)
                return;

            if (animIdx >= list.Count || animIdx < 0)
            {
                animIdx = list.Count - 1;
            }

            Play(list[animIdx].animationName, playFrom);
        }

        private void OnDisable()
        {
            playing = false;
        }

        private void Update()
        {
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
        /// Plays an animation by name with a frame update callback.
        /// </summary>
        /// <param name="animName">Name of the animation to play</param>
        /// <param name="callback">Callback invoked on each frame change (passes frame index)</param>
        public void Play(string animName, Action<int> callback)
        {
            Play(animName);
            m_animFrameUpdateCallback = callback;
            AnimationFrameUpdate += m_animFrameUpdateCallback;
        }

        /// <summary>
        /// Plays an animation by name.
        /// </summary>
        /// <param name="animName">Name of the animation to play</param>
        /// <param name="startFrame">Frame index to start from (default: 0)</param>
        /// <param name="showDebug">Whether to log debug warnings (default: false)</param>
        /// <param name="setActive">Activate GameObject if inactive (default: true)</param>
        /// <param name="reverse">Play animation in reverse (default: false)</param>
        /// <returns>True if animation started successfully, false otherwise</returns>
        public bool Play(string animName, int startFrame = 0, bool showDebug = false, bool setActive = true, bool reverse = false)
        {
            if (!this)
                return false;

            if (gameObject == null)
            {
                if (showDebug) Debug.LogWarning("[SpriteAnimation] GameObject is null");
                return false;
            }

            if (string.IsNullOrEmpty(animName))
            {
                if (showDebug) Debug.LogWarning($"[SpriteAnimation] Animation name is not valid: '{animName}'");
                return false;
            }

            if (!gameObject.activeSelf && setActive)
            {
                gameObject.SetActive(true);
            }

            if (NeedsToInitialize)
            {
                UpdateAnimations();
            }

            RemoveCallbacks();
            SetReverse(reverse);
            startFrame = m_reversed ? int.MaxValue : startFrame;
            SetCurrentAnimation(animName, false, startFrame);

            if (CurrentAnimation != null && CurrentAnimation.Valid())
            {
                PlayCurrentAnim();
                return true;
            }

            if (showDebug)
            {
                Debug.LogWarning($"[SpriteAnimation] Current animation is null or not valid: {name} - {animName}");
            }

            return false;
        }

        /// <summary>
        /// Pauses or resumes the current animation.
        /// </summary>
        /// <param name="pause">True to pause, false to resume</param>
        public void Pause(bool pause)
        {
            paused = pause;
        }

        /// <summary>
        /// Stops the current animation and removes callbacks.
        /// </summary>
        public void Stop()
        {
            playing = false;
            RemoveCallbacks();
        }

        /// <summary>
        /// Sets whether the animation plays in reverse.
        /// </summary>
        /// <param name="reverse">True for reverse playback</param>
        public void SetReverse(bool reverse)
        {
            m_reversed = reverse;
        }

        /// <summary>
        /// Returns true if the animation is playing in reverse.
        /// </summary>
        public bool IsReversed()
        {
            return m_reversed;
        }

        /// <summary>
        /// Checks if a specific animation ID is currently playing.
        /// </summary>
        /// <param name="animId">Animation ID to check</param>
        /// <returns>True if the specified animation is playing</returns>
        public bool IsPlaying(int animId)
        {
            return animIdx == animId;
        }

        #endregion

        #region Animation Data Management

        /// <summary>
        /// Updates the internal animation dictionaries from the animation asset.
        /// Call this after changing the animation asset or its contents.
        /// </summary>
        public void UpdateAnimations()
        {
            m_animationsByName ??= new Dictionary<string, SpriteAnimationData>();
            m_animationsById ??= new Dictionary<string, int>();
            list ??= new List<AnimationListItem>();

            m_animationsByName.Clear();
            m_animationsById.Clear();
            list.Clear();

            if (animationAsset == null || animationAsset.animations == null || animationAsset.animations.Count <= 0)
                return;

            for (int i = 0; i < animationAsset.animations.Count; i++)
            {
                SpriteAnimationData data = animationAsset.animations[i];
                m_animationsByName[data.name] = data;
                m_animationsById.TryAdd(data.name, i);

                AnimationListItem item = new AnimationListItem
                {
                    animation = data,
                    animationName = data.name,
                    file = animationAsset
                };
                list.Add(item);
            }
        }

        /// <summary>
        /// Sets the current animation by index.
        /// </summary>
        /// <param name="idx">Index of the animation in the list</param>
        public void SetCurrentAnimation(int idx)
        {
            animIdx = idx;
            currentIdx = 0;
        }

        /// <summary>
        /// Sets the current animation by name.
        /// </summary>
        /// <param name="animName">Name of the animation</param>
        /// <param name="editor">Whether this is being called from the editor</param>
        /// <param name="frame">Starting frame index</param>
        public void SetCurrentAnimation(string animName, bool editor, int frame = 0)
        {
            if (editor)
                UpdateAnimations();

            m_currentAnimation = GetAnimationData(animName);

            if (m_currentAnimation == null)
                return;

            animIdx = m_animationsById[animName];
            currentIdx = Mathf.Clamp(frame, 0, m_currentAnimation.frameDatas.Count - 1);
            m_singleFrame = m_currentAnimation.frameDatas.Count <= 1;
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

        #region Editor Support

        /// <summary>
        /// Editor-only update method for animation preview in edit mode.
        /// </summary>
        /// <param name="deltaTime">Time since last editor update</param>
        public void EditorUpdate(float deltaTime)
        {
            OnUpdate(deltaTime);
        }

        #endregion
    }
}
