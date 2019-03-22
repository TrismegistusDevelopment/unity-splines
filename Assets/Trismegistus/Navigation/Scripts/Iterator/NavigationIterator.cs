using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Trismegistus.Navigation.Iterator
{
    public class NavigationIterator : MonoBehaviour, INavigationIterator, IEnumerator<Vector3>
    {
        public bool MoveNext()
        {
            _index++;
            
            if (_index >= _manager.WaypointsCount) return false;
            
            Current = _manager.GetDestination(_index);
            return true;
        }

        public void Reset()
        {
            _index = -1;
            StartCheck();
        }

        public Vector3 Current { get; private set; }

        object IEnumerator.Current => Current;

        public void Dispose()
        {
            throw new System.NotImplementedException();
        }

        public float StoppingDistance { get; private set; }
        public UnityEvent DestinationChanged { get; } = new UnityEvent();
        public Vector3 Destination => Current;
        public void SetStoppingDistance(float distance) => StoppingDistance = distance;

        public void SetNavigationManager(INavigationManager manager) => _manager = manager;

        private int _index = -1;
        private INavigationManager _manager;
        private Coroutine _checkDistanceCoroutine;

        void Start()
        {
            MoveNext();
            StartCheck();
        }

        private IEnumerator CheckDistanceEnumerator()
        {
            while ((transform.position - Destination).sqrMagnitude > StoppingDistance * StoppingDistance)
            {
                yield return new WaitForEndOfFrame();
            }
            OnDestinationReached();
        }

        private void OnDestinationReached()
        {
            if (MoveNext())
            {
                StartCheck();
            }
            else
            {
                Reset();
                MoveNext();
            }
            DestinationChanged.Invoke();
        }

        private void StartCheck()
        {
            StopCheck();
            _checkDistanceCoroutine = StartCoroutine(CheckDistanceEnumerator());
        }

        private void StopCheck()
        {
            if (_checkDistanceCoroutine != null)
                StopCoroutine(_checkDistanceCoroutine);
        }
    }
}