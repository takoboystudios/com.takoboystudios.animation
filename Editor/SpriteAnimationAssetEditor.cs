using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;

namespace TakoBoyStudios.Animation.Editor
{
    class CustomDragData
    {
        public int originalIndex = -1;
        public int animationIdx = -1;
    }

    [CustomEditor(typeof(SpriteAnimationAsset))]
    public class SpriteAnimationAssetEditor : UnityEditor.Editor
    {
        const int FRAME_THUMB_SIZE = 40;
        const int FRAME_SPACING = 8;
        const int ANIMATIONS_PER_PAGE = 3;
        const string DRAG_DATA_KEY = "dragKeyframe";
        const float PREVIEW_SIZE = 80f;

        static Event currentEvent;
        static Vector2 mousePosition;
        bool isDraggingKeyframe = false;
        int currentPage;
        float _setFrameTimeValue = 0.04f;

        Dictionary<int, AnimationPreviewState> previewStates = new Dictionary<int, AnimationPreviewState>();
        Dictionary<int, bool> foldoutStates = new Dictionary<int, bool>();

        class AnimationPreviewState
        {
            public bool isPlaying = false;
            public int currentFrame = 0;
            public double lastUpdateTime = 0;
            public double elapsedTime = 0;
        }

        [MenuItem("GameObject/TakoBoy Studios/Animated Sprite", false, 10)]
        public static void CreateAnimatedSpriteGameObject()
        {
            GameObject newObj = new GameObject("Animated Sprite");
            newObj.AddComponent<SpriteRenderer>();
            newObj.AddComponent<SpriteAnimation>();
            Selection.activeGameObject = newObj;

            if (SceneView.lastActiveSceneView != null)
            {
                SceneView.lastActiveSceneView.FrameSelected();
            }
        }

        void OnEnable()
        {
            SpriteAnimationAsset animAsset = target as SpriteAnimationAsset;
            if (animAsset != null)
            {
                animAsset.SortAnimationsAlphabetically();
            }

            //EditorApplication.update += OnEditorUpdate;
        }

        void OnDisable()
        {
            //EditorApplication.update -= OnEditorUpdate;
        }

        // void OnEditorUpdate()
        // {
        //     if (target == null)
        //         return;
        //
        //     bool needsRepaint = false;
        //     double currentTime = EditorApplication.timeSinceStartup;
        //
        //     // Update all playing animations
        //     foreach (KeyValuePair<int, AnimationPreviewState> kvp in previewStates.ToList())
        //     {
        //         AnimationPreviewState previewState = kvp.Value;
        //         if (!previewState.isPlaying)
        //             continue;
        //
        //         int animIdx = kvp.Key;
        //         SpriteAnimationAsset animAsset = target as SpriteAnimationAsset;
        //         if (animAsset == null || animIdx >= animAsset.animations.Count)
        //             continue;
        //
        //         SpriteAnimationData animData = animAsset.animations[animIdx];
        //         if (animData.frameDatas == null || animData.frameDatas.Count == 0)
        //             continue;
        //
        //         // Calculate delta time
        //         double deltaTime = currentTime - previewState.lastUpdateTime;
        //         previewState.lastUpdateTime = currentTime;
        //
        //         // Clamp delta to avoid huge jumps
        //         deltaTime = System.Math.Min(deltaTime, 0.1);
        //
        //         previewState.elapsedTime += deltaTime;
        //
        //         // Get current frame timing
        //         float frameTime = animData.frameDatas[previewState.currentFrame].time / 1000f; // ms to seconds
        //         float speedRatio = animData.speedRatio;
        //
        //         if (speedRatio <= 0) speedRatio = 1f;
        //         if (frameTime <= 0) frameTime = 0.1f;
        //
        //         float adjustedFrameTime = frameTime / speedRatio;
        //
        //         // Advance frames with safety counter
        //         int safetyCounter = 0;
        //         while (previewState.elapsedTime >= adjustedFrameTime && safetyCounter < 100)
        //         {
        //             previewState.elapsedTime -= adjustedFrameTime;
        //             previewState.currentFrame = (previewState.currentFrame + 1) % animData.frameDatas.Count;
        //             safetyCounter++;
        //
        //             // Update timing for next frame
        //             frameTime = animData.frameDatas[previewState.currentFrame].time / 1000f;
        //             if (frameTime <= 0) frameTime = 0.1f;
        //             adjustedFrameTime = frameTime / speedRatio;
        //         }
        //
        //         needsRepaint = true;
        //     }
        //
        //     if (needsRepaint)
        //     {
        //         Repaint();
        //     }
        // }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            currentEvent = Event.current;
            if (currentEvent.isMouse)
            {
                mousePosition = currentEvent.mousePosition;
            }
            
