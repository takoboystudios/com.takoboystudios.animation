using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace TakoBoyStudios.Animation.Editor
{
    /// <summary>
    /// Custom inspector editor for SpriteAnimation component with built-in animation preview.
    /// </summary>
    [CustomEditor(typeof(SpriteAnimation))]
    public class SpriteAnimationEditor : UnityEditor.Editor
    {
        #region Private Fields

        private SpriteAnimation m_target;
        private List<string> m_animationNames = new List<string>();
        private bool m_cachedAnimationNames = false;
        private int m_selectedAnimationIdx;
        private bool m_changedAnim = false;
        private int m_changedAnimIdx = -1;

        // Preview state
        private bool m_isPreviewing = false;
        private double m_lastEditorTime;
        private float m_previewSpeed = 1f;
        private bool m_previewLoop = true;

        #endregion

        #region Unity Editor Callbacks

        private void OnEnable()
        {
            m_target = target as SpriteAnimation;
            m_lastEditorTime = EditorApplication.timeSinceStartup;
            EditorApplication.update += OnEditorUpdate;
        }

        private void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
            StopPreview();
        }

        public override void OnInspectorGUI()
        {
            m_target = target as SpriteAnimation;
            m_target.SortAnimations();

            var serializedObject = new SerializedObject(m_target);
            serializedObject.Update();

            CacheAnimationNames();

            // Draw properties
            DrawProperties(serializedObject);

            // Draw animation selector
            DrawAnimationSelector(serializedObject);

            // Draw preview controls
            DrawPreviewControls();

            // Draw animation list
            DrawAnimationList(serializedObject);

            // Handle deferred animation change
            if (Event.current.type == EventType.Repaint && m_changedAnim)
            {
                m_changedAnim = false;
                m_target.SetCurrentAnimation(m_changedAnimIdx - 1);
            }

            if (GUI.changed)
                serializedObject.ApplyModifiedProperties();
        }

        #endregion

        #region Drawing Methods

        private void DrawProperties(SerializedObject serializedObject)
        {
            var spSpeedRatio = serializedObject.FindProperty("speedRatio");
            var spMode = serializedObject.FindProperty("mode");
            var spAssets = serializedObject.FindProperty("animationAsset");

            EditorGUILayout.PropertyField(spSpeedRatio, new GUIContent("Speed Ratio"));
            EditorGUILayout.PropertyField(spMode, new GUIContent("Time Mode"));

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(spAssets, new GUIContent("Animation Asset"), true);
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                m_cachedAnimationNames = false;
                CacheAnimationNames();

                int newIdx = GetSelectedAnimationIdx();
                if (newIdx == -1)
                {
                    SetCurrentAnimation(string.Empty);
                }
            }
        }

        private void DrawAnimationSelector(SerializedObject serializedObject)
        {
            m_selectedAnimationIdx = m_target.CurrentAnimationIdx + 1;

            int lastAnimIdx = m_selectedAnimationIdx;
            m_selectedAnimationIdx = EditorGUILayout.Popup(
                "Selected Animation",
                m_selectedAnimationIdx,
                m_animationNames.ToArray()
            );

            if (m_selectedAnimationIdx != lastAnimIdx)
            {
                serializedObject.ApplyModifiedProperties();
                m_changedAnim = true;
                m_changedAnimIdx = m_selectedAnimationIdx;
                
                // Stop preview when changing animation
                StopPreview();
            }

            if (m_target.CurrentAnimationName != null)
            {
                EditorGUILayout.PropertyField(
                    serializedObject.FindProperty("playFrom"),
                    new GUIContent("Play from Frame")
                );
            }
        }

        private void DrawPreviewControls()
        {
            if (m_target.CurrentAnimationName == null || string.IsNullOrEmpty(m_target.CurrentAnimationName))
                return;

            GUILayout.Space(10);
            EditorGUILayout.LabelField("Animation Preview", EditorStyles.boldLabel);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // Preview info
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Animation: {m_target.CurrentAnimationName}", EditorStyles.miniLabel);
            EditorGUILayout.LabelField(
                $"Frame: {m_target.CurrentFrame + 1} / {m_target.GetFrameCount()}",
                EditorStyles.miniLabel,
                GUILayout.Width(100)
            );
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Length: {m_target.GetCurrentAnimationLength():F2}s", EditorStyles.miniLabel);
            EditorGUILayout.LabelField(
                $"Time: {m_target.timer:F2}s",
                EditorStyles.miniLabel,
                GUILayout.Width(100)
            );
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(5);

            // Play/Pause/Stop buttons
            EditorGUILayout.BeginHorizontal();

            if (!m_isPreviewing)
            {
                if (GUILayout.Button("▶ Play", GUILayout.Height(30)))
                {
                    StartPreview();
                }
            }
            else
            {
                if (GUILayout.Button(m_target.paused ? "▶ Resume" : "⏸ Pause", GUILayout.Height(30)))
                {
                    m_target.Pause(!m_target.paused);
                }
            }

            if (GUILayout.Button("⏹ Stop", GUILayout.Height(30)))
            {
                StopPreview();
            }

            EditorGUILayout.EndHorizontal();

            GUILayout.Space(5);

            // Frame scrubber
            EditorGUI.BeginChangeCheck();
            int newFrame = EditorGUILayout.IntSlider(
                "Frame",
                m_target.CurrentFrame,
                0,
                Mathf.Max(0, m_target.GetFrameCount() - 1)
            );
            if (EditorGUI.EndChangeCheck())
            {
                m_target.CurrentFrame = newFrame;
                m_target.timer = 0;
                if (!m_isPreviewing)
                {
                    // Manual scrubbing - show the frame
                    StartPreview();
                    m_target.Pause(true);
                }
                Repaint();
            }

            // Speed control
            m_previewSpeed = EditorGUILayout.Slider("Preview Speed", m_previewSpeed, 0.1f, 3f);

            // Loop toggle
            m_previewLoop = EditorGUILayout.Toggle("Loop Preview", m_previewLoop);

            EditorGUILayout.EndVertical();
        }

        private void DrawAnimationList(SerializedObject serializedObject)
        {
            GUILayout.Space(20);
            SerializedProperty animList = serializedObject.FindProperty("list");

            if (animList.arraySize <= 0)
                return;

            float nameSize = 140;
            GUILayoutOption[] options = new GUILayoutOption[2];
            options[0] = GUILayout.MaxWidth(265);
            options[1] = GUILayout.MinWidth(265);

            // Header
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            Rect itemRect = GUILayoutUtility.GetRect(200, 18, options);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUI.Box(itemRect, string.Empty);
            itemRect.width = nameSize;
            EditorGUI.LabelField(itemRect, "Animation Name", EditorStyles.boldLabel);
            itemRect.xMin = itemRect.xMax;
            itemRect.width = 45;
            EditorGUI.LabelField(itemRect, "Index", EditorStyles.boldLabel);

            // List items (limit to 20 for performance)
            var count = Mathf.Clamp(animList.arraySize, 0, 20);

            for (int i = 0; i < count; i++)
            {
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                itemRect = GUILayoutUtility.GetRect(200, 18, options);
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();

                itemRect.width = nameSize;
                GUI.Box(itemRect, string.Empty);

                SerializedProperty item = animList.GetArrayElementAtIndex(i);
                EditorGUI.LabelField(itemRect, item.FindPropertyRelative("animationName").stringValue);

                itemRect.xMin = itemRect.xMax;
                itemRect.width = 45;
                GUI.Box(itemRect, string.Empty);
                EditorGUI.LabelField(itemRect, i.ToString());

                // Move up button
                itemRect.xMin = itemRect.xMax;
                itemRect.width = 30;
                if (i > 0 && GUI.Button(itemRect, "↑"))
                {
                    animList.MoveArrayElement(i, i - 1);
                    if (m_target.CurrentAnimationIdx == i)
                        serializedObject.FindProperty("animIdx").intValue--;
                    else if (m_target.CurrentAnimationIdx == i - 1)
                        serializedObject.FindProperty("animIdx").intValue++;
                    serializedObject.ApplyModifiedProperties();
                    return;
                }

                // Move down button
                itemRect.xMin = itemRect.xMax;
                itemRect.width = 30;
                if (i < animList.arraySize - 1 && GUI.Button(itemRect, "↓"))
                {
                    animList.MoveArrayElement(i, i + 1);
                    if (m_target.CurrentAnimationIdx == i)
                        serializedObject.FindProperty("animIdx").intValue++;
                    else if (m_target.CurrentAnimationIdx == i + 1)
                        serializedObject.FindProperty("animIdx").intValue--;
                    serializedObject.ApplyModifiedProperties();
                    return;
                }
            }

            if (animList.arraySize > 20)
            {
                EditorGUILayout.HelpBox(
                    $"Showing first 20 of {animList.arraySize} animations.",
                    MessageType.Info
                );
            }
        }

        #endregion

        #region Preview Methods

        private void StartPreview()
        {
            if (m_target.CurrentAnimationName == null)
                return;

            m_isPreviewing = true;
            m_lastEditorTime = EditorApplication.timeSinceStartup;

            // Initialize the animation for preview
            m_target.SetCurrentAnimation(m_target.CurrentAnimationName, true, m_target.CurrentFrame);
            m_target.playing = true;
            m_target.paused = false;

            Repaint();
        }

        private void StopPreview()
        {
            m_isPreviewing = false;
            if (m_target != null)
            {
                m_target.playing = false;
                m_target.paused = false;
                m_target.CurrentFrame = 0;
                m_target.timer = 0;
            }
            Repaint();
        }

        private void OnEditorUpdate()
        {
            if (!m_isPreviewing || m_target == null)
                return;

            double currentTime = EditorApplication.timeSinceStartup;
            float deltaTime = (float)(currentTime - m_lastEditorTime) * m_previewSpeed;
            m_lastEditorTime = currentTime;

            // Update animation
            m_target.EditorUpdate(deltaTime);

            // Handle preview loop
            if (!m_previewLoop && m_target.IsDone)
            {
                StopPreview();
            }

            // Force repaint to show animation
            Repaint();
        }

        #endregion

        #region Helper Methods

        private void SetCurrentAnimation(string animName)
        {
            m_target.SetCurrentAnimation(animName, true);
        }

        private int GetSelectedAnimationIdx()
        {
            int i = 1;
            for (; i < m_animationNames.Count; i++)
            {
                if (m_animationNames[i] == m_target.CurrentAnimationName)
                {
                    return i;
                }
            }
            return 0;
        }

        private void CacheAnimationNames()
        {
            if (m_cachedAnimationNames)
                return;

            m_target.UpdateAnimations();
            m_animationNames.Clear();
            m_animationNames.Add("Not Set");

            if (m_target.animationAsset != null &&
                m_target.animationAsset.animations != null &&
                m_target.animationAsset.animations.Count > 0)
            {
                foreach (SpriteAnimationData data in m_target.animationAsset.animations)
                {
                    m_animationNames.Add(data.name);
                }
            }

            m_selectedAnimationIdx = GetSelectedAnimationIdx();
            m_cachedAnimationNames = true;
        }

        #endregion
    }
}
