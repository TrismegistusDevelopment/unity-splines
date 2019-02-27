using System;
using System.Collections.Generic;
using System.Linq;
using Trismegistus.CoreTools.Utils;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;

namespace Trismegistus.Navigation
{
    public class NavigationManager : MonoBehaviour, ITriInspectorMovableList
    {
        public static NavigationManager Instance {get; private set; }

        public bool IsCycled;

        public bool StickToColliders;

        public WaypointBehaviour WaypointPrefab;

        public Gradient GradientForWaypoints;

        public Vector3 CurrentWaypointPosition
        {
            get
            {
                var index = currentWaypointIndex >= Waypoints.Count
                    ? Waypoints.Count - 1
                    : currentWaypointIndex;
                return Waypoints[index].Position; 
            }
        }

        public UnityEvent WaypointChanged;

        public bool AllTargetsWalked => currentWaypointIndex == Waypoints.Count;

        public int WaypointsCount => Waypoints.Count;

        public List<WaypointBehaviour> Waypoints;
        public WaypointEntity[] DynamicWaypoints;
        public int Iterations = 20;
        public int IndexToAddButton;
        private int currentWaypointIndex;
        
        void Awake()
        {
            Instance = this;
            Init();
        }

        public void Init()
        {
            FindWaypoints();
            currentWaypointIndex = 0;
            WaypointChanged = new UnityEvent();

            foreach (var waypoint in Waypoints)
            {
                waypoint.GetComponent<WaypointBehaviour>().PlayerReachedThePoint.AddListener(SwitchToTheNextWaypoint);
            }
        }

#if UNITY_EDITOR
        void OnDrawGizmos()
        {
            var wps = Waypoints;

            if (wps == null || wps.Count == 0)
            {
                FindWaypoints();
            }

            if (wps == null || wps.Count == 0)
            {
                return;
            }

            var dwps = DynamicWaypoints;

            if (dwps == null || dwps.Length == 0)
            {
                try
                {
                    CalculateWaypoints();
                }
                catch (Exception e)
                {
                    Debug.Log($"Error while calculating waywoints: {e}");
                    return;
                }
                if (dwps == null) return;
            }

            var length = IsCycled ? dwps.Length : dwps.Length - 1;

            for (int i = 0; i < length; i++)
            {
                Handles.color = dwps[i].LabelColor;
                Handles.DrawLine(dwps[i].Position, dwps[(i+dwps.Length+1)%dwps.Length].Position);
                Handles.DrawLine(dwps[i].Position, dwps[i].Position + Vector3.up);
            }

            foreach (var wp in Waypoints)
            {
                wp.WaypointEntity.DrawGizmos();
            }
        }
#endif

        public Vector3 GetDestination(int index)
        {
            if (!IsCycled && index > DynamicWaypoints.Length - 1) index = DynamicWaypoints.Length - 1;
            index %= DynamicWaypoints.Length;
            return DynamicWaypoints[index].Position;
        }

        public void FindWaypoints()
        {
            Waypoints = transform.GetComponentsInChildren<WaypointBehaviour>().ToList();
            CalculateWaypoints();
            UpdateColors();
        }

        public int SelectClosestWaypointIndex(Vector3 position)
        {
            float min = float.MaxValue;
            int nextIndex = 0;

            for (var i = 0; i < DynamicWaypoints.Length; i++)
            {
                var point = DynamicWaypoints[i];
                float dist = Vector3.Distance(position, point.Position);
                if (dist < min)
                {
                    min = dist;
                    nextIndex = i;
                }
            }

            return nextIndex;
        }

        public void SwitchToTheNextWaypoint()
        {
            if (IsCycled)
            {
                currentWaypointIndex = (currentWaypointIndex + 1) % Waypoints.Count;
            }
            else if (currentWaypointIndex < Waypoints.Count)
            {
                currentWaypointIndex++;
            }
            
            WaypointChanged.Invoke();
        }

