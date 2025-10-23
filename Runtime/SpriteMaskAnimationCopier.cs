using UnityEngine;

namespace TakoBoyStudios.Animation
{
    /// <summary>
    /// Component that automatically copies the current sprite from a SpriteRenderer to a SpriteMask.
    /// Useful for creating animated sprite masks that follow a character's animation.
    /// </summary>
    /// <remarks>
    /// This is commonly used for:
    /// - Character shadows that match animation
    /// - Animated cutouts/reveals
    /// - Dynamic masking effects
    /// 
    /// Simply attach this to a GameObject with a SpriteMask component and assign the SpriteRenderer to copy from.
    /// The SpriteMask will automatically update every frame to match the source renderer's sprite.
    /// </remarks>
    [RequireComponent(typeof(SpriteMask))]
    public class SpriteMaskAnimationCopier : MonoBehaviour
    {
        #region Serialized Fields

        /// <summary>
        /// The SpriteRenderer to copy the sprite from.
        /// </summary>
        [Tooltip("SpriteRenderer to copy sprite from")]
        public SpriteRenderer m_renderer;

        #endregion

        #region Private Fields

        private SpriteMask m_spriteMask;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            m_spriteMask = GetComponent<SpriteMask>();

            if (m_spriteMask == null)
            {
                Debug.LogError("[SpriteMaskAnimationCopier] SpriteMask component not found!", gameObject);
            }

            if (m_renderer == null)
            {
                Debug.LogWarning("[SpriteMaskAnimationCopier] No SpriteRenderer assigned!", gameObject);
            }
        }

        private void Update()
        {
            if (m_renderer != null && m_spriteMask != null)
            {
                m_spriteMask.sprite = m_renderer.sprite;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Sets the source SpriteRenderer to copy from.
        /// </summary>
        /// <param name="renderer">The SpriteRenderer to copy</param>
        public void SetRenderer(SpriteRenderer renderer)
        {
            m_renderer = renderer;
        }

        /// <summary>
        /// Manually triggers a sprite copy from the source renderer.
        /// </summary>
        public void CopySprite()
        {
            if (m_renderer != null && m_spriteMask != null)
            {
                m_spriteMask.sprite = m_renderer.sprite;
            }
        }

        #endregion
    }
}
