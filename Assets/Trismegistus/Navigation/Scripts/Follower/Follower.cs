using System.Collections;
using UnityEditor;
using UnityEngine;

namespace Trismegistus.Navigation.Follower
{
    public class Follower : NavigationFollowerBase
    {
        [SerializeField] private NavigationManager manager;
        [SerializeField] [Range(0, 500)] private float speed = 1;
        [SerializeField] private bool followRotation;
        
        private Coroutine _movingCoroutine;
        private float _f;

        #region Monobehaviour
        void Start()
        {
            Manager = manager;
            StartMoving();
        }

        void OnGUI()
        {
            if (GUI.Button(new Rect(10, 10, 100, 30), "Start"))
            {
                StartMoving();
            }
            
            if (GUI.Button(new Rect(110, 10, 100, 30), "Stop"))
            {
                StopMoving();
            }
        }
        
        #if UNITY_EDITOR
        void OnDrawGizmos()
        {
            Handles.Label(transform.position, $"{_f}");
        }
        #endif
        #endregion
        
        #region Implementations INavigationFollower 
        public override void StartMoving()
        {
            if (_movingCoroutine!=null) StopCoroutine(_movingCoroutine);
            _movingCoroutine = StartCoroutine(MovingEnumerator(mode));
        }

        public override void StopMoving()
        {
            if (_movingCoroutine!=null) StopCoroutine(_movingCoroutine);
        }
        #endregion

        private IEnumerator MovingEnumerator(FollowerMode followerMode = FollowerMode.Loop)
        {
            if (followerMode == FollowerMode.None) yield break;
            _f = 0;
            var sign = 1;
            while (true)
            {
                transform.position = Manager.GetDestination(_f);

                var velocity = Manager.GetVelocity(_f);
                if (followRotation) transform.rotation = Quaternion.LookRotation(velocity);
                _f += Time.deltaTime * speed / velocity.magnitude * sign;
                if (_f > 1 || _f < 0)
                {
                    switch (followerMode)
                    {
                        case FollowerMode.Loop:
                            _f = 0;
                            break;
                        case FollowerMode.Once:
                            yield break;
                        case FollowerMode.PingPong:
                            sign *= -1;
                            break;
                    }
                }
                yield return new WaitForEndOfFrame();
            }
        }
    }
}