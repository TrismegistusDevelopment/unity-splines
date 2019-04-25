using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEditorInternal;

namespace Trismegistus.Navigation {
    [CustomEditor(typeof(NavigationManager), true)]
    public class NavigationManagerEditor : Editor {
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
            var navManager = (NavigationManager) target;

            if (!navManager.NavigationData) return;

            navManager.CalculateWaypoints();
        }

        void OnDisable() {
            Tools.current = _lastTool;
            _currentMode  = Mode.None;
        }

        public override void OnInspectorGUI() {
            EditorGUI.BeginChangeCheck();

            var navManager = (NavigationManager) target;

            if (DrawNavData(navManager)) return;

            serializedObject.Update();

            DrawParams(navManager);

            DrawSmoothing(navManager);

            if (DrawAddButton(navManager)) return;

            if (DrawWaypoints(navManager)) return;

            if (EditorGUI.EndChangeCheck()) {
                EditorUtility.SetDirty(navManager.NavigationData);
                SceneView.RepaintAll();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="navManager"></param>
        /// <returns>Need to break OnInspectorGUI</returns>
        private bool DrawWaypoints(NavigationManager navManager) {
            var w = navManager.Waypoints;

            for (int i = 0; i <= w.Count; i++) {
                var showMoveButton = _indexFrom != i && _indexFrom != i - 1;

                if (_currentMode == Mode.Add) {
                    if (GUILayout.Button("Add")) {
                        navManager.AddWaypoint(i);
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
        private bool DrawAddButton(NavigationManager navManager) {
            EditorGUILayout.BeginHorizontal(EditorStyles.boldLabel);
            {
                if (GUILayout.Button(
                    new GUIContent(_currentMode == Mode.Add ? "x" : "+", "Hold shift to add to the end"),
                    GUILayout.Width(30))) {
                    if (navManager.Waypoints.Count == 0 || Event.current.shift) {
                        navManager.AddWaypoint();
                        return true;
                    }

                    _currentMode = _currentMode == Mode.Add ? Mode.None : Mode.Add;
                }

                GUI.enabled = true;
            }
            EditorGUILayout.EndHorizontal();
            return false;
        }

        private void DrawSmoothing(NavigationManager navManager) {
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
        private static void DrawParams(NavigationManager navManager) {
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
                EditorUtility.SetDirty(navManager.NavigationData);
            }
        }

        /// <summary>
        /// Draws NavigationData field
        /// </summary>
        /// <param name="navManager"></param>
        /// <returns>Need to break OnInspectorGUI</returns>
        private bool DrawNavData(NavigationManager navManager) {
            navManager.NavigationData = EditorGUILayout.ObjectField("Navigation Data",
                navManager.NavigationData, typeof(NavigationData), false) as NavigationData;
            serializedObject.Update();

            if (navManager.NavigationData == null) {
                GUILayout.Label("You must add navigation data!", EditorStyles.helpBox);
                if (GUILayout.Button("Create navigation data")) {
                    var path = EditorUtility.SaveFilePanelInProject("Save NavigationData asset",
                        "New NavigationData",
                        "asset", "Enter name");
                    var navData = CreateInstance<NavigationData>();
                    AssetDatabase.CreateAsset(navData, path);
                    navManager.NavigationData = navData;
                }

                return true;
            }

            return false;
        }

        private void OnSceneGUI() {
            var navManager = target as NavigationManager;

            if (!navManager) return;
            if (!navManager.NavigationData) return;

            foreach (var waypoint in navManager.Waypoints) {
                EditorGUI.BeginChangeCheck();
                var newTargetPosition = Handles.PositionHandle(waypoint.Position + Vector3.up, Quaternion.identity);
                var newTargetRotation = Handles.RotationHandle(waypoint.Rotation, waypoint.Position);
                if (!EditorGUI.EndChangeCheck()) continue;

                waypoint.Position = newTargetPosition - Vector3.up;
                waypoint.Rotation = newTargetRotation;
                Undo.RecordObject(navManager.NavigationData, $"Change waypoint {waypoint.Caption} Position/Rotation");
                EditorUtility.SetDirty(navManager);
                navManager.CalculateWaypoints();
            }
        }

        #endregion

        [MenuItem("GameObject/Trismegistus/Navigator", false, 0)]
        public static void CreateNavigator() {
            var parent        = Selection.activeTransform;
            var navigatorGuid = AssetDatabase.FindAssets("Navigation t:Prefab").First();
            var navigatorPath = AssetDatabase.GUIDToAssetPath(navigatorGuid);
            Debug.Log($"Navigator prefab found at {navigatorPath}");
            var navigatorPrefab = AssetDatabase.LoadAssetAtPath(navigatorPath, typeof(GameObject));
            var navigator       = Instantiate(navigatorPrefab, parent);
            navigator.name = navigatorPrefab.name;
        }
    }
}