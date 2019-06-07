using System.Collections.Generic;
using System.Linq;
using Trismegistus.Splines.Iterator;
using UnityEditor;
using UnityEngine;

namespace Trismegistus.Splines
{
    public struct PointParams
    {
        public Vector3 Destination;
        public Vector3 Velocity;

        public PointParams(Vector3 destination, Vector3 velocity)
        {
            Destination = destination;
            Velocity = velocity;
        }
    }
    public class SplineManager : MonoBehaviour, ISplineManager, ISplineIteratorCreator
    {
        public SplineData splineData;

        public bool IsCycled
        {
            get => splineData.IsCycled;
            set => splineData.IsCycled = value;
        }

        public bool StickToColliders
        {
            get => splineData.StickToColliders;
            set => splineData.StickToColliders = value;
        }

        public Gradient GradientForWaypoints
        {
            get => splineData.GradientForWaypoints;
            set => splineData.GradientForWaypoints = value;
        }
        
        public int Iterations
        {
            get => splineData.Iterations;
            set => splineData.Iterations = value;
        }

        public LayerMask LayerMask
        {
            get => splineData.LayerMask;
            set => splineData.LayerMask = value;
        }

        public WaypointEntity[] DynamicWaypoints;

        private List<WaypointEntity> _waypoints => splineData.Waypoints;
        private static readonly RaycastHit[] Hits = new RaycastHit[5];
        
        private void Init()
        {
            if (!splineData) return; 
            CalculateWaypoints();
        }
        
