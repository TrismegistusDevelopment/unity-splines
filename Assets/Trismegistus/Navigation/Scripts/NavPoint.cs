using System;
using UnityEngine;

namespace Trismegistus.Navigation
{
    /// <summary>
    /// Point with calculated auxiliary points and directions for making a curve
    /// </summary>
    public class NavPoint
    {
        /// <summary>
        /// Current point position
        /// </summary>
        public Vector3 PointCenter { get; }
        /// <summary>
        /// Position of previous point
        /// </summary>
        public Vector3 PointBackward { get; }
        /// <summary>
        /// Position of next point
        /// </summary>
        public Vector3 PointForward { get; }
        /// <summary>
        /// Normalized direction of bisect for angle from points PointBackward-PointCenter-PointForward
        /// </summary>
        public Vector3 Bisector { get; }
        /// <summary>
        /// Local point on a line, perpendicular to bisector from PointCenter, towards PointForward
        /// </summary>
        public Vector3 PerpendicularForward { get; }
        /// <summary>
        /// Local point on a line, perpendicular to bisector from PointCenter, towards PointBackward
        /// </summary>
        public Vector3 PerpendicularBackward { get; }
        /// <summary>
        /// PerpendicularForward in World coordinates
        /// </summary>
        public Vector3 AbsPerpendicularForward => PointCenter + PerpendicularForward;
        /// <summary>
        /// PerpendicularBackward in World coordinates
        /// </summary>
        public Vector3 AbsPerpendicularBackward => PointCenter + PerpendicularBackward;

        /// <summary>
        /// Making new NavPoint with calculated auxiliary points
        /// </summary>
        /// <param name="pointCenter">Target point position</param>
        /// <param name="pointBackward">Prev point position. If there is none, pass Vector3.positiveInfinity</param>
        /// <param name="pointForward">Next point position. If there is none, pass Vector3.positiveInfinity</param>
        /// <exception cref="ArgumentException">Only one non-center can be Vector3.positiveInfinity</exception>
        public NavPoint(Vector3 pointCenter, Vector3 pointBackward, Vector3 pointForward)
        {
            if (pointBackward == Vector3.positiveInfinity && pointForward == Vector3.positiveInfinity)
                throw new ArgumentException("Both points cannot be positiveInfinity");
            
            PointCenter = pointCenter;
            
            PointBackward = pointBackward == Vector3.positiveInfinity
                ? pointCenter + (pointForward - pointCenter) 
                : pointBackward;
            PointForward = pointForward == Vector3.positiveInfinity
                ? pointCenter + (pointBackward - pointCenter) 
                : pointForward;
            
            Bisector = (
                           (pointForward - pointCenter).normalized 
                        + 
                           (pointBackward - pointCenter).normalized
                           )
                       .normalized * 10;
            
            var up = Vector3.Cross(
                pointForward - pointCenter, 
                pointBackward - pointCenter)
                .normalized;
            
            PerpendicularForward =
                - Vector3.Cross(up, Bisector).normalized 
                * (pointForward - PointCenter).magnitude 
                * 0.5f;
            PerpendicularBackward =
                Vector3.Cross(up, Bisector).normalized 
                * (pointBackward - PointCenter).magnitude 
                * 0.5f;
        }
        
        /// <summary>
        /// Get point on bezier by 4 points 
        /// </summary>
        /// <param name="p0">Start of curve</param>
        /// <param name="p1">Start aux point</param>
        /// <param name="p2">End aux point</param>
        /// <param name="p3">End of curve</param>
        /// <param name="t">Normalized position on curve</param>
        /// <returns>Absolute position of calculated point</returns>
        public static Vector3 GetPoint(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            t = Mathf.Clamp01(t);
            var oneMinusT = 1f - t;
            return
                oneMinusT * oneMinusT * oneMinusT * p0 +
                3f * oneMinusT * oneMinusT * t * p1 +
                3f * oneMinusT * t * t * p2 +
                t * t * t * p3;
        }

        /// <summary>
        /// Get point on bezier by 2 navPoints 
        /// </summary>
        /// <param name="firstPoint"></param>
        /// <param name="secondPoint"></param>
        /// <param name="t">Normalized position on curve</param>
        /// <returns>Absolute position of calculated point</returns>
        public static Vector3 GetPoint(NavPoint firstPoint, NavPoint secondPoint, float t) =>
            GetPoint(firstPoint.PointCenter, 
                firstPoint.AbsPerpendicularForward,
                secondPoint.AbsPerpendicularBackward, 
                secondPoint.PointCenter, t);
    }
}