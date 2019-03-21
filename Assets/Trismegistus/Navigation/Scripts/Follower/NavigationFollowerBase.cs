using System;
using UnityEngine;

namespace Trismegistus.Navigation.Follower
{
    public enum FollowerMode
    {
        None,
        Loop,
        Once,
        PingPong
    }
    public abstract class NavigationFollowerBase : MonoBehaviour, INavigationFollower
    {
        protected INavigationManager Manager;
        protected int CurrentIndex;
        [SerializeField] protected FollowerMode mode;
        #region Implementations INavigationFollower 
        public abstract void StartMoving();

        public abstract void StopMoving();
        
        public void SetManager(INavigationManager manager) => Manager = manager;

        public void RecalculateNextPoint() => CurrentIndex = Manager.SelectClosestWaypointIndex(transform.position);

        public Vector3 GetCurrentDestination() => Manager?.GetDestination(CurrentIndex) ?? 
                                                  throw new NullReferenceException("Manager is null");
        #endregion
    }
}