using System;
using UnityEngine;

namespace Trismegistus.Navigation
{
    [Serializable]
    public class WaypointEntity
    {
        public Vector3 Position;
        public Quaternion Rotation = Quaternion.identity;
        public bool IsTemp;
        public Color LabelColor;
        public string Caption;
        public NavPoint NavPoint;

        public WaypointEntity()
        {
        }

        public WaypointEntity(Vector3 position, bool isTemp, Color labelColor, string caption)
        {
            Position = position;
            IsTemp = isTemp;
            LabelColor = labelColor;
            Caption = caption;
        }
        
        public WaypointEntity(Vector3 position, bool isTemp, Color labelColor, string caption, Quaternion rotation)
        {
            Position = position;
            IsTemp = isTemp;
            LabelColor = labelColor;
            Caption = caption;
            Rotation = rotation;
        }
        
        public WaypointEntity(Vector3 position, bool isTemp, Color labelColor, string caption, Quaternion rotation, NavPoint navPoint)
        {
            Position = position;
            IsTemp = isTemp;
            LabelColor = labelColor;
            Caption = caption;
            Rotation = rotation;
            NavPoint = navPoint;
        }

        public WaypointEntity(Vector3 position, bool isTemp)
        {
            Position = position;
            IsTemp = isTemp;
        }
    }
}