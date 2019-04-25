using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Trismegistus.Navigation.Iterator
{
    public interface INavigationIterator
    {
        float StoppingDistance { get; }
        UnityEvent DestinationChanged { get; }
        Vector3 Destination { get; }
        void SetStoppingDistance(float distance);
        void SetNavigationManager(INavigationManager manager);
    }

    public interface INavigationIteratorCreator
    {
        INavigationIterator GetNavigationIterator(GameObject targetGameObject, float stoppingDistance);
    }
}