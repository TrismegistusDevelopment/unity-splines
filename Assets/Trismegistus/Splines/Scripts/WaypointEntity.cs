﻿using System;
using UnityEngine;

namespace Trismegistus.Splines
{
    [Serializable]
    public class WaypointEntity
    {
        public Vector3 Position;
        public Vector3 Velocity;
        public Quaternion Rotation = Quaternion.identity;
        public bool IsTemp;
        public Color LabelColor;
        public string Caption;
        public SplinePoint splinePoint;

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
        
        public WaypointEntity(Vector3 position, bool isTemp, Color labelColor, string caption, Quaternion rotation, SplinePoint splinePoint)
        {
            Position = position;
            IsTemp = isTemp;
            LabelColor = labelColor;
            Caption = caption;
            Rotation = rotation;
            this.splinePoint = splinePoint;
        }

        public WaypointEntity(Vector3 position, bool isTemp)
        {
            Position = position;
            IsTemp = isTemp;
        }
    }
}