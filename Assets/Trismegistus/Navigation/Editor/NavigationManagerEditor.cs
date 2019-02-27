using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Trismegistus.Navigation
{ 
    [CustomEditor(typeof(NavigationManager), true)]
    public class NavigationManagerEditor : Editor
    {
        private Tool LastTool = Tool.None;

        void OnEnable()
        {
            LastTool = Tools.current;
            Tools.current = Tool.None;


            var navManager = (NavigationManager)target;
            navManager.FindWaypoints();
            navManager.IndexToAddButton = -1;
        }

        void OnDisable()
        {
            Tools.current = LastTool;
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
            navManager.WaypointPrefab =
                (WaypointBehaviour) EditorGUILayout.ObjectField("Prefab", navManager.WaypointPrefab, typeof(WaypointBehaviour), false);
            //navManager.CalculateWaypoints();
            serializedObject.Update();
            var gradient = serializedObject.FindProperty("GradientForWaypoints");
            
            EditorGUILayout.PropertyField(gradient, new GUIContent("Waypoint coloring gradient"));
            serializedObject.ApplyModifiedProperties();
            
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
                    //navManager.CalculateWaypoints();
                    EditorUtility.SetDirty(target);
                    foreach (var wp in navManager.Waypoints)
                    {
                        EditorUtility.SetDirty(wp);
                    }
                }

                GUI.enabled = true;
                EditorGUILayout.LabelField($"Smoothing per unit:");
                var prevIterations = navManager.Iterations;
                navManager.Iterations = EditorGUILayout.IntField(navManager.Iterations);
                if (prevIterations != navManager.Iterations)
                {
                    //navManager.CalculateWaypoints();
                    EditorUtility.SetDirty(target);
                    foreach (var wp in navManager.Waypoints)
                    {
                        EditorUtility.SetDirty(wp);
                    }
                }
                if (GUILayout.Button("+", GUILayout.Width(30)))
                {
                    navManager.Iterations++;
                    //navManager.CalculateWaypoints();
                    EditorUtility.SetDirty(target);
                    foreach (var wp in navManager.Waypoints)
                    {
                        EditorUtility.SetDirty(wp);
                    }
                }
                if (GUILayout.Button("Reload", GUILayout.Width(50)))
                {
                    //navManager.CalculateWaypoints();
                    EditorUtility.SetDirty(target);
                    foreach (var wp in navManager.Waypoints)
                    {
                        EditorUtility.SetDirty(wp);
                    }
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal(EditorStyles.boldLabel);
            {
                if (navManager.WaypointPrefab == null) GUI.enabled = false;
                if (GUILayout.Button("+", GUILayout.Width(30)))
                {
                    //navManager.AddInspectorLine();
                    foreach (var wp in navManager.Waypoints)
                    {
                        EditorUtility.SetDirty(wp);
                    }
                }
                GUI.enabled = true;
                if (navManager.WaypointPrefab == null) GUILayout.Label("You must add prefab!", EditorStyles.helpBox);
                else
                {
                    var postfix = navManager.IndexToAddButton == -1 ? "to end of list" : $"after {navManager.IndexToAddButton+1} position";
                    GUILayout.Label($"Insert waypoint {postfix}", EditorStyles.helpBox);
                }
            }
            EditorGUILayout.EndHorizontal();

            var w = navManager.Waypoints;
            if (navManager.IndexToAddButton > w.Count - 1) navManager.IndexToAddButton = -1;
            for (int i = 0; i < w.Count; i++)
            {
                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                {  
                    EditorGUILayout.BeginVertical(GUILayout.Width(30));
                    {
                        GUI.backgroundColor = w[i].LabelColor;
                        var prevIndex = navManager.IndexToAddButton;
                        var thisIndex = GUILayout.Toggle(navManager.IndexToAddButton == i, $"{i + 1}", customGui);
                        if (thisIndex) navManager.IndexToAddButton = i;
                        if (!thisIndex && prevIndex == i) navManager.IndexToAddButton = -1;
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
                        w[i].Caption = EditorGUILayout.TextField("Caption", w[i].Caption);
                        var so = new SerializedObject(w[i]);
                        so.Update();
                        var onPlayerReached = so.FindProperty("PlayerReachedThePoint");
                        EditorGUILayout.PropertyField(onPlayerReached, new GUIContent("Player Reached The Point"));
                        so.ApplyModifiedProperties();
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
                    Undo.RecordObject(waypoint.transform, $"Change waypoint {waypoint.FullCaption} Position");
                    waypoint.Position = newTargetPosition - Vector3.up;
                    //n.CalculateWaypoints();
                    EditorUtility.SetDirty(waypoint);
                }
            }
        }
        
        
    }
}
