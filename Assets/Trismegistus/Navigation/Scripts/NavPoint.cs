using UnityEngine;

namespace Trismegistus
{
    public class NavPoint
    {
        public Vector3 PointCenter { get; }
        public Vector3 PointBackward { get; }
        public Vector3 PointForward { get; }
        public Vector3 Bisector { get; }
        public Vector3 PerpendicularForward { get; }
        public Vector3 PerpendicularBackward { get; }
        public Vector3 AbsPerpendicularForward => PointCenter + PerpendicularForward;
        public Vector3 AbsPerpendicularBackward => PointCenter + PerpendicularBackward;

        public NavPoint(Vector3 pointCenter, Vector3 pointBackward, Vector3 pointForward)
        {
            PointCenter = pointCenter;
            PointBackward = pointBackward;
            PointForward = pointForward;
            
            Bisector = ((pointForward - pointCenter).normalized + (pointBackward - pointCenter).normalized).normalized * 10;
            var up = Vector3.Cross(pointForward - pointCenter, pointBackward - pointCenter).normalized;
            PerpendicularForward =
                -Vector3.Cross(up, Bisector).normalized * (pointForward - PointCenter).magnitude * 0.5f;
            PerpendicularBackward =
                Vector3.Cross(up, Bisector).normalized * (pointBackward - PointCenter).magnitude * 0.5f;
        }

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
    }
}