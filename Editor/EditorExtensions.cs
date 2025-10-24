using UnityEngine;
using UnityEditor;

namespace TakoBoyStudios.Animation.Editor
{
    /// <summary>
    /// Editor utility extensions for drag and drop operations.
    /// </summary>
    public static class EditorExtensions
    {
        public static GUIStyle headerStyle
        {
            get
            {
                GUIStyle style = new GUIStyle(EditorStyles.largeLabel);
                style.fontStyle = FontStyle.Bold;
                style.fontSize = 14;
                style.normal.textColor = EditorGUIUtility.isProSkin ? new Color(0.7f, 0.7f, 0.7f, 1f) : new Color(0.4f, 0.4f, 0.4f, 1f);
                return style;
            }
        }

        public static object[] DropZone<T>(string title, Rect pos)
        {
            GUI.Box(pos, title);
            var mPos = Event.current.mousePosition;
            var relX = mPos.x - pos.xMin;
            var relY = mPos.y - pos.yMin;
            if (relX > 0 && relX < pos.width && relY > 0 && relY < pos.height)
                return DragResult<T>();
            return null;
        }

        public static object[] DropZone<T>(string title, int w, int h)
        {
            Rect pos = GUILayoutUtility.GetRect(w, h);
            GUI.Box(pos, title);
            var mPos = Event.current.mousePosition;
            var relX = mPos.x - pos.xMin;
            var relY = mPos.y - pos.yMin;
            if (relX > 0 && relX < pos.width && relY > 0 && relY < pos.height)
                return DragResult<T>();
            return null;
        }

        public static object[] DragResult<T>()
        {
            EventType eventType = Event.current.type;
            foreach (object o in DragAndDrop.objectReferences)
                if (!(o is T)) return null;

            if (eventType == EventType.DragUpdated || eventType == EventType.DragPerform)
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                if (eventType == EventType.DragPerform)
                {
                    DragAndDrop.AcceptDrag();
                    Event.current.Use();
                    return DragAndDrop.objectReferences;
                }
                Event.current.Use();
            }
            return null;
        }
    }
}
