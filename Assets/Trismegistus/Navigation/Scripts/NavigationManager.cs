using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;

namespace Trismegistus.Navigation
{
    public interface INavigationManager
    {
        Vector3 CurrentWaypointPosition { get; }
        bool AllTargetsWalked { get; }
        int WaypointsCount { get; }
        List<WaypointEntity> Waypoints { get; }

        Vector3 GetDestination(int index);
        void SwitchToTheNextWaypoint();
        int SelectClosestWaypointIndex(Vector3 position);
        Vector3 SelectClosestWaypointPosition(Vector3 position);
    }
    
    public class NavigationManager : MonoBehaviour, INavigationManager
    {
        public NavigationData NavigationData;
        
        public static NavigationManager Instance {get; private set; }

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

        public WaypointBehaviour WaypointPrefab
        {
            get => NavigationData.WaypointPrefab;
            set => NavigationData.WaypointPrefab = value;
        }

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

        public Vector3 CurrentWaypointPosition =>
            _waypoints[_currentWaypointIndex >= _waypoints.Count
                ? _waypoints.Count - 1
                : _currentWaypointIndex].Position;

        public bool AllTargetsWalked => _currentWaypointIndex == _waypoints.Count;

        public int WaypointsCount => _waypoints.Count;

        public List<WaypointEntity> Waypoints => _waypoints;
        
        public UnityEvent WaypointChanged;
        
        public WaypointEntity[] DynamicWaypoints;

        private List<WaypointEntity> _waypoints => NavigationData.Waypoints;
        private int _currentWaypointIndex;
        
        void Awake()
        {
            Instance = this;
            Init();
        }
        

#if UNITY_EDITOR
        void OnDrawGizmos()
        {
            for (var index = 0; index < _waypoints.Count; index++)
            {
                GizmosDrawer.DrawWaypointEntity(_waypoints[index], index);
            }

            var dwps = DynamicWaypoints;
            if (dwps == null || dwps.Length == 0)
            {
                //CalculateWaypoints();
            }

            if (dwps == null || dwps.Length == 0)
            {
                //return;
            }

            GizmosDrawer.DrawSpline(dwps, IsCycled);
        }

        
#endif

        public Vector3 GetDestination(int index)
        {
            if (!IsCycled && index > DynamicWaypoints.Length - 1) index = DynamicWaypoints.Length - 1;
            index %= DynamicWaypoints.Length;
            return DynamicWaypoints[index].Position;
        }

        public void SwitchToTheNextWaypoint()
        {
            if (IsCycled)
            {
                _currentWaypointIndex = (_currentWaypointIndex + 1) % _waypoints.Count;
            }
            else if (_currentWaypointIndex < _waypoints.Count)
            {
                _currentWaypointIndex++;
            }
            
            Debug.LogFormat($"Waypoint Changed! CurrentWaypointIndex: {_currentWaypointIndex}");
            WaypointChanged.Invoke();
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

        public static WaypointEntity[] CalculateWaypoints(List<WaypointEntity> list, int iterations,
            WaypointBehaviour prefab, Transform tr, bool stickToColliders, bool cycled)
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
                //list[i] = new WaypointEntity(list[i].Position, false, Color.black, list[i].Caption);
                curve.Add(list[i]);

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

                            var hit = hits?.OrderBy(x => x.distance)?
                                .First();

                            if (hit != null) position.y = hit.Value.point.y;
                        }

                        curve.Add(new WaypointEntity(position, true));
                    }
                }
            }

            return curve.ToArray();
        }

        private void Init()
        {
            _currentWaypointIndex = 0;
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
            NavigationData.Relocate(NavigationData.Waypoints, from, to);
            CalculateWaypoints();
        }

        public void DeleteWaypoint(int i)
        {
            NavigationData.DeleteWaypoint(i);
            CalculateWaypoints();
        }

        public void CalculateWaypoints()
        {
            DynamicWaypoints = CalculateWaypoints(_waypoints, Iterations, WaypointPrefab, transform, StickToColliders, IsCycled);
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