        public static WaypointEntity[] CalculateWaypoints(List<WaypointBehaviour> list, int iterations,
            WaypointBehaviour prefab, Transform tr, bool stickToColliders, bool cycled)
        {
            var distances = list.Select((t, i) => (t.Position - list[(i + 1) % list.Count].Position).magnitude)
                .ToList();

            var p = new List<Vector3>();
            
            foreach (var waypointBehaviour in list)
            {
                waypointBehaviour.EntityPosition = waypointBehaviour.Position;
                
                if (stickToColliders)
                {
                    Ray ray = new Ray(waypointBehaviour.Position + Vector3.up * 5, Vector3.down);
                    RaycastHit[] hits = Physics.RaycastAll(ray);

                    var hit = hits?.Where(x => x.collider != waypointBehaviour.Collider)?.OrderBy(x => x.distance)?
                        .First();

                    if (hit != null) waypointBehaviour.EntityPosition = hit.Value.point;
                }

                p.Add(waypointBehaviour.EntityPosition);
            }

            var curve = new List<WaypointEntity>();


            var points = new List<NavPoint>();
            if (cycled)
            {
                points = p.Select((t, i) =>
                    new NavPoint(t, p[(p.Count + i - 1) % p.Count], p[(p.Count + i + 1) % p.Count])).ToList();
            }
            else
            {
                for (int i = 0; i < p.Count; i++)
                {
                    var pCenter = p[i];
                    if (p.Count == 1) points.Add(new NavPoint(pCenter, pCenter, pCenter));
                    else
                    {
                        var pForward = i != p.Count - 1 ? p[i + 1] : p[i] + (p[i - 1] - p[i]);
                        var pBackward = i != 0 ? p[i - 1] : p[i] + (p[i + 1] - p[i]);
                        points.Add(new NavPoint(pCenter, pBackward, pForward));
                    }
                }
            }

            for (var i = 0; i < points.Count; i++)
            {
                var navPoint = points[i];
                list[i].WaypointEntity = new WaypointEntity(list[i].EntityPosition, false, Color.black, list[i].FullCaption);
                curve.Add(list[i].WaypointEntity);

                var localIterations = Mathf.CeilToInt(distances[i] * iterations / 10);

                if (i == points.Count-1 && !cycled) continue;
                {
                    for (var j = 1; j < localIterations; j++)
                    {
                        var position = NavPoint.GetPoint(navPoint.PointCenter,
                            navPoint.AbsPerpendicularForward, points[(i + 1) % points.Count].AbsPerpendicularBackward,
                            points[(i + 1) % points.Count].PointCenter, (float) j / localIterations);

                        if (stickToColliders)
                        {
                            Ray ray = new Ray(position + Vector3.up * 1, Vector3.down);
                            RaycastHit[] hits = Physics.RaycastAll(ray);
                            if (hits != null && hits.Length > 0)
                            {
                                var hit = hits?.OrderBy(x => x.distance).First();

                                position.y = hit.Value.point.y;
                            }
                        }

                        curve.Add(new WaypointEntity(position, true));
                    }
                }
            }

            return curve.ToArray();
        }

        #region Inspector
        public void AddInspectorLine()
        {
            TriInspector.AddInspectorLine(ref Waypoints, WaypointPrefab, transform, index: IndexToAddButton);
            CalculateWaypoints();
            UpdateColors();
        }

        public void DeleteInspectorLine(int index)
        {
            TriInspector.DeleteInspectorLine(index, ref Waypoints);
            CalculateWaypoints();
            UpdateColors();
        }

        public void MoveInspectorLine(int index, InspectorDirection direction)
        {
            TriInspector.MoveInspectorLine(index, direction, ref Waypoints);
            CalculateWaypoints();
            UpdateColors();
        }

        public void UpdateHierarchy()
        {
            TriInspector.UpdateHierarchy(Waypoints);
            for (var i = 0; i < Waypoints.Count; i++)
            {
                Waypoints[i].Index = i;
                Waypoints[i].gameObject.name = Waypoints[i].FullCaption;
            }
        }

        public GameObject GetObject(int index)
        {
            return TriInspector.GetObject(Waypoints, index);
        }

        public void CalculateWaypoints()
        {
            try
            {
                DynamicWaypoints = CalculateWaypoints(Waypoints, Iterations, WaypointPrefab, transform, StickToColliders, IsCycled);
                UpdateHierarchy();
                UpdateColors();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Exception{e}, reloading targets");
                throw;
            }
            
        }

        public void UpdateColors()
        {
            if (DynamicWaypoints == null) return;
            if (DynamicWaypoints.Length <= 1) return;
            for (var i = 0; i < DynamicWaypoints.Length; i++)
            {
                var waypoint = DynamicWaypoints[i];
                var gradient = GradientForWaypoints ?? new Gradient();
                
                waypoint.LabelColor = gradient.Evaluate((float)i / (DynamicWaypoints.Length-1));
            }
        }
        
        #endregion

    }
}
