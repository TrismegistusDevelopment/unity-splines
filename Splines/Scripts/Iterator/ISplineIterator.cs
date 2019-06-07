using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Trismegistus.Splines.Iterator
{
    public interface ISplineIterator
    {
        float StoppingDistance { get; }
        UnityEvent DestinationChanged { get; }
        Vector3 Destination { get; }
        void SetStoppingDistance(float distance);
        void SetNavigationManager(ISplineManager manager);
    }

    public interface ISplineIteratorCreator
    {
        ISplineIterator GetNavigationIterator(GameObject targetGameObject, float stoppingDistance);
    }
}