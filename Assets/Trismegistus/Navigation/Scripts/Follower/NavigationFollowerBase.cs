using System;
using UnityEngine;

namespace Trismegistus.Navigation.Follower
{
    public abstract class NavigationFollowerBase : MonoBehaviour, INavigationFollower
    {
        protected INavigationManager Manager;
        protected int CurrentIndex;
        
        public abstract void StartMoving();

        public abstract void StopMoving();
        
        public void SetManager(INavigationManager manager) => Manager = manager;

        public void RecalculateNextPoint() => CurrentIndex = Manager.SelectClosestWaypointIndex(transform.position);

        public Vector3 GetCurrentDestination() => Manager?.GetDestination(CurrentIndex) ?? 
                                                  throw new NullReferenceException("_manager is null");
    }
}