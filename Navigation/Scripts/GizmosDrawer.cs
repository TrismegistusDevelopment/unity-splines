#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Trismegistus.Navigation
{
    public static class GizmosDrawer
    {
        public static void DrawWaypointEntity(WaypointEntity entity, int index)
        {
            var isTemp = entity.IsTemp;
            var labelColor = entity.LabelColor;
            var position = entity.Position;
            var caption = entity.Caption;
            GUIStyle style;
            
            if (!isTemp)
            {
                style = new GUIStyle(EditorStyles.helpBox)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontStyle = FontStyle.Bold
                };

                if ((labelColor.r + labelColor.g + labelColor.b) / 3 < 0.5f) style.normal.textColor = Color.white;
                GUI.backgroundColor = labelColor;

                for (int i = 0; i < 3; i++) //Because it's too dull
                {
                    Handles.Label(position, $"{index+1} {caption}".Trim(' '), style);
                }
            }

            var editorCamNormal = SceneView.currentDrawingSceneView.camera.transform.position - position;
            var editorDistance = editorCamNormal.magnitude;
            style = new GUIStyle
            {
                alignment = TextAnchor.UpperCenter,
                fontStyle = FontStyle.Bold
            };
            Handles.color = Color.Lerp(Color.white, Color.black, (labelColor.r + labelColor.g + labelColor.b) / 3);
            style.normal.textColor = labelColor;
            Handles.color = labelColor;

            Handles.DrawSolidDisc(position, editorCamNormal, editorDistance / 200f);

            var sign = Mathf.Sign(position.y);
            var distance = position.y * sign;

            Handles.color = labelColor;

            if (sign > 0) Handles.DrawLine(position, position - Vector3.up * distance);
            else
            {
                Handles.DrawDottedLine(position, position + Vector3.up * distance, 2);
            }

            var col = labelColor;
            col.a = 0.3f;
            Handles.color = col;
            Handles.DrawSolidDisc(position - Vector3.up * sign * distance, Vector3.up, 0.5f);
        }
        
        public static void DrawSpline(IReadOnlyList<WaypointEntity> dwps, bool isCycled)
        {
            var length = isCycled ? dwps.Count : dwps.Count - 1;
            if (length <= 0) return;

            for (var i = 0; i < length; i++)
            {
                var entity = dwps[i];
                Handles.color = entity.LabelColor;
                var position = entity.Position;
                Handles.DrawLine(position, dwps[(i + dwps.Count + 1) % dwps.Count].Position);
                Handles.DrawLine(position, position + Vector3.up);
            }
        }
    }
}
#endif