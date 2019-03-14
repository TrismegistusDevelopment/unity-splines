using System;
using System.Collections;
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

        /// <summary>
        /// Move element within IList
        /// </summary>
        /// <param name="list">Any IList</param>
        /// <param name="indexFrom">Index of moving item, must be within list.Count</param>
        /// <param name="indexTo">Desired index for item, must be within list.Count</param>
        /// <typeparam name="T">Type of IList</typeparam>
        /// <returns>Rearranged IList of type T</returns>
        /// <exception cref="ArgumentException">Both indexes must be within list.Count</exception>
        public static T Relocate<T>(T list, int indexFrom, int indexTo) where T : IList
        {
            if (indexFrom == indexTo) return list;
            if (indexTo < 0 || 
                indexFrom < 0 || 
                indexTo >= list.Count - 1 ||
                indexFrom >= list.Count - 1) throw new ArgumentException();
            
            var item = list[indexFrom];
            list.RemoveAt(indexFrom);
            list.Insert(indexTo > indexFrom? indexTo - 1: indexTo, item);
            list.
            return list;
        }
    }
}
