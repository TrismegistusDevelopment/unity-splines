using System.Collections.Generic;
using UnityEngine;

namespace Trismegistus.Navigation
{
    public interface INavigationManager
    {
        int WaypointsCount { get; }
        List<WaypointEntity> Waypoints { get; }

        Vector3 GetDestination(int index);
        Vector3 GetDestination(float t);
        Vector3 GetVelocity(int index);
        Vector3 GetVelocity(float t);
        int SelectClosestWaypointIndex(Vector3 position);
        Vector3 SelectClosestWaypointPosition(Vector3 position);
    }
}