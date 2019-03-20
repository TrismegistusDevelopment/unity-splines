using System.Collections;
using UnityEngine;

namespace Trismegistus.Navigation.Follower
{
    public class Follower : NavigationFollowerBase
    {
        [SerializeField] private NavigationManager manager;
        [SerializeField] [Range(0, 500)] private float speed = 1;
        
        private Coroutine _movingCoroutine;
        private const float MinSquareDistance = 0.1f;

        void Start()
        {
            Manager = manager;
            StartMoving();
        }
        
        public override void StartMoving()
        {
            if (_movingCoroutine!=null) StopCoroutine(_movingCoroutine);
            _movingCoroutine = StartCoroutine(MovingEnumerator());
        }

        public override void StopMoving()
        {
            if (_movingCoroutine!=null) StopCoroutine(_movingCoroutine);
        }

        private IEnumerator MovingEnumerator()
        {
            var t = transform;
            while (true)
            {
                var destination = GetCurrentDestination();
                var tPosition = t.position;
                var startSqrDist = Vector3.SqrMagnitude(tPosition - destination);
                var currentSqrDist = Vector3.SqrMagnitude(tPosition - destination);

                var startRotation = t.rotation;
                var direction = (destination - transform.position).normalized;
                var targetRotation = Quaternion.LookRotation(direction);
                
                while (currentSqrDist > MinSquareDistance)
                {
                    var cross = Vector3.Cross(direction, t.forward);
                    /*t.Rotate(cross, Vector3.SignedAngle(t.forward, direction, cross) * 0.1f, Space.World);*/
                    t.rotation = Quaternion.Lerp(startRotation, targetRotation, Mathf.SmoothStep(0,1,(startSqrDist-currentSqrDist) / startSqrDist));
                    
                    t.Translate(direction * Time.deltaTime * speed, Space.World);
                    
                    yield return new WaitForEndOfFrame();
                    currentSqrDist = Vector3.SqrMagnitude(t.position - destination);
                }

                CurrentIndex++;
                yield return new WaitForEndOfFrame();
            }
        } 
    }
}