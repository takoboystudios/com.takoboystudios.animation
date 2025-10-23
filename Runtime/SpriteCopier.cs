using UnityEngine;
using UnityEngine.UI;

namespace TakoBoyStudios.Animation
{
    /// <summary>
    /// Component that automatically copies sprites from a source SpriteRenderer or Image to a target SpriteRenderer or Image.
    /// Useful for synchronizing sprites across multiple renderers (e.g., shadows, reflections, UI mirrors).
    /// </summary>
    /// <remarks>
    /// This component can copy between:
    /// - SpriteRenderer → SpriteRenderer
    /// - Image → Image
    /// - Cross-component copying (SpriteRenderer ↔ Image)
    /// 
    /// The sprite is copied every frame in Update(), ensuring animations stay synchronized.
    /// </remarks>
    public class SpriteCopier : MonoBehaviour
    {
        #region Serialized Fields

        /// <summary>
        /// If true, copies from a SpriteRenderer. If false, copies from a UI Image.
        /// </summary>
        [Tooltip("If true, copy from SpriteRenderer. If false, copy from Image.")]
        public bool isSpriteRenderer = true;

        /// <summary>
        /// The SpriteRenderer to copy from (only used if isSpriteRenderer is true).
        /// </summary>
        [Tooltip("SpriteRenderer to copy from")]
#if ODIN_INSPECTOR
        [Sirenix.OdinInspector.ShowIf("isSpriteRenderer")]
#endif
        public SpriteRenderer m_rendererToCopy;

        /// <summary>
        /// The Image to copy from (only used if isSpriteRenderer is false).
        /// </summary>
        [Tooltip("Image to copy from")]
#if ODIN_INSPECTOR
        [Sirenix.OdinInspector.HideIf("isSpriteRenderer")]
#endif
        public Image m_ImageToCopy;

        #endregion

        #region Private Fields

        private SpriteRenderer m_renderer;
        private Image m_image;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the target SpriteRenderer on this GameObject (cached).
        /// </summary>
        public SpriteRenderer spriteRenderer
        {
            get
            {
                if (m_renderer == null)
                {
                    m_renderer = GetComponent<SpriteRenderer>();
                }
                return m_renderer;
            }
        }

        /// <summary>
        /// Gets the target Image on this GameObject (cached).
        /// </summary>
        public Image image
        {
            get
            {
                if (m_image == null)
                {
                    m_image = GetComponent<Image>();
                }
                return m_image;
            }
        }

        /// <summary>
        /// Gets the sprite from the source (either SpriteRenderer or Image).
        /// </summary>
        public Sprite spriteToCopy
        {
            get
            {
                if (m_rendererToCopy != null)
                    return m_rendererToCopy.sprite;

                if (m_ImageToCopy != null)
                    return m_ImageToCopy.sprite;

                return null;
            }
        }

        /// <summary>
        /// Gets or sets the sprite on the target (either SpriteRenderer or Image).
        /// </summary>
        public Sprite sprite
        {
            get
            {
                if (m_renderer != null)
                    return m_renderer.sprite;

                if (m_image != null)
                    return m_image.sprite;

                return null;
            }
            set
            {
                if (m_renderer != null)
                {
                    m_renderer.sprite = value;
                }
                else if (m_image != null)
                {
                    m_image.sprite = value;
                }
            }
        }

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            m_renderer = GetComponent<SpriteRenderer>();
            m_image = GetComponent<Image>();

            // Validate setup
            if (m_renderer == null && m_image == null)
            {
                Debug.LogWarning(
                    "[SpriteCopier] No SpriteRenderer or Image component found on this GameObject! " +
                    "Add one to enable sprite copying.",
                    gameObject
                );
            }

            if (isSpriteRenderer && m_rendererToCopy == null)
            {
                Debug.LogWarning("[SpriteCopier] isSpriteRenderer is true but m_rendererToCopy is not assigned!", gameObject);
            }

            if (!isSpriteRenderer && m_ImageToCopy == null)
            {
                Debug.LogWarning("[SpriteCopier] isSpriteRenderer is false but m_ImageToCopy is not assigned!", gameObject);
            }
        }

        public void Update()
        {
            Sprite source = spriteToCopy;
            Sprite current = sprite;

            // Only update if sprite changed
            if (source != null && source != current)
            {
                sprite = source;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Sets the source to copy from (automatically detects type).
        /// </summary>
        /// <param name="renderer">SpriteRenderer to copy from</param>
        public void SetSource(SpriteRenderer renderer)
        {
            isSpriteRenderer = true;
            m_rendererToCopy = renderer;
            m_ImageToCopy = null;
        }

        /// <summary>
        /// Sets the source to copy from (automatically detects type).
        /// </summary>
        /// <param name="image">Image to copy from</param>
        public void SetSource(Image image)
        {
            isSpriteRenderer = false;
            m_ImageToCopy = image;
            m_rendererToCopy = null;
        }

        /// <summary>
        /// Manually triggers a sprite copy from the source.
        /// </summary>
        public void CopySprite()
        {
            Sprite source = spriteToCopy;
            if (source != null)
            {
                sprite = source;
            }
        }

        #endregion
    }
}