            SirenixEditorGUI.BeginBox("Sprite Animation Asset");

            DrawToolbar();
            SirenixEditorGUI.IndentSpace();
            DrawAnimationList();
            HandleDragAndDrop();

            SirenixEditorGUI.EndBox();

            serializedObject.ApplyModifiedProperties();
        }

        void DrawToolbar()
        {
            SerializedProperty animationList = serializedObject.FindProperty("animations");

            SirenixEditorGUI.BeginHorizontalToolbar();

            if (GUILayout.Button("+ Add Animation", GUILayout.Width(120)))
            {
                AddNewAnimation(animationList);
                return;
            }

            GUILayout.FlexibleSpace();

            int totalAnims = animationList.arraySize;
            int pages = Mathf.CeilToInt((float)totalAnims / ANIMATIONS_PER_PAGE);
            if (currentPage >= pages) currentPage = Mathf.Max(0, pages - 1);

            GUI.enabled = currentPage > 0;
            if (GUILayout.Button("◄", GUILayout.Width(30))) currentPage--;
            GUI.enabled = true;

            GUILayout.Label($"Page {currentPage + 1} / {Mathf.Max(1, pages)}", GUILayout.Width(80));

            GUI.enabled = currentPage < pages - 1 && pages > 1;
            if (GUILayout.Button("►", GUILayout.Width(30))) currentPage++;
            GUI.enabled = true;

#if ODIN_INSPECTOR
            SirenixEditorGUI.EndHorizontalToolbar();
#else
            EditorGUILayout.EndHorizontal();
#endif
        }

        void AddNewAnimation(SerializedProperty animationList)
        {
            int idx = animationList.arraySize;
            animationList.InsertArrayElementAtIndex(idx);

            SerializedProperty newItem = animationList.GetArrayElementAtIndex(idx);
            newItem.FindPropertyRelative("name").stringValue = "new animation";
            newItem.FindPropertyRelative("speedRatio").floatValue = 1f;
            newItem.FindPropertyRelative("loop").enumValueIndex = 0;
            newItem.FindPropertyRelative("frameToLoop").intValue = 0;
            newItem.FindPropertyRelative("frameDatas").arraySize = 0;
            newItem.FindPropertyRelative("selectedIndex").intValue = -1;

            serializedObject.ApplyModifiedProperties();
        }

        void DrawAnimationList()
        {
            SerializedProperty animationList = serializedObject.FindProperty("animations");

            int startIndex = ANIMATIONS_PER_PAGE * currentPage;
            int endIndex = Mathf.Min(startIndex + ANIMATIONS_PER_PAGE, animationList.arraySize);

            for (int i = startIndex; i < endIndex; i++)
            {
                SerializedProperty animProp = animationList.GetArrayElementAtIndex(i);
                SpriteAnimationData animData = (target as SpriteAnimationAsset).animations[i];
                DrawAnimation(i, animProp, animData);
            }
        }

