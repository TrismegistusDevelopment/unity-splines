using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;

namespace Trismegistus.Navigation
{
    public interface INavigationManager
    {
        int WaypointsCount { get; }
        List<WaypointEntity> Waypoints { get; }

        Vector3 GetDestination(int index);
        Vector3 GetDestination(float t);
        Vector3 GetVelocity(float t);
        int SelectClosestWaypointIndex(Vector3 position);
        Vector3 SelectClosestWaypointPosition(Vector3 position);
    }
    
    public class NavigationManager : MonoBehaviour, INavigationManager
    {
        public NavigationData NavigationData;
        
        public bool IsCycled
        {
            get => NavigationData.IsCycled;
            set => NavigationData.IsCycled = value;
        }

        public bool StickToColliders
        {
            get => NavigationData.StickToColliders;
            set => NavigationData.StickToColliders = value;
        }

        /*public WaypointBehaviour WaypointPrefab
        {
            get => NavigationData.WaypointPrefab;
            set => NavigationData.WaypointPrefab = value;
        }*/

        public Gradient GradientForWaypoints
        {
            get => NavigationData.GradientForWaypoints;
            set => NavigationData.GradientForWaypoints = value;
        }
        
        public int Iterations
        {
            get => NavigationData.Iterations;
            set => NavigationData.Iterations = value;
        }

        
        public int WaypointsCount => _waypoints.Count;

        public List<WaypointEntity> Waypoints => _waypoints;
        
        public UnityEvent WaypointChanged;
        
        public WaypointEntity[] DynamicWaypoints;

        private List<WaypointEntity> _waypoints => NavigationData.Waypoints;
        
        void Awake()
        {
            Init();
        }
        

#if UNITY_EDITOR
        void OnDrawGizmos()
        {
            if (_waypoints.Count ==0) return;
            
            for (var index = 0; index < _waypoints.Count; index++)
            {
                GizmosDrawer.DrawWaypointEntity(_waypoints[index], index);
            }

            if (DynamicWaypoints == null || DynamicWaypoints.Length == 0)
            {
                CalculateWaypoints();
            }

            if (DynamicWaypoints == null || DynamicWaypoints.Length == 0)
            {
                return;
            }

            GizmosDrawer.DrawSpline(DynamicWaypoints, IsCycled);
        }

        
#endif

        public Vector3 GetDestination(int index)
        {
            if (!IsCycled && index > DynamicWaypoints.Length - 1) index = DynamicWaypoints.Length - 1;
            index %= DynamicWaypoints.Length;
            return DynamicWaypoints[index].Position;
        }

        public Vector3 SelectClosestWaypointPosition(Vector3 position)
        {
            return GetDestination(SelectClosestWaypointIndex(position));
        }

