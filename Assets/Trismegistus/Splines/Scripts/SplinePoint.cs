using System;
using UnityEngine;

namespace Trismegistus.Splines
{
    /// <summary>
    /// Point with calculated auxiliary points and directions for making a curve
    /// </summary>
    [Serializable]
    public class SplinePoint
    {
        /// <summary>
        /// Current point position
        /// </summary>
        public Vector3 PointCenter;

        /// <summary>
        /// Position of previous point
        /// </summary>
        public Vector3 PointBackward;

        /// <summary>
        /// Position of next point
        /// </summary>
        public Vector3 PointForward;

        /// <summary>
        /// Normalized direction of bisect for angle from points PointBackward-PointCenter-PointForward
        /// </summary>
        public Vector3 Bisector;

        /// <summary>
        /// Local point on a line, perpendicular to bisector from PointCenter, towards PointForward
        /// </summary>
        public Vector3 PerpendicularForward;

        /// <summary>
        /// Local point on a line, perpendicular to bisector from PointCenter, towards PointBackward
        /// </summary>
        public Vector3 PerpendicularBackward;

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
        public SplinePoint(Vector3 pointCenter, Vector3 pointBackward, Vector3 pointForward)
        {
            if (float.IsInfinity(pointBackward.sqrMagnitude) && float.IsInfinity(pointForward.sqrMagnitude))
                throw new ArgumentException("Both points cannot be positiveInfinity");

            PointCenter = pointCenter;

            PointBackward = float.IsInfinity(pointBackward.sqrMagnitude)
                ? pointCenter + (pointCenter - pointForward)
                : pointBackward;

            PointForward = float.IsInfinity(pointForward.sqrMagnitude)
                ? pointCenter + (pointCenter - pointBackward)
                : pointForward;

            var backwardVector = PointBackward - PointCenter;
            var forwardVector = PointForward - PointCenter;

            // Calculation normals
            var mainNormal = Vector3.Cross(backwardVector, forwardVector).normalized;
            var backwardNormal = Vector3.Cross(mainNormal, backwardVector).normalized;
            var forwardNormal = Vector3.Cross(forwardVector, mainNormal).normalized;

            // The lengths of the corresponding shoulder
            var lengthShoulderBackward = 0.5f * backwardVector.magnitude;
            var lengthShoulderForward = 0.5f * forwardVector.magnitude;

            // Rotation (clockwise) angles relative to its shoulder
            var angleRotationShoulderBackward =  0.0f;
            var angleRotationShoulderForward =  0.0f;
            
            // Calculation of radian for shoulders
            var angle = (180f - Mathf.Abs(Vector3.Angle(backwardVector, forwardVector))) / 2;
            var radianShoulderBackward = Mathf.Deg2Rad * (angle - angleRotationShoulderBackward);
            var radianShoulderForward = Mathf.Deg2Rad * (angle + angleRotationShoulderForward);

            PerpendicularBackward = lengthShoulderBackward *
                                    (Mathf.Sin(radianShoulderBackward) * -backwardNormal +
                                     Mathf.Cos(radianShoulderBackward) * backwardVector.normalized)
                                    .normalized;
            PerpendicularForward = lengthShoulderForward *
                                   (Mathf.Sin(radianShoulderForward) * -forwardNormal +
                                    Mathf.Cos(radianShoulderForward) * forwardVector.normalized)
                                   .normalized;

            Bisector = ((PointForward - PointCenter).normalized + (PointBackward - PointCenter).normalized).normalized;
        }

        #region Statics

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
        /// Get velocity of 4-points bezier point
        /// </summary>
        /// <param name="p0">Start of curve</param>
        /// <param name="p1">Start aux point</param>
        /// <param name="p2">End aux point</param>
        /// <param name="p3">End of curve</param>
        /// <param name="t">Normalized position on curve</param>
        /// <returns>Relative velocity (direction*speed)</returns>
        public static Vector3 GetFirstDerivative(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            t = Mathf.Clamp01(t);
            float oneMinusT = 1f - t;
            return
                3f * oneMinusT * oneMinusT * (p1 - p0) +
                6f * oneMinusT * t * (p2 - p1) +
                3f * t * t * (p3 - p2);
        }

        public static Vector3 GetFirstDerivative(SplinePoint firstPoint, SplinePoint secondPoint, float t) =>
            GetFirstDerivative(firstPoint.PointCenter,
                firstPoint.AbsPerpendicularForward,
                secondPoint.AbsPerpendicularBackward,
                secondPoint.PointCenter, t);

        /// <summary>
        /// Get point on bezier by 2 navPoints 
        /// </summary>
        /// <param name="firstPoint"></param>
        /// <param name="secondPoint"></param>
        /// <param name="t">Normalized position on curve</param>
        /// <returns>Absolute position of calculated point</returns>
        public static Vector3 GetPoint(SplinePoint firstPoint, SplinePoint secondPoint, float t) =>
            GetPoint(firstPoint.PointCenter,
                firstPoint.AbsPerpendicularForward,
                secondPoint.AbsPerpendicularBackward,
                secondPoint.PointCenter, t);

        #endregion
    }
}