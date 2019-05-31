using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Trismegistus.Splines.Editor {
    [CustomEditor(typeof(SplineManager), true)]
    public class SplineManagerEditor : UnityEditor.Editor {
        private Tool _lastTool = Tool.None;

        private enum Mode {
            None,
            Add,
            Move
        }

        private Mode _currentMode = Mode.None;
        private int  _indexFrom   = -1;

        private GUIStyle _customGui;
        private Color    _guiBackgroundColor;

        #region Editor

        void OnEnable() {
            _lastTool     = Tools.current;
            Tools.current = Tool.None;
            _customGui = new GUIStyle(EditorStyles.helpBox)
                {alignment = TextAnchor.MiddleCenter};
            _guiBackgroundColor = GUI.backgroundColor;
            var navManager = (SplineManager) target;

            if (!navManager.splineData) return;

            navManager.CalculateWaypoints();
        }

        void OnDisable() {
            Tools.current = _lastTool;
            _currentMode  = Mode.None;
        }

        public override void OnInspectorGUI() {
            EditorGUI.BeginChangeCheck();

            var navManager = (SplineManager) target;

            if (DrawNavData(navManager)) return;

            serializedObject.Update();

            DrawParams(navManager);

            DrawSmoothing(navManager);

            if (DrawAddButton(navManager)) return;

            if (DrawWaypoints(navManager)) return;

            if (EditorGUI.EndChangeCheck()) {
                EditorUtility.SetDirty(navManager.splineData);
                SceneView.RepaintAll();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="navManager"></param>
        /// <returns>Need to break OnInspectorGUI</returns>
        private bool DrawWaypoints(SplineManager navManager) {
            var w = navManager.Waypoints;

            for (int i = 0; i <= w.Count; i++) {
                var showMoveButton = _indexFrom != i && _indexFrom != i - 1;

                if (_currentMode == Mode.Add) {
                    if (GUILayout.Button("Add")) {
                        navManager.AddPoint(i);
                        _currentMode = Mode.None;
                        return true;
                    }
                }

                if (_currentMode == Mode.Move && showMoveButton) {
                    if (GUILayout.Button("Move here")) {
                        navManager.Relocate(_indexFrom, i);
                        _currentMode = Mode.None;
                        _indexFrom   = -1;
                        return true;
                    }
                }

                if (i == w.Count) continue;
                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                {
                    EditorGUILayout.BeginVertical(GUILayout.Width(30));
                    {
                        GUI.backgroundColor = w[i].LabelColor;
                        GUILayout.Label($"{i + 1}", _customGui);
                        GUI.backgroundColor = _guiBackgroundColor;
                    }
                    EditorGUILayout.EndVertical();
                    EditorGUILayout.BeginVertical();
                    {
                        if (GUILayout.Button(_indexFrom == i ? "x" : "Move", GUILayout.Width(40))) {
                            if (_indexFrom == i) {
                                _currentMode = Mode.None;
                                _indexFrom   = -1;
                                return true;
                            }

                            _indexFrom   = i;
                            _currentMode = Mode.Move;
                            return true;
                        }

                        if (GUILayout.Button(
                            new GUIContent("Del", "Hold shift to delete without prompt"),
                            GUILayout.Width(40))) {
                            if (Event.current.shift || EditorUtility.DisplayDialog("Delete item?",
                                    "You will lose all it's data", "Delete", "Cancel")) {
                                navManager.DeleteWaypoint(i);
                                return true;
                            }
                        }
                    }
                    EditorGUILayout.EndVertical();
                    EditorGUILayout.BeginVertical();
                    {
                        w[i].Caption = EditorGUILayout.TextField("Caption", w[i].Caption);

                        //TODO add event displaying
                        //var so = new SerializedObject(w[i]);
                        //so.Update();
                        //var onPlayerReached = so.FindProperty("PlayerReachedThePoint");
                        //EditorGUILayout.PropertyField(onPlayerReached, new GUIContent("Player Reached The Point"));
                        //so.ApplyModifiedProperties();*/

                        EditorGUI.BeginChangeCheck();
                        w[i].IsTemp = !EditorGUILayout.Toggle("Basic", !w[i].IsTemp);

                        if (EditorGUI.EndChangeCheck())
                            SceneView.RepaintAll();
                    }
                    EditorGUILayout.EndVertical();
                }
                EditorGUILayout.EndHorizontal();
            }

            return false;
        }

        /// <summary>
        /// Draws "add waypoint" button
        /// </summary>
        /// <param name="navManager"></param>
        /// <returns>Need to break OnInspectorGUI</returns>
        private bool DrawAddButton(SplineManager navManager) {
            EditorGUILayout.BeginHorizontal(EditorStyles.boldLabel);
            {
                if (GUILayout.Button(
                    new GUIContent(_currentMode == Mode.Add ? "x" : "+", "Hold shift to add to the end"),
                    GUILayout.Width(30))) {
                    if (navManager.Waypoints.Count == 0 || Event.current.shift) {
                        navManager.AddPoint();
                        return true;
                    }

                    _currentMode = _currentMode == Mode.Add ? Mode.None : Mode.Add;
                }

                GUI.enabled = true;
            }
            EditorGUILayout.EndHorizontal();
            return false;
        }

        private void DrawSmoothing(SplineManager navManager) {
            EditorGUILayout.BeginHorizontal();
            {
                GUI.enabled = navManager.Iterations > 0;
                if (GUILayout.Button("-", GUILayout.Width(30))) {
                    navManager.Iterations--;
                    navManager.CalculateWaypoints();
                    EditorUtility.SetDirty(target);
                }

                GUI.enabled = true;
                EditorGUILayout.LabelField("Smoothing per unit:");
                var prevIterations = navManager.Iterations;
                navManager.Iterations = EditorGUILayout.IntField(navManager.Iterations);
                if (prevIterations != navManager.Iterations) {
                    navManager.CalculateWaypoints();
                    EditorUtility.SetDirty(target);
                }

                if (GUILayout.Button("+", GUILayout.Width(30))) {
                    navManager.Iterations++;
                    navManager.CalculateWaypoints();
                    EditorUtility.SetDirty(target);
                }

                if (GUILayout.Button("Reload", GUILayout.Width(50))) {
                    EditorUtility.SetDirty(target);
                    navManager.CalculateWaypoints();
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// Draws gradient, closed and colliders params
        /// </summary>
        /// <param name="navManager"></param>
        private static void DrawParams(SplineManager navManager) {
            EditorGUI.BeginChangeCheck();
            {
                navManager.GradientForWaypoints =
                    EditorGUILayout.GradientField("Waypoint coloring gradient", navManager.GradientForWaypoints);

                navManager.IsCycled = EditorGUILayout.Toggle("Closed spline", navManager.IsCycled);

                navManager.StickToColliders = EditorGUILayout.Toggle("Stick to colliders", navManager.StickToColliders);

                if (navManager.StickToColliders) {
                    LayerMask tempMask = EditorGUILayout.MaskField("Raycast mask",
                        InternalEditorUtility.LayerMaskToConcatenatedLayersMask(navManager.LayerMask),
                        InternalEditorUtility.layers);

                    navManager.LayerMask = InternalEditorUtility.ConcatenatedLayersMaskToLayerMask(tempMask);
                }
            }
            if (EditorGUI.EndChangeCheck()) {
                navManager.CalculateWaypoints();
                EditorUtility.SetDirty(navManager.splineData);
            }
        }

        /// <summary>
        /// Draws NavigationData field
        /// </summary>
        /// <param name="navManager"></param>
        /// <returns>Need to break OnInspectorGUI</returns>
        private bool DrawNavData(SplineManager navManager) {
            navManager.splineData = EditorGUILayout.ObjectField("Navigation Data",
                navManager.splineData, typeof(SplineData), false) as SplineData;
            serializedObject.Update();

            if (navManager.splineData == null) {
                GUILayout.Label("You must add navigation data!", EditorStyles.helpBox);
                if (GUILayout.Button("Create navigation data")) {
                    var path = EditorUtility.SaveFilePanelInProject("Save NavigationData asset",
                        "New NavigationData",
                        "asset", "Enter name");
                    var navData = CreateInstance<SplineData>();
                    AssetDatabase.CreateAsset(navData, path);
                    navManager.splineData = navData;
                }

                return true;
            }

            return false;
        }

        private void OnSceneGUI() {
            var navManager = target as SplineManager;

            if (!navManager) return;
            if (!navManager.splineData) return;

            foreach (var waypoint in navManager.Waypoints) {
                EditorGUI.BeginChangeCheck();
                var newTargetPosition = Handles.PositionHandle(waypoint.Position + Vector3.up, Quaternion.identity);
                var newTargetRotation = Handles.RotationHandle(waypoint.Rotation, waypoint.Position);
                if (!EditorGUI.EndChangeCheck()) continue;

                waypoint.Position = newTargetPosition - Vector3.up;
                waypoint.Rotation = newTargetRotation;
                Undo.RecordObject(navManager.splineData, $"Change waypoint {waypoint.Caption} Position/Rotation");
                EditorUtility.SetDirty(navManager);
                navManager.CalculateWaypoints();
            }
        }

        #endregion

        [MenuItem("GameObject/Trismegistus/Spline Manager", false, 0)]
        public static void CreateNavigator(){
            var parent = Selection.activeTransform;
            var splineManagerGuid = AssetDatabase.FindAssets("SplineManager t:Prefab").First();
            var splineManagerPath = AssetDatabase.GUIDToAssetPath(splineManagerGuid);
            //Debug.Log($"SplineManager prefab found at {navigatorPath}");
            var splineManagerPrefab = AssetDatabase.LoadAssetAtPath(splineManagerPath, typeof(GameObject));
            var splineManager = Instantiate(splineManagerPrefab, parent);
            splineManager.name = splineManagerPrefab.name;
        }
    }
}