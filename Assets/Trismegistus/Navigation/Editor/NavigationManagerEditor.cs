using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Trismegistus.Navigation
{
    [CustomEditor(typeof(NavigationManager), true)]
    public class NavigationManagerEditor : Editor
    {
        private Tool _lastTool = Tool.None;

        private enum Mode
        {
            None,
            Add,
            Move
        }

        private Mode _currentMode = Mode.None;
        private int _indexFrom = -1;

        void OnEnable()
        {
            _lastTool = Tools.current;
            Tools.current = Tool.None;

            var navManager = (NavigationManager) target;
            
            navManager.CalculateWaypoints();
        }

        void OnDisable()
        {
            Tools.current = _lastTool;
            _currentMode = Mode.None;
        }

        [MenuItem("GameObject/Trismegistus/Navigator", false, 0)]
        public static void CreateNavigator()
        {
            var parent = Selection.activeTransform;
            var navigatorGuid = AssetDatabase.FindAssets("Navigation t:Prefab").First();
            var navigatorPath = AssetDatabase.GUIDToAssetPath(navigatorGuid);
            Debug.Log($"Navigator prefab finded at {navigatorPath}");
            var navigatorPrefab = AssetDatabase.LoadAssetAtPath(navigatorPath, typeof(GameObject));
            var navigator = Instantiate(navigatorPrefab, parent);
            navigator.name = navigatorPrefab.name;
        }

        public override void OnInspectorGUI()
        {
            EditorGUI.BeginChangeCheck();
            var customGui = new GUIStyle(EditorStyles.helpBox) {alignment = TextAnchor.MiddleCenter};

            var guiBackgroundColor = GUI.backgroundColor;
            var navManager = (NavigationManager) target;

            navManager.NavigationData = EditorGUILayout.ObjectField("Navigation Data",
                navManager.NavigationData, typeof(NavigationData), false) as NavigationData;
            serializedObject.Update();

            if (navManager.NavigationData == null)
            {
                GUILayout.Label("You must add navigation data!", EditorStyles.helpBox);
                if (GUILayout.Button("Create navigation data"))
                {
                    var path = EditorUtility.SaveFilePanelInProject("Save NavigationData asset", "New NavigationData",
                        "asset", "Enter name");
                    var navData = ScriptableObject.CreateInstance<NavigationData>();
                    AssetDatabase.CreateAsset(navData, path);
                    navManager.NavigationData = navData;
                }

                return;
            }

            navManager.WaypointPrefab =
                EditorGUILayout.ObjectField("Prefab", navManager.WaypointPrefab, typeof(WaypointBehaviour), false) as
                    WaypointBehaviour;
            //navManager.CalculateWaypoints();
            serializedObject.Update();
            var gradient = serializedObject.FindProperty("GradientForWaypoints");


            navManager.GradientForWaypoints =
                EditorGUILayout.GradientField("Waypoint coloring gradient", navManager.GradientForWaypoints);
            /*EditorGUILayout.PropertyField(gradient, new GUIContent("Waypoint coloring gradient"));
            serializedObject.ApplyModifiedProperties();*/

            //Drawing "Closed spline" Toggle
            {
                EditorGUI.BeginChangeCheck();
                navManager.IsCycled = EditorGUILayout.Toggle("Closed spline", navManager.IsCycled);
                
            
            navManager.StickToColliders = EditorGUILayout.Toggle("Stick to colliders", navManager.StickToColliders);
            if (EditorGUI.EndChangeCheck())
                navManager.CalculateWaypoints();
            }

            serializedObject.Update();
            var onClick = serializedObject.FindProperty("WaypointChanged");

            EditorGUILayout.PropertyField(onClick, new GUIContent("On Waypoint Changed"));
            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.BeginHorizontal();
            {
                GUI.enabled = navManager.Iterations > 0;
                if (GUILayout.Button("-", GUILayout.Width(30)))
                {
                    navManager.Iterations--;
                    navManager.CalculateWaypoints();
                    EditorUtility.SetDirty(target);
                }

                GUI.enabled = true;
                EditorGUILayout.LabelField("Smoothing per unit:");
                var prevIterations = navManager.Iterations;
                navManager.Iterations = EditorGUILayout.IntField(navManager.Iterations);
                if (prevIterations != navManager.Iterations)
                {
                    navManager.CalculateWaypoints();
                    EditorUtility.SetDirty(target);
                }

                if (GUILayout.Button("+", GUILayout.Width(30)))
                {
                    navManager.Iterations++;
                    navManager.CalculateWaypoints();
                    EditorUtility.SetDirty(target);
                }

                if (GUILayout.Button("Reload", GUILayout.Width(50)))
                {
                    EditorUtility.SetDirty(target);
                    navManager.CalculateWaypoints();
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal(EditorStyles.boldLabel);
            {
                if (navManager.WaypointPrefab == null) GUI.enabled = false;
                if (GUILayout.Button(
                    new GUIContent(_currentMode == Mode.Add ? "x" : "+", "Hold shift to add to the end"),
                    GUILayout.Width(30)))
                {
                    if (navManager.Waypoints.Count == 0 || Event.current.shift)
                    {
                        navManager.AddWaypoint();
                        return;
                    }

                    _currentMode = _currentMode == Mode.Add ? Mode.None : Mode.Add;
                }

                GUI.enabled = true;
                if (navManager.WaypointPrefab == null) GUILayout.Label("You must add prefab!", EditorStyles.helpBox);
            }
            EditorGUILayout.EndHorizontal();

            var w = navManager.Waypoints;
            
            for (int i = 0; i <= w.Count; i++)
            {
                if (_currentMode == Mode.Add)
                {
                    if (GUILayout.Button("Add"))
                    {
                        navManager.AddWaypoint(i);
                        _currentMode = Mode.None;
                        return;
                    }
                }

                if (_currentMode == Mode.Move && _indexFrom != i && _indexFrom != i - 1)
                {
                    if (GUILayout.Button("Move here"))
                    {
                        navManager.Relocate(_indexFrom, i);
                        _currentMode = Mode.None;
                        _indexFrom = -1;
                        return;
                    }
                }

                if (i == w.Count) continue;
                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                {
                    EditorGUILayout.BeginVertical(GUILayout.Width(30));
                    {
                        GUI.backgroundColor = w[i].LabelColor;
                        GUILayout.Label($"{i + 1}", customGui);
                        GUI.backgroundColor = guiBackgroundColor;
                    }
                    EditorGUILayout.EndVertical();
                    EditorGUILayout.BeginVertical();
                    {
                        if (GUILayout.Button(_indexFrom == i ? "x" : "Move", GUILayout.Width(40)))
                        {
                            if (_indexFrom == i)
                            {
                                _currentMode = Mode.None;
                                _indexFrom = -1;
                                return;
                            }

                            _indexFrom = i;
                            _currentMode = Mode.Move;
                            return;
                        }

                        if (GUILayout.Button(
                            new GUIContent("Del", "Hold shift to delete without prompt"),
                            GUILayout.Width(40)))
                        {
                            if (Event.current.shift || EditorUtility.DisplayDialog("Delete item?",
                                    "You will lose all it's data", "Delete", "Cancel"))
                            {
                                navManager.DeleteWaypoint(i);
                                return;
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
            if (EditorGUI.EndChangeCheck())
                SceneView.RepaintAll();
        }

        private void OnSceneGUI()
        {
            var n = target as NavigationManager;
            if (n == null) return;
            foreach (var waypoint in n.Waypoints)
            {
                EditorGUI.BeginChangeCheck();
                var newTargetPosition = Handles.PositionHandle(waypoint.Position + Vector3.up, Quaternion.identity);
                var newTargetRotation = Handles.RotationHandle(waypoint.Rotation, waypoint.Position);
                if (!EditorGUI.EndChangeCheck()) continue;

                waypoint.Position = newTargetPosition - Vector3.up;
                waypoint.Rotation = newTargetRotation;
                Undo.RecordObject(n.NavigationData, $"Change waypoint {waypoint.Caption} Position/Rotation");
                EditorUtility.SetDirty(n);
                n.CalculateWaypoints();
            }
        }
    }
}
