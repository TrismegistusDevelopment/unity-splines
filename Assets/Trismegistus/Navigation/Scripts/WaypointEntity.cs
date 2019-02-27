#if UNITY_EDITOR
using UnityEditor;
#endif

using UnityEngine;

namespace Trismegistus.Navigation
{
    public class WaypointEntity
    {
        public Vector3 Position;
        public bool IsTemp;
        public Color LabelColor;
        public string Caption;

        public WaypointEntity()
        {
        }

        public WaypointEntity(Vector3 position, bool isTemp, Color labelColor, string caption)
        {
            Position = position;
            IsTemp = isTemp;
            LabelColor = labelColor;
            Caption = caption;
        }

        public WaypointEntity(Vector3 position, bool isTemp)
        {
            Position = position;
            IsTemp = isTemp;
        }

#if UNITY_EDITOR
        public void DrawGizmos()
        {
            if (!IsTemp)
            {
                var style = new GUIStyle(EditorStyles.helpBox)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontStyle = FontStyle.Bold
                };

                if ((LabelColor.r + LabelColor.g + LabelColor.b) / 3 < 0.5f) style.normal.textColor = Color.white;
                GUI.backgroundColor = LabelColor;

                for (int i = 0; i < 3; i++)
                {
                    Handles.Label(Position, Caption, style);
                }
                
                
            }
            else
            {
                var editorCamNormal = SceneView.currentDrawingSceneView.camera.transform.position - Position;
                var editorDistance = editorCamNormal.magnitude;
                var style = new GUIStyle()
                {
                    alignment = TextAnchor.UpperCenter,
                    fontStyle = FontStyle.Bold
                };
                Handles.color = Color.Lerp(Color.white, Color.black, (LabelColor.r + LabelColor.g + LabelColor.b) / 3);
                
                style.normal.textColor = LabelColor;
                Handles.color = LabelColor;
                
                Handles.DrawSolidDisc(Position, editorCamNormal, editorDistance / 200f);
            }
        }
#endif
    }
}