using UnityEngine;

namespace Trismegistus.Navigation.Follower
{
    public interface INavigationFollower
    {
        void SetManager(INavigationManager manager);
        void StartMoving();
        void StopMoving();
        Vector3 GetCurrentDestination();
        void RecalculateNextPoint();
    }
}