using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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

        [SerializeField]
        public List<WaypointEntity> Waypoints = new List<WaypointEntity>();
        
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
                indexTo > list.Count||
                indexFrom > list.Count - 1) throw new ArgumentException($"indexFrom: {indexFrom}, indexTo: {indexTo}");
            
            var item = list[indexFrom];
            list.RemoveAt(indexFrom);
            list.Insert(indexTo > indexFrom? indexTo - 1: indexTo, item);
            
            return list;
        }

        public void AddWaypoint()
        {
            AddWaypoint(Waypoints.Count);
        }

        public void AddWaypoint(int index)
        {
            index = Mathf.Clamp(index, 0, Waypoints.Count);
            Vector3 pos;
            if (Waypoints.Count == 0) pos = Vector3.zero;
            else
            {
                if (!IsCycled && index == Waypoints.Count)
                    pos = Waypoints.Last().Position;
                else
                    pos = Vector3.Lerp(Waypoints[(index - 1 + Waypoints.Count) % Waypoints.Count].Position,
                        Waypoints[(index + Waypoints.Count)% Waypoints.Count].Position,
                        0.5f);
            } 
            
            if (index == Waypoints.Count)
            {
                Waypoints.Add(new WaypointEntity(pos, false));
                return;
            }
            Waypoints.Insert(index, new WaypointEntity(pos, false));
        }

        public void DeleteWaypoint(int index)
        {
            index = Mathf.Clamp(index, 0, Waypoints.Count - 1);
            Waypoints.RemoveAt(index);
        }
    }
}
