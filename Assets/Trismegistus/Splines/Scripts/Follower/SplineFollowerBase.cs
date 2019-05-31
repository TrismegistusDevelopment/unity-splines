using System;
using UnityEngine;

namespace Trismegistus.Splines.Follower
{
    public enum FollowerMode
    {
        None,
        Loop,
        Once,
        PingPong
    }
    public abstract class SplineFollowerBase : MonoBehaviour, ISplineFollower
    {
        protected ISplineManager Manager;
        protected int CurrentIndex;
        [SerializeField] protected FollowerMode mode;
        #region Implementations INavigationFollower 
        public abstract void StartMoving();

        public abstract void StopMoving();
        
        public void SetManager(ISplineManager manager) => Manager = manager;

        public void RecalculateNextPoint() => CurrentIndex = Manager.SelectClosestWaypointIndex(transform.position);

        public Vector3 GetCurrentDestination() => Manager?.GetParams(CurrentIndex).Destination ?? 
                                                  throw new NullReferenceException("Manager is null");
        #endregion
    }
}