using System;
using UnityEngine;

namespace TakoBoyStudios.Animation
{
    [RequireComponent(typeof(SpriteAnimation))]
    public class DestroyAfterAnimation : MonoBehaviour
    {
        public enum DestroyBehaviour { Destroy, Disable }
        
        #region Serialized Fields
        
        [Tooltip("Name of the animation to play")]
        [SerializeField]
        [InspectorName("animation")]
        string animationToPlay;

        [Tooltip("The type of stop behaviour, either disabled or destroyed.")]
        [SerializeField]
        DestroyBehaviour destroyBehaviour;

        #endregion

        #region Private Fields

        SpriteAnimation _animator;

        #endregion

        #region Events

        /// <summary>
        /// Event invoked when the animation completes, before the GameObject is destroyed/pooled.
        /// Passes the GameObject that is about to be cleaned up.
        /// </summary>
        public event Action<GameObject> OnFinished;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the SpriteAnimation component (cached).
        /// </summary>
        public SpriteAnimation Animator
        {
            get
            {
                if (_animator == null)
                    _animator = GetComponent<SpriteAnimation>();
                return _animator;
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
            Animator.OnAnimationComplete += AnimationComplete;
            Animator.UpdateAnimations();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Plays the configured animation and destroys/pools the GameObject when it completes.
        /// </summary>
        public void Play()
        {
            if (string.IsNullOrEmpty(animationToPlay))
            {
                Debug.LogWarning("[DestroyAfterAnimation] No animation name set!", gameObject);
                return;
            }

            Animator.Play(animationToPlay);
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

            Animator.Play(animName);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Called when the animation completes. Handles cleanup by either returning to pool or destroying.
        /// </summary>
        void AnimationComplete()
        {
            RemoveCallbacks();
            
            // Invoke event before cleanup
            OnFinished?.Invoke(gameObject);

            if(destroyBehaviour == DestroyBehaviour.Disable)
                gameObject.SetActive(false);
            else if (destroyBehaviour == DestroyBehaviour.Destroy)
                Destroy(gameObject);
        }

        void RemoveCallbacks()
        {
            Animator.OnAnimationComplete -= AnimationComplete;
        }

        void OnValidate()
        {
            if (_animator == null)
                _animator = GetComponent<SpriteAnimation>();
        }

        void OnDestroy() => RemoveCallbacks();

        #endregion
    }
}
