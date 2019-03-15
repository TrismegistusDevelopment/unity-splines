using System;
using UnityEngine;

namespace Trismegistus.Navigation
{
    [Serializable]
    public class WaypointEntity
    {
        public Vector3 Position;
        public bool IsTemp;
        public Color LabelColor;
        public string Caption;

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

        public WaypointEntity(Vector3 position, bool isTemp)
        {
            Position = position;
            IsTemp = isTemp;
        }
    }
}