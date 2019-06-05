using UnityEngine;

namespace Trismegistus.Splines.Follower
{
    public interface ISplineFollower
    {
        void SetManager(ISplineManager manager);
        void StartMoving();
        void StopMoving();
        Vector3 GetCurrentDestination();
        void RecalculateNextPoint();
    }
}