        public int SelectClosestWaypointIndex(Vector3 position)
        {
            // return DynamicWaypoints.Select((x, i) => new {x, i}).OrderBy(a => Vector3.Distance(position, a.x.Position)).First().i; TODO test
            
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
        private static RaycastHit[] Hits = new RaycastHit[5];
        public static WaypointEntity[] CalculateWaypoints(List<WaypointEntity> list, int iterations,
            bool stickToColliders, bool cycled)
        {
            var distances = list.Select((t, i) => (t.Position - list[(i + 1) % list.Count].Position).magnitude)
                .ToList();

            var p = new List<Vector3>();
            
            foreach (var waypointEntity in list)
            {
                //TODO add stickToColliders implementation
                //waypointEntity.Position = waypointEntity.Position;
                
                /*if (stickToColliders)
                {
                    Ray ray = new Ray(waypointEntity.Position + Vector3.up * 5, Vector3.down);
                    RaycastHit[] hits = Physics.RaycastAll(ray);

                    var hit = hits?.Where(x => x.collider != waypointEntity.Collider)?.OrderBy(x => x.distance)?
                        .First();

                    if (hit != null) waypointEntity.Position = hit.Value.point;
                }*/

                p.Add(waypointEntity.Position);
            }

            var curve = new List<WaypointEntity>();

            var points = CalculateNavPoints(cycled, p);

            for (var i = 0; i < points.Length; i++)
            {
                var navPoint = points[i];
                
                curve.Add(list[i]);

                var localIterations = Mathf.CeilToInt(distances[i] * iterations / 10);

                if (i == points.Length-1 && !cycled) continue;
                {
                    for (var j = 1; j < localIterations; j++)
                    {
                        var position = NavPoint.GetPoint(navPoint.PointCenter,
                            navPoint.AbsPerpendicularForward, points[(i + 1) % points.Length].AbsPerpendicularBackward,
                            points[(i + 1) % points.Length].PointCenter, (float) j / localIterations);

                        if (stickToColliders)
                        {
                            Ray ray = new Ray(position + Vector3.up * 1, Vector3.down);
                            var hits = Physics.RaycastAll(ray);
                            
                            if (hits.Length > 0)
                            {
                                var hit = hits.OrderBy(x => x.distance)
                                    .First();

                                position.y = hit.point.y;
                            }
                        }

                        var waypointEntity = new WaypointEntity(position, true);
                        waypointEntity.NavPoint = navPoint;
                        curve.Add(waypointEntity);
                    }
                }
            }

            return curve.ToArray();
        }

        private static NavPoint[] CalculateNavPoints(bool cycled, List<Vector3> p)
        {
            return CalculateNavPoints(cycled, p.ToArray());
        }
        private static NavPoint[] CalculateNavPoints(bool cycled, Vector3[] p)
        {
            var points = new List<NavPoint>();

            //Make closed curve with rounding on every point
            if (cycled)
            {
                points = p.Select((t, i) =>
                    new NavPoint(t,
                        p[(p.Length + i - 1) % p.Length],
                        p[(p.Length + i + 1) % p.Length])).ToList();
            }

            //Or make open curve withoud roundings on utmost points
            else
            {
                for (int i = 0; i < p.Length; i++)
                {
                    var pCenter = p[i];
                    if (p.Length == 1) points.Add(new NavPoint(pCenter, pCenter, pCenter));
                    else
                    {
                        var pForward = i != p.Length - 1 
                            ? p[i + 1] 
                            : Vector3.positiveInfinity;
                        var pBackward = i != 0 
                            ? p[i - 1] 
                            : Vector3.positiveInfinity;
                        points.Add(new NavPoint(pCenter, pBackward, pForward));
                    }
                }
            }

            return points.ToArray();
        }

        private void Init()
        {
            CalculateWaypoints();
            WaypointChanged = new UnityEvent();
        }

        public void AddWaypoint()
        {
            NavigationData.AddWaypoint();
            CalculateWaypoints();
        }

        public void AddWaypoint(int index)
        {
            NavigationData.AddWaypoint(index);
            CalculateWaypoints();
        }

        public void Relocate(int from, int to)
        {
            NavigationData.Relocate(NavigationData.Waypoints, @from, to);
            CalculateWaypoints();
        }

        public void DeleteWaypoint(int i)
        {
            NavigationData.DeleteWaypoint(i);
            CalculateWaypoints();
        }

        public Vector3 GetDestination(float t)
        {
            var wayPoints = _waypoints.ToArray();
            var arrayShift = IsCycled ? 0 : 1;
            int i;
            if (t >= 1)
            {
                t = 1;
                i = wayPoints.Length - 1;
            }
            else
            {
                t = Mathf.Clamp01(t) * (wayPoints.Length - arrayShift);
                i = Mathf.FloorToInt(t);
                t -= i;
            }
            
            return NavPoint.GetPoint(wayPoints[i].NavPoint,
                wayPoints[(i + 1) % wayPoints.Length].NavPoint, t);
        }

        public Vector3 GetVelocity(float t)
        {
            var wayPoints = _waypoints.ToArray();
            var arrayShift = IsCycled ? 0 : 1;
            int i;
            /*if (t <= 0 && !IsCycled)
            {
                t = float.Epsilon;
            }*/
            if (t >= 1)
            {
                t = 1;
                i = wayPoints.Length - 1;
            }
            else
            {
                t = Mathf.Clamp01(t) * (wayPoints.Length - arrayShift);
                i = Mathf.FloorToInt(t);
                t -= i;
            }
            return NavPoint.GetFirstDerivative(wayPoints[i].NavPoint,
                wayPoints[(i + 1) % wayPoints.Length].NavPoint, t);
        }

        public void CalculateWaypoints()
        {
            var navPoints = CalculateNavPoints(IsCycled, NavigationData.Waypoints.Select(x => x.Position).ToArray());
            for (var i = 0; i < NavigationData.Waypoints.Count; i++)
            {
                NavigationData.Waypoints[i].NavPoint = navPoints[i];
            }

            DynamicWaypoints = CalculateWaypoints(_waypoints, Iterations, StickToColliders, IsCycled);
            Colorize();
#if UNITY_EDITOR
            SceneView.RepaintAll();
#endif
        }

        
        public void Colorize()
        {
            var count = DynamicWaypoints.Length;

            for (var index = 0; index < DynamicWaypoints.Length; index++)
            {
                var waypointEntity = DynamicWaypoints[index];
                waypointEntity.LabelColor = GradientForWaypoints.Evaluate((float) index / (count - 1));
            }
        }
    }
}