        void DrawProp(SerializedProperty prop, string propName, string propId)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(propName, GUILayout.Width(60));
            EditorGUILayout.PropertyField(prop.FindPropertyRelative(propId), GUIContent.none);
            GUILayout.EndHorizontal();
        }

        // Remove the Dictionary field and use these helper methods instead

        string GetFoldoutKey(int animIdx)
        {
            // Use the target's instance ID to make keys unique per asset
            return $"SpriteAnimAsset_{target.GetInstanceID()}_Foldout_{animIdx}";
        }

        bool GetFoldoutState(int animIdx)
        {
            return SessionState.GetBool(GetFoldoutKey(animIdx), false); // default true (expanded)
        }

        void SetFoldoutState(int animIdx, bool state)
        {
            SessionState.SetBool(GetFoldoutKey(animIdx), state);
        }

        void DrawAnimation(int animIdx, SerializedProperty animProp, SpriteAnimationData animData)
        {
            SerializedProperty nameProp = animProp.FindPropertyRelative("name");
    
            // Get saved foldout state
            bool isExpanded = GetFoldoutState(animIdx);
    
            // Draw the foldout group with styled header
            SirenixEditorGUI.BeginBox();
            isExpanded = SirenixEditorGUI.Foldout(isExpanded, $"Animation: {nameProp.stringValue}");
    
            // Save state if changed
            SetFoldoutState(animIdx, isExpanded);
    
            // Only draw contents if expanded
            if (isExpanded)
            {
                SirenixEditorGUI.BeginBox($"Settings");
        
                DrawProp(animProp, "Name", "name");
                DrawProp(animProp, "Loop Type", "loop");
        
                SerializedProperty loopProp = animProp.FindPropertyRelative("loop");
                if (loopProp.enumValueIndex == (int)SpriteAnimationLoopMode.LOOPTOFRAME)
                    DrawProp(animProp, "To Frame", "frameToLoop");
        
                SirenixEditorGUI.EndBox();

                // Layout
                SerializedProperty framesProp = animProp.FindPropertyRelative("frameDatas");

                DrawFrameTimeline(animIdx, animProp, framesProp);
            }
    
            SirenixEditorGUI.EndBox();
        }

        void DrawAnimationPreview(int animIdx, SerializedProperty animProp, SerializedProperty framesProp, SpriteAnimationData animData)
        {
            if (!previewStates.ContainsKey(animIdx))
            {
                previewStates[animIdx] = new AnimationPreviewState();
            }

            AnimationPreviewState previewState = previewStates[animIdx];

            SirenixEditorGUI.BeginBox("Preview");

            // Controls
            EditorGUILayout.BeginHorizontal();

            string playButtonLabel = previewState.isPlaying ? "⏸ Pause" : "▶ Play";
            if (GUILayout.Button(playButtonLabel, GUILayout.Height(30)))
            {
                previewState.isPlaying = !previewState.isPlaying;
                if (previewState.isPlaying)
                {
                    // Initialize timing when starting
                    previewState.lastUpdateTime = EditorApplication.timeSinceStartup;
                    previewState.elapsedTime = 0;
                }
            }

            if (GUILayout.Button("⏹ Stop", GUILayout.Height(30)))
            {
                previewState.isPlaying = false;
                previewState.currentFrame = 0;
                previewState.elapsedTime = 0;
            }

            EditorGUILayout.EndHorizontal();

            // Preview sprite
            if (framesProp.arraySize > 0)
            {
                int frameIndex = Mathf.Clamp(previewState.currentFrame, 0, framesProp.arraySize - 1);
                SerializedProperty frameProp = framesProp.GetArrayElementAtIndex(frameIndex);
                SerializedProperty spriteProp = frameProp.FindPropertyRelative("sprite");
                Sprite sprite = spriteProp.objectReferenceValue as Sprite;

                Rect previewRect = GUILayoutUtility.GetRect(PREVIEW_SIZE, PREVIEW_SIZE);

                // Background
                EditorGUI.DrawRect(previewRect, new Color(0.15f, 0.15f, 0.15f, 1f));

                if (sprite != null)
                {
                    Texture2D texture = sprite.texture;
                    Rect spriteRect = sprite.rect;
                    Rect texCoords = new Rect(
                        spriteRect.x / texture.width,
                        spriteRect.y / texture.height,
                        spriteRect.width / texture.width,
                        spriteRect.height / texture.height
                    );

                    // Fit with padding
                    float padding = 10f;
                    Rect paddedRect = new Rect(
                        previewRect.x + padding,
                        previewRect.y + padding,
                        previewRect.width - padding * 2,
                        previewRect.height - padding * 2
                    );

                    float spriteAspect = spriteRect.width / spriteRect.height;
                    float previewAspect = paddedRect.width / paddedRect.height;

                    Rect drawRect = paddedRect;
                    if (spriteAspect > previewAspect)
                    {
                        float height = paddedRect.width / spriteAspect;
                        drawRect.y += (paddedRect.height - height) / 2;
                        drawRect.height = height;
                    }
                    else
                    {
                        float width = paddedRect.height * spriteAspect;
                        drawRect.x += (paddedRect.width - width) / 2;
                        drawRect.width = width;
                    }

                    GUI.DrawTextureWithTexCoords(drawRect, texture, texCoords);
                }
                else
                {
                    EditorGUI.LabelField(previewRect, "No Sprite", EditorStyles.centeredGreyMiniLabel);
                }

                EditorGUILayout.LabelField($"Frame {frameIndex + 1} / {framesProp.arraySize}", EditorStyles.centeredGreyMiniLabel);
            }

            SirenixEditorGUI.EndBox();
        }

        void DrawFrameTimeline(int animIdx, SerializedProperty animProp, SerializedProperty framesProp)
        {
            SerializedProperty selectedIdxProp = animProp.FindPropertyRelative("selectedIndex");
            int selectedIdx = selectedIdxProp.intValue;

            if (selectedIdx >= framesProp.arraySize)
            {
                selectedIdx = framesProp.arraySize - 1;
                selectedIdxProp.intValue = selectedIdx;
            }

            SirenixEditorGUI.BeginBox("Frames");

            // Controls
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("+ Add Frame"))
            {
                framesProp.InsertArrayElementAtIndex(framesProp.arraySize);
                SerializedProperty newFrame = framesProp.GetArrayElementAtIndex(framesProp.arraySize - 1);
                newFrame.FindPropertyRelative("sprite").objectReferenceValue = null;
                newFrame.FindPropertyRelative("time").floatValue = 0.04f;
                selectedIdxProp.intValue = framesProp.arraySize - 1;
            }

            GUI.enabled = selectedIdx >= 0 && framesProp.arraySize > 0;
            if (GUILayout.Button("- Remove", GUILayout.Width(80)))
            {
                framesProp.DeleteArrayElementAtIndex(selectedIdx);
                selectedIdxProp.intValue = Mathf.Max(0, selectedIdx - 1);
            }
            GUI.enabled = true;

            GUILayout.FlexibleSpace();
            
            _setFrameTimeValue = EditorGUILayout.FloatField(GUIContent.none, _setFrameTimeValue, GUILayout.Width(40));
            if (GUILayout.Button("Set Frame Times", GUILayout.Width(140)))
            {
                for (int i = 0; i < framesProp.arraySize; i++)
                    framesProp.GetArrayElementAtIndex(i).FindPropertyRelative("time").floatValue = _setFrameTimeValue;
            }
            EditorGUILayout.EndHorizontal();

            if (framesProp.arraySize == 0)
            {
                SirenixEditorGUI.InfoMessageBox("No frames. Add frames to start animating!");
                SirenixEditorGUI.EndBox();
                return;
            }

            EditorGUILayout.Space(5);

            DrawFrameGrid(animIdx, framesProp, selectedIdxProp);

            EditorGUILayout.Space(10);

            if (selectedIdx >= 0 && selectedIdx < framesProp.arraySize)
            {
                DrawSelectedFrameDetails(framesProp.GetArrayElementAtIndex(selectedIdx));
            }
            
            SirenixEditorGUI.EndBox();
        }

        void DrawFrameGrid(int animIdx, SerializedProperty framesProp, SerializedProperty selectedIdxProp)
        {
            float availableWidth = EditorGUIUtility.currentViewWidth - 60;
            float timelineWidth = Mathf.Min(availableWidth * 0.6f, 400f);
            float gridWidth = timelineWidth - 40;

            int columns = Mathf.FloorToInt(gridWidth / (FRAME_THUMB_SIZE + FRAME_SPACING));
            columns = Mathf.Max(1, columns);

            int mouseOverIdx = -1;
            Rect currentRect = GUILayoutUtility.GetRect(gridWidth,
                Mathf.CeilToInt((float)framesProp.arraySize / columns) * (FRAME_THUMB_SIZE + FRAME_SPACING));

            float xPos = currentRect.x;
            float yPos = currentRect.y;

            for (int i = 0; i < framesProp.arraySize; i++)
            {
                SerializedProperty frameProp = framesProp.GetArrayElementAtIndex(i);
                bool isSelected = (i == selectedIdxProp.intValue);

                Rect thumbRect = new Rect(xPos, yPos, FRAME_THUMB_SIZE, FRAME_THUMB_SIZE);

                if (thumbRect.Contains(mousePosition))
                {
                    mouseOverIdx = i;
                }

                DrawFrameThumb(i, frameProp, thumbRect, isSelected, mouseOverIdx == i);

                xPos += FRAME_THUMB_SIZE + FRAME_SPACING;
                if ((i + 1) % columns == 0 && i < framesProp.arraySize - 1)
                {
                    xPos = currentRect.x;
                    yPos += FRAME_THUMB_SIZE + FRAME_SPACING;
                }
            }

            if (mouseOverIdx != -1)
            {
                HandleFrameInteraction(animIdx, framesProp, selectedIdxProp, mouseOverIdx);
            }
        }

        void DrawFrameThumb(int frameIdx, SerializedProperty frameProp, Rect rect, bool isSelected, bool isHovered)
        {
            SerializedProperty spriteProp = frameProp.FindPropertyRelative("sprite");
            Sprite sprite = spriteProp.objectReferenceValue as Sprite;

            Color borderColor = isSelected ? new Color(0.3f, 0.7f, 1f) : (isHovered ? Color.yellow : Color.gray);
            EditorGUI.DrawRect(rect, borderColor);

            Rect innerRect = new Rect(rect.x + 2, rect.y + 2, rect.width - 4, rect.height - 4);
            EditorGUI.DrawRect(innerRect, new Color(0.2f, 0.2f, 0.2f));

            if (sprite != null)
            {
                Texture2D texture = sprite.texture;
                Rect spriteRect = sprite.rect;
                Rect texCoords = new Rect(
                    spriteRect.x / texture.width,
                    spriteRect.y / texture.height,
                    spriteRect.width / texture.width,
                    spriteRect.height / texture.height
                );

                Rect previewRect = new Rect(innerRect.x + 2, innerRect.y + 2, innerRect.width - 4, innerRect.height - 4);
                GUI.DrawTextureWithTexCoords(previewRect, texture, texCoords);
            }

            GUIStyle labelStyle = new GUIStyle(EditorStyles.miniLabel);
            labelStyle.normal.textColor = Color.white;
            labelStyle.alignment = TextAnchor.UpperLeft;
            labelStyle.padding = new RectOffset(2, 2, 2, 2);
            GUI.Label(rect, frameIdx.ToString(), labelStyle);
        }

        void DrawSelectedFrameDetails(SerializedProperty frameProp)
        {
            SirenixEditorGUI.BeginBox("Selected Frame");

            GUILayout.BeginHorizontal();
            GUILayout.Label("Sprite", GUILayout.Width(60));
            EditorGUILayout.PropertyField(frameProp.FindPropertyRelative("sprite"), GUIContent.none);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Time (ms)", GUILayout.Width(60));
            EditorGUILayout.PropertyField(frameProp.FindPropertyRelative("time"), GUIContent.none);
            GUILayout.EndHorizontal();

            SirenixEditorGUI.EndBox();
        }

        void HandleFrameInteraction(int animIdx, SerializedProperty framesProp, SerializedProperty selectedIdxProp, int mouseOverIdx)
        {
            switch (currentEvent.type)
            {
                case EventType.MouseDown:
                    if (currentEvent.button == 0)
                    {
                        selectedIdxProp.intValue = mouseOverIdx;

                        DragAndDrop.PrepareStartDrag();
                        CustomDragData dragData = new CustomDragData
                        {
                            originalIndex = mouseOverIdx,
                            animationIdx = animIdx
                        };
                        DragAndDrop.SetGenericData(DRAG_DATA_KEY, dragData);

                        serializedObject.ApplyModifiedProperties();
                        currentEvent.Use();
                        Repaint();
                    }
                    break;

                case EventType.DragUpdated:
                    if (isDraggingKeyframe)
                    {
                        CustomDragData dragData = DragAndDrop.GetGenericData(DRAG_DATA_KEY) as CustomDragData;
                        if (dragData != null && dragData.animationIdx == animIdx && dragData.originalIndex != mouseOverIdx)
                        {
                            DragAndDrop.visualMode = DragAndDropVisualMode.Link;
                            currentEvent.Use();
                        }
                    }
                    break;

                case EventType.DragPerform:
                    if (isDraggingKeyframe)
                    {
                        DragAndDrop.AcceptDrag();
                        CustomDragData dragData = DragAndDrop.GetGenericData(DRAG_DATA_KEY) as CustomDragData;

                        if (dragData != null && dragData.animationIdx == animIdx)
                        {
                            SwapFrames(framesProp, dragData.originalIndex, mouseOverIdx);
                        }

                        isDraggingKeyframe = false;
                        currentEvent.Use();
                        DragAndDrop.PrepareStartDrag();
                        Repaint();
                    }
                    break;
            }
        }

        void SwapFrames(SerializedProperty framesProp, int fromIdx, int toIdx)
        {
            SerializedProperty fromFrame = framesProp.GetArrayElementAtIndex(fromIdx);
            SerializedProperty toFrame = framesProp.GetArrayElementAtIndex(toIdx);

            Object fromSprite = fromFrame.FindPropertyRelative("sprite").objectReferenceValue;
            float fromTime = fromFrame.FindPropertyRelative("time").floatValue;

            fromFrame.FindPropertyRelative("sprite").objectReferenceValue = toFrame.FindPropertyRelative("sprite").objectReferenceValue;
            fromFrame.FindPropertyRelative("time").floatValue = toFrame.FindPropertyRelative("time").floatValue;

            toFrame.FindPropertyRelative("sprite").objectReferenceValue = fromSprite;
            toFrame.FindPropertyRelative("time").floatValue = fromTime;

            serializedObject.ApplyModifiedProperties();
        }

        void HandleDragAndDrop()
        {
            switch (currentEvent.type)
            {
                case EventType.MouseDrag:
                    CustomDragData dragData = DragAndDrop.GetGenericData(DRAG_DATA_KEY) as CustomDragData;
                    if (dragData != null)
                    {
                        DragAndDrop.visualMode = DragAndDropVisualMode.Link;
                        isDraggingKeyframe = true;
                        DragAndDrop.StartDrag("Dragging Frame");
                        currentEvent.Use();
                    }
                    break;

                case EventType.MouseUp:
                    if (isDraggingKeyframe)
                    {
                        DragAndDrop.PrepareStartDrag();
                        isDraggingKeyframe = false;
                    }
                    break;

                case EventType.DragExited:
                    if (isDraggingKeyframe)
                    {
                        DragAndDrop.PrepareStartDrag();
                        isDraggingKeyframe = false;
                        currentEvent.Use();
                    }
                    break;
            }
        }
    }
}