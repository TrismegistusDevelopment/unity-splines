using System;
using System.Collections.Generic;
using UnityEngine;

namespace Trismegistus.Navigation
{
    [Serializable]
    [CreateAssetMenu(fileName = "New NavigationData", menuName = "Trismegistus/Navigation Data", order = 51)]
    public class NavigationData : ScriptableObject
    {
        public WaypointBehaviour WaypointPrefab;
        public Gradient GradientForWaypoints = new Gradient();
        public bool IsCycled;
        public bool StickToColliders;
        public int Iterations = 20;
        public List<WaypointBehaviour> Waypoints { get; private set; }
    }
}
