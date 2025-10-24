using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace TakoBoyStudios.Animation
{
    // =================================================================================================================
    // EDITOR ONLY ANIMATION PREVIEW
    // =================================================================================================================
    public partial class SpriteAnimation
    {
#if UNITY_EDITOR
        [SerializeField]
        [BoxGroup("Animation Preview")]
        [HorizontalGroup("Animation Preview/h1"), HideLabel]
        [ValueDropdown("GetAnimationDropdown")]
        [OnValueChanged("OnSelectedAnimationChanged")]
        private string m_selectedAnimation;

        private bool m_playingEditorAnimation;
        private DateTime m_lastUpdateTime;
        private float m_simulatedDeltaTime;

        // =================================================================================================================
        // UPDATES FOR THE EDITOR
        // =================================================================================================================

        private void EditorUpdate()
        {
            m_simulatedDeltaTime = (float)(DateTime.Now - m_lastUpdateTime).TotalSeconds;
            m_lastUpdateTime = DateTime.Now;
            EditorUpdateAnimation(m_simulatedDeltaTime);
        }

        private void EditorUpdateAnimation(float deltaTime)
        {
            if (m_playingEditorAnimation && !string.IsNullOrEmpty(m_selectedAnimation))
            {
                if (NeedsToInitialize)
                    UpdateAnimations();

                if (IsDone && !Play(m_selectedAnimation))
                {
                    if (animationAsset != null && animationAsset.animations.Count > 0)
                        m_selectedAnimation = animationAsset.animations[0].name;
                    return;
                }

                OnUpdate(deltaTime);
            }
        }

        // =================================================================================================================
        // INSPECTOR EDITOR BUTTONS
        // =================================================================================================================

        [GUIColor("GetButtonColor")]
        [BoxGroup("Animation Preview")]
        [Button("", Icon = SdfIconType.CaretRightFill)]
        [HorizontalGroup("Animation Preview/h1", Width = 20)]
        private void PlayButton()
        {
            m_playingEditorAnimation = !m_playingEditorAnimation;
        }

        // =================================================================================================================
        // ON PROPERTY CHANGED CALLBACKS
        // =================================================================================================================

        private void OnSelectedAnimationChanged()
        {
            if (NeedsToInitialize)
                UpdateAnimations();

            if (string.IsNullOrEmpty(m_selectedAnimation) || !HasAnimation(m_selectedAnimation))
                return;

            UpdateAnimations();
            Play(m_selectedAnimation);
        }

        // =================================================================================================================
        // VALUE GETTERS
        // =================================================================================================================

        private Color GetButtonColor()
        {
            return m_playingEditorAnimation ? Color.green : Color.white;
        }

        private IEnumerable<ValueDropdownItem<string>> GetAnimationDropdown()
        {
            if (animationAsset == null || animationAsset.animations == null)
            {
                yield return new ValueDropdownItem<string>();
                yield break;
            }

            yield return new ValueDropdownItem<string>("[none]", string.Empty);

            for (int i = 0; i < animationAsset.animations.Count; i++)
            {
                yield return new ValueDropdownItem<string>(
                    animationAsset.animations[i].name,
                    animationAsset.animations[i].name
                );
            }
        }

        // =================================================================================================================
        // UNITY CALLBACKS
        // =================================================================================================================

        private void OnEnable()
        {
            UnityEditor.EditorApplication.update += EditorUpdate;
        }

        private void OnDisable()
        {
            m_playingEditorAnimation = false;
            UnityEditor.EditorApplication.update -= EditorUpdate;
        }

#endif
    }
}