        #region MonoBehaviour
        void Awake()
        {
            Init();
        }
        

#if UNITY_EDITOR
        void OnDrawGizmos()
        {
            if (!splineData) return;
            
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
        
        #region Implementations 
        #region INavigationManager
        public int Count => DynamicWaypoints.Length;
        public List<WaypointEntity> Entities => _waypoints;
        

        public Vector3 SelectClosestWaypointPosition(Vector3 position)
        {
            return GetParams(SelectClosestWaypointIndex(position)).Destination;
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

        public PointParams GetParams(float t)
        {
            var wayPoints = StickToColliders? DynamicWaypoints : _waypoints.ToArray();
            var arrayShift = IsCycled ? 0 : 1;

            t = Mathf.Clamp01(t) * (wayPoints.Length - arrayShift);
            var i = Mathf.FloorToInt(t);
            t -= i;
            
            var pos = SplinePoint.GetPoint(wayPoints[i].splinePoint,
                wayPoints[(i + 1) % wayPoints.Length].splinePoint, t);
            var destination = pos;
            
            var velocity = SplinePoint.GetFirstDerivative(wayPoints[i].splinePoint,
                       wayPoints[(i + 1) % wayPoints.Length].splinePoint, t)*wayPoints.Length;
            
            return new PointParams(destination, velocity);
        }
        
        public PointParams GetParams(int entity)
        {
            if (!IsCycled && entity > DynamicWaypoints.Length - 1) entity = DynamicWaypoints.Length - 1;
            entity %= DynamicWaypoints.Length;
            var destination = DynamicWaypoints[entity].Position;
            
            var i = entity;
            var wayPoints = DynamicWaypoints;
            var velocity =  SplinePoint.GetFirstDerivative(wayPoints[i].splinePoint,
                       wayPoints[(i + 1) % wayPoints.Length].splinePoint, 0f)*wayPoints.Length;
            
            return new PointParams(destination, velocity);
        }
        #endregion
        
        public ISplineIterator GetNavigationIterator(GameObject targetGameObject, float stoppingDistance)
        {
            var navigationIterator = targetGameObject.AddComponent<SplineIterator>();
            navigationIterator.SetStoppingDistance(stoppingDistance);
            navigationIterator.SetNavigationManager(this);
            return navigationIterator;
        }
        
        #endregion

        #region Reordering

        public void AddPoint(int? index = null, Vector3? position = null)
        {
            if (index == null)
                splineData.AddPoint(position);
            else
                splineData.AddPoint(index.Value, position);
            CalculateWaypoints();
        }

        public void Relocate(int from, int to)
        {
            SplineData.Relocate(splineData.Waypoints, @from, to);
            CalculateWaypoints();
        }

        public void Delete(int i)
        {
            splineData.DeleteWaypoint(i);
            CalculateWaypoints();
        }

        #endregion

        #region Calculations
        public void CalculateWaypoints()
        {
            var navPoints = CalculateNavPoints(IsCycled, splineData.Waypoints.Select(x => x.Position).ToArray());
            for (var i = 0; i < splineData.Waypoints.Count; i++)
            {
                splineData.Waypoints[i].splinePoint = navPoints[i];
            }

            DynamicWaypoints = CalculateWaypoints(_waypoints, Iterations, StickToColliders, IsCycled, LayerMask);
            var dynNavPoints = CalculateNavPoints(IsCycled, DynamicWaypoints.Select(x => x.Position).ToArray());
            for (var i = 0; i < DynamicWaypoints.Length; i++)
            {
                DynamicWaypoints[i].splinePoint = dynNavPoints[i];
            }
            CalculateVelocities();
            Colorize();
#if UNITY_EDITOR
            SceneView.RepaintAll();
#endif
        }
        public static WaypointEntity[] CalculateWaypoints(List<WaypointEntity> list, int iterations,
            bool stickToColliders, bool cycled, LayerMask layerMask)
        {
            var distances = list.Select((t, i) => (t.Position - list[(i + 1) % list.Count].Position).magnitude)
                .ToList();

            var p = new List<Vector3>();
            
            foreach (var waypointEntity in list)
            {
                if (stickToColliders)
                {
                    waypointEntity.Position = AdjustYToCollider(waypointEntity.Position, layerMask);
                }

                p.Add(waypointEntity.Position);
            }
            
            var curve = new List<WaypointEntity>();

            var baseNavPoints = CalculateNavPoints(cycled, p);
            var arrayShift = cycled ? 1 : 0;
            for (var i = 0; i < baseNavPoints.Length; i++)
            {
                var navPoint = baseNavPoints[i];
                
                var localIterations = Mathf.CeilToInt(distances[i] * iterations / 10);

                if (i == baseNavPoints.Length-1 && !cycled) continue;
                {
                    for (var j = 0; j <= localIterations-arrayShift; j++)
                    {
                        var position = SplinePoint.GetPoint(navPoint.PointCenter,
                            navPoint.AbsPerpendicularForward, baseNavPoints[(i + 1) % baseNavPoints.Length].AbsPerpendicularBackward,
                            baseNavPoints[(i + 1) % baseNavPoints.Length].PointCenter, (float) j / localIterations);

                        if (stickToColliders)
                        {
                            position = AdjustYToCollider(position, layerMask);
                        }

                        var waypointEntity = new WaypointEntity(position, true) {splinePoint = navPoint};
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
                ent.Velocity = GetParams(i).Velocity;
            }
        }
        
        private static SplinePoint[] CalculateNavPoints(bool cycled, List<Vector3> p)
        {
            return CalculateNavPoints(cycled, p.ToArray());
        }
        private static SplinePoint[] CalculateNavPoints(bool cycled, Vector3[] p)
        {
            var points = new List<SplinePoint>();

            //Make closed curve with rounding on every point
            if (cycled)
            {
                /*points = p.Select((t, i) =>
                    new NavPoint(p[i],
                        p[(p.Length + i - 1) % p.Length],
                        p[(p.Length + i + 1) % p.Length])).ToList();*/
                for (int i = 0; i < p.Length; i++)
                {
                    points.Add(new SplinePoint(p[i], 
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
                    if (p.Length == 1) points.Add(new SplinePoint(pCenter, pCenter, pCenter));
                    else
                    {
                        var pForward = i != p.Length - 1 
                            ? p[i + 1] 
                            : Vector3.positiveInfinity;
                        var pBackward = i != 0 
                            ? p[i - 1] 
                            : Vector3.positiveInfinity;
                        points.Add(new SplinePoint(pCenter, pBackward, pForward));
                    }
                }
            }

            return points.ToArray();
        }

        
        private static Vector3 AdjustYToCollider(Vector3 pos, LayerMask layerMask)
        {
            var ray = new Ray(Vector3.up * 2 +pos, Vector3.down);
            var size = Physics.RaycastNonAlloc(ray, Hits, float.PositiveInfinity, layerMask);

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

        public void Delete(WaypointEntity entity)
        {
            _waypoints.Remove(entity);
        }
    }
}
