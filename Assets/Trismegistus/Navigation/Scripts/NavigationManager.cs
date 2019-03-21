using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;

namespace Trismegistus.Navigation
{
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

        public UnityEvent WaypointChanged;
        
        public WaypointEntity[] DynamicWaypoints;

        private List<WaypointEntity> _waypoints => NavigationData.Waypoints;
        private static readonly RaycastHit[] Hits = new RaycastHit[5];
        
        private void Init()
        {
            if (!NavigationData) return; 
            CalculateWaypoints();
            WaypointChanged = new UnityEvent();
        }
        
        #region MonoBehaviour
        void Awake()
        {
            Init();
        }
        

#if UNITY_EDITOR
        void OnDrawGizmos()
        {
            if (!NavigationData) return;
            
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
        #endregion
        
        #region Implementations INavigationManager
        public int WaypointsCount => _waypoints.Count;
        public List<WaypointEntity> Waypoints => _waypoints;
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
        
        public Vector3 GetDestination(float t)
        {
            var newT = t;
            var wayPoints = StickToColliders? DynamicWaypoints : _waypoints.ToArray();
            var arrayShift = IsCycled ? 0 : 1;

            newT = Mathf.Clamp01(newT) * (wayPoints.Length - arrayShift);
            var mult = newT;
            var i = Mathf.FloorToInt(newT);
            newT -= i;
            var pos = NavPoint.GetPoint(wayPoints[i].NavPoint,
                wayPoints[(i + 1) % wayPoints.Length].NavPoint, newT);
            return pos;
        }

        public Vector3 GetVelocity(int index)
        {
            var i = index;
            var wayPoints = DynamicWaypoints;
            return NavPoint.GetFirstDerivative(wayPoints[i].NavPoint,
                       wayPoints[(i + 1) % wayPoints.Length].NavPoint, 0f)*wayPoints.Length;
        }

        public Vector3 GetVelocity(float t)
        {
            var wayPoints = StickToColliders? DynamicWaypoints : _waypoints.ToArray();
            var arrayShift = IsCycled ? 0 : 1;

            t = Mathf.Clamp01(t) * (wayPoints.Length - arrayShift);
            var i = Mathf.FloorToInt(t);
            t -= i;
            
            return NavPoint.GetFirstDerivative(wayPoints[i].NavPoint,
                       wayPoints[(i + 1) % wayPoints.Length].NavPoint, t)*wayPoints.Length;
        }
        #endregion

        #region Reordering

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

        #endregion

        #region Calculations
        public void CalculateWaypoints()
        {
            var navPoints = CalculateNavPoints(IsCycled, NavigationData.Waypoints.Select(x => x.Position).ToArray());
            for (var i = 0; i < NavigationData.Waypoints.Count; i++)
            {
                NavigationData.Waypoints[i].NavPoint = navPoints[i];
            }

            DynamicWaypoints = CalculateWaypoints(_waypoints, Iterations, StickToColliders, IsCycled);
            var dynNavPoints = CalculateNavPoints(IsCycled, DynamicWaypoints.Select(x => x.Position).ToArray());
            for (var i = 0; i < DynamicWaypoints.Length; i++)
            {
                DynamicWaypoints[i].NavPoint = dynNavPoints[i];
            }
            CalculateVelocities();
            Colorize();
#if UNITY_EDITOR
            SceneView.RepaintAll();
#endif
        }
        public static WaypointEntity[] CalculateWaypoints(List<WaypointEntity> list, int iterations,
            bool stickToColliders, bool cycled)
        {
            var distances = list.Select((t, i) => (t.Position - list[(i + 1) % list.Count].Position).magnitude)
                .ToList();

            var p = new List<Vector3>();
            
            foreach (var waypointEntity in list)
            {
                
                //waypointEntity.Position = waypointEntity.Position;
                
                if (stickToColliders)
                {
                    waypointEntity.Position = AdjustYToCollider(waypointEntity.Position);
                }

                p.Add(waypointEntity.Position);
            }

            var curve = new List<WaypointEntity>();

            var baseNavPoints = CalculateNavPoints(cycled, p);
            var arrayShift = cycled ? 1 : 0;
            for (var i = 0; i < baseNavPoints.Length; i++)
            {
                var navPoint = baseNavPoints[i];
                
                //curve.Add(list[i]);

                var localIterations = Mathf.CeilToInt(distances[i] * iterations / 10);

                if (i == baseNavPoints.Length-1 && !cycled) continue;
                {
                    for (var j = 0; j <= localIterations-arrayShift; j++)
                    {
                        var position = NavPoint.GetPoint(navPoint.PointCenter,
                            navPoint.AbsPerpendicularForward, baseNavPoints[(i + 1) % baseNavPoints.Length].AbsPerpendicularBackward,
                            baseNavPoints[(i + 1) % baseNavPoints.Length].PointCenter, (float) j / localIterations);

                        if (stickToColliders)
                        {
                            position = AdjustYToCollider(position);
                        }

                        var waypointEntity = new WaypointEntity(position, true) {NavPoint = navPoint};
                        curve.Add(waypointEntity);
                    }
                }
            }
            
            return curve.ToArray();
        }

        private void CalculateVelocities()
        {
            for (int i = 0; i < DynamicWaypoints.Length; i++)
            {
                var ent = DynamicWaypoints[i];
                ent.Velocity = GetVelocity(i);
            }
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
                /*points = p.Select((t, i) =>
                    new NavPoint(p[i],
                        p[(p.Length + i - 1) % p.Length],
                        p[(p.Length + i + 1) % p.Length])).ToList();*/
                for (int i = 0; i < p.Length; i++)
                {
                    points.Add(new NavPoint(p[i], 
                        p[(p.Length + i - 1) % p.Length],
                        p[(i + 1) % p.Length]));
                }
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
        private static Vector3 AdjustYToCollider(Vector3 pos)
        {
            var ray = new Ray(pos + Vector3.up * 2, Vector3.down);
            var size = Physics.RaycastNonAlloc(ray, Hits);

            /*var hit = hits?.Where(x => x.collider != waypointEntity.Collider)?.OrderBy(x => x.distance)?
                        .First();*/
            
            if (size <= 0) return pos;
            
            var hit = Hits.Take(size).OrderBy(x => x.distance)
                .First();
            pos.y = hit.point.y;

            return pos;
        }
        #endregion
        
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
