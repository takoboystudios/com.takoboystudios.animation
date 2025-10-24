using System;
using UnityEngine;

namespace TakoBoyStudios.Animation
{
    /// <summary>
    /// Component that automatically destroys or returns a GameObject to a pool after its animation completes.
    /// Automatically detects if the object came from a pool and handles cleanup appropriately.
    /// </summary>
    /// <remarks>
    /// This component requires a SpriteAnimation component on the same GameObject.
    /// 
    /// Usage:
    /// 1. Attach to a GameObject with SpriteAnimation
    /// 2. Set the animation name to play
    /// 3. Call Play() to start the animation
    /// 4. Object will be automatically destroyed/pooled when animation completes
    /// 
    /// The component automatically detects if the GameObject came from PoolManager and will
    /// return it to the pool instead of destroying it. No manual configuration needed!
    /// </remarks>
    [RequireComponent(typeof(SpriteAnimation))]
    public class DestroyAfterAnimation : MonoBehaviour
    {
        #region Serialized Fields

        /// <summary>
        /// Name of the animation to play when Play() is called.
        /// </summary>
        [Tooltip("Name of the animation to play")]
        public string m_animation;

        public enum DestroyBehaviour { Destroy, Disable }

        public DestroyBehaviour destroyBehaviour;

        #endregion

        #region Private Fields

        private SpriteAnimation m_animator;

        #endregion

        #region Events

        /// <summary>
        /// Event invoked when the animation completes, before the GameObject is destroyed/pooled.
        /// Passes the GameObject that is about to be cleaned up.
        /// </summary>
        public event Action<GameObject> OnAnimationComplete;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the SpriteAnimation component (cached).
        /// </summary>
        public SpriteAnimation animator
        {
            get
            {
                if (m_animator == null)
                    m_animator = GetComponent<SpriteAnimation>();
                return m_animator;
            }
        }

        #endregion

        #region Unity Lifecycle

        /// <summary>
        /// Called when the component starts.
        /// Automatically updates animations and plays if not using pooling.
        /// </summary>
        public void Start()
        {
            animator.UpdateAnimations();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Plays the configured animation and destroys/pools the GameObject when it completes.
        /// </summary>
        public void Play()
        {
            if (string.IsNullOrEmpty(m_animation))
            {
                Debug.LogWarning("[DestroyAfterAnimation] No animation name set!", gameObject);
                return;
            }

            animator.Play(m_animation, AnimationComplete);
        }

        /// <summary>
        /// Plays a specific animation and destroys/pools the GameObject when it completes.
        /// </summary>
        /// <param name="animName">Name of the animation to play</param>
        public void Play(string animName)
        {
            if (string.IsNullOrEmpty(animName))
            {
                Debug.LogWarning("[DestroyAfterAnimation] Animation name is null or empty!", gameObject);
                return;
            }

            animator.Play(animName, AnimationComplete);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Called when the animation completes. Handles cleanup by either returning to pool or destroying.
        /// </summary>
        private void AnimationComplete()
        {
            // Invoke event before cleanup
            OnAnimationComplete?.Invoke(gameObject);

            if(destroyBehaviour == DestroyBehaviour.Disable)
                gameObject.SetActive(false);
            else if (destroyBehaviour == DestroyBehaviour.Destroy)
                Destroy(gameObject);
        }

        #endregion
    }
}
