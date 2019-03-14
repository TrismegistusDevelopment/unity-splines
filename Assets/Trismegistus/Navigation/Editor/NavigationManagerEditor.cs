using System;
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

        private Mode currentMode = Mode.None;
        private int indexFrom = 0;

        void OnEnable()
        {
            _lastTool = Tools.current;
            Tools.current = Tool.None;


            var navManager = (NavigationManager)target;
            navManager.FindWaypoints();
            navManager.IndexToAddButton = -1;
            navManager.CalculateWaypoints();
        }

        void OnDisable()
        {
            Tools.current = _lastTool;
            currentMode = Mode.None;
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
            var customGui = new GUIStyle(EditorStyles.helpBox) { alignment = TextAnchor.MiddleCenter};

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
                    var path = EditorUtility.SaveFilePanelInProject("Save NavigationData asset", "New NavigationData", "asset", "Enter name");
                    var navData = ScriptableObject.CreateInstance<NavigationData>();
                    AssetDatabase.CreateAsset(navData, path);
                    navManager.NavigationData = navData;
                }
                return;
            }
            
            navManager.WaypointPrefab =
                EditorGUILayout.ObjectField("Prefab", navManager.WaypointPrefab, typeof(WaypointBehaviour), false) as WaypointBehaviour;
            //navManager.CalculateWaypoints();
            serializedObject.Update();
            var gradient = serializedObject.FindProperty("GradientForWaypoints");


            navManager.GradientForWaypoints =
                EditorGUILayout.GradientField("Waypoint coloring gradient", navManager.GradientForWaypoints);
            /*EditorGUILayout.PropertyField(gradient, new GUIContent("Waypoint coloring gradient"));
            serializedObject.ApplyModifiedProperties();*/
            
            navManager.IsCycled = EditorGUILayout.Toggle("Closed spline", navManager.IsCycled);
            navManager.StickToColliders = EditorGUILayout.Toggle("Stick to colliders", navManager.StickToColliders);

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
                    foreach (var wp in navManager.Waypoints)
                    {
                        //EditorUtility.SetDirty(wp);
                    }
                }

                GUI.enabled = true;
                EditorGUILayout.LabelField($"Smoothing per unit:");
                var prevIterations = navManager.Iterations;
                navManager.Iterations = EditorGUILayout.IntField(navManager.Iterations);
                if (prevIterations != navManager.Iterations)
                {
                    navManager.CalculateWaypoints();
                    EditorUtility.SetDirty(target);
                    foreach (var wp in navManager.Waypoints)
                    {
                        //EditorUtility.SetDirty(wp);
                    }
                }
                if (GUILayout.Button("+", GUILayout.Width(30)))
                {
                    navManager.Iterations++;
                    navManager.CalculateWaypoints();
                    EditorUtility.SetDirty(target);
                    foreach (var wp in navManager.Waypoints)
                    {
                        //EditorUtility.SetDirty(wp);
                    }
                }
                if (GUILayout.Button("Reload", GUILayout.Width(50)))
                {
                    //navManager.CalculateWaypoints();
                    EditorUtility.SetDirty(target);
                    navManager.CalculateWaypoints();
                    foreach (var wp in navManager.Waypoints)
                    {
                        //EditorUtility.SetDirty(wp);
                    }
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal(EditorStyles.boldLabel);
            {
                if (navManager.WaypointPrefab == null) GUI.enabled = false;
                if (GUILayout.Button(new GUIContent(currentMode == Mode.Add ? "x" : "+", "Hold shift to add to the end"), GUILayout.Width(30)))
                {
                    if (navManager.Waypoints.Count == 0 || Event.current.shift)
                    {
                        navManager.AddWaypoint();
                        //navManager.CalculateWaypoints();
                        return;
                    }
                    
                    currentMode = currentMode == Mode.Add ? Mode.None : Mode.Add;
                }

                GUI.enabled = true;
                if (navManager.WaypointPrefab == null) GUILayout.Label("You must add prefab!", EditorStyles.helpBox);
            }
            EditorGUILayout.EndHorizontal();

            var w = navManager.Waypoints;
            if (navManager.IndexToAddButton > w.Count - 1) navManager.IndexToAddButton = -1;
            for (int i = 0; i <= w.Count; i++)
            {
                if (currentMode==Mode.Add)
                {
                    if (GUILayout.Button("Add"))
                    {
                        navManager.AddWaypoint(i);
                        currentMode = Mode.None;
                        //navManager.CalculateWaypoints();
                        return;
                    }
                }
                
                if (currentMode==Mode.Move && indexFrom !=i && indexFrom !=i-1)
                {
                    if (GUILayout.Button("Move here"))
                    {
                        navManager.Relocate(indexFrom, i);
                        currentMode = Mode.None;
                        indexFrom = -1;
                        return;
                    }
                }
                if (i == w.Count) continue; 
                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                {  
                    EditorGUILayout.BeginVertical(GUILayout.Width(30));
                    {
                        GUI.backgroundColor = w[i].LabelColor;
                        GUILayout.Label( $"{i + 1}", customGui);
                        GUI.backgroundColor = guiBackgroundColor;
                        
                        /*if (TriInspectorEditor.DrawListButtons(navManager, i, w.Count))
                        {
                            foreach (var wp in navManager.Waypoints)
                            {
                                EditorUtility.SetDirty(wp);
                            }
                            return;
                        }*/
                    }
                    EditorGUILayout.EndVertical();
                    EditorGUILayout.BeginVertical();
                    {
                        if (GUILayout.Button(indexFrom == i ? "x" : "Move", GUILayout.Width(40)))
                        {
                            if (indexFrom == i)
                            {
                                currentMode = Mode.None;
                                indexFrom = -1;
                                return;
                            }

                            indexFrom = i;
                            currentMode = Mode.Move;
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
                        //var so = new SerializedObject(w[i]);
                        //so.Update();
                        //var onPlayerReached = so.FindProperty("PlayerReachedThePoint");
                        //EditorGUILayout.PropertyField(onPlayerReached, new GUIContent("Player Reached The Point"));
                        //so.ApplyModifiedProperties();*/
                    }
                    EditorGUILayout.EndVertical();
                }
                EditorGUILayout.EndHorizontal();
            }
        }

        private void OnSceneGUI()
        {
            var n = target as NavigationManager;
            if (n == null) return;
            foreach (var waypoint in n.Waypoints)
            {
                EditorGUI.BeginChangeCheck();
                Vector3 newTargetPosition = Handles.PositionHandle(waypoint.Position+Vector3.up, Quaternion.identity);
                if (EditorGUI.EndChangeCheck())
                {
                    waypoint.Position = newTargetPosition - Vector3.up;
                    Undo.RecordObject(n.NavigationData, $"Change waypoint {waypoint.Caption} Position");
                    EditorUtility.SetDirty(n);
                    n.CalculateWaypoints();
                }
            }
        }
    }
}
