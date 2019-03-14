using System.Collections;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;

namespace Trismegistus.Navigation
{
    public class WaypointBehaviour : MonoBehaviour
    {
        public UnityEvent PlayerReachedThePoint;

        public Vector3 Position { get => transform.position;
            set => transform.position = value;
        }

        public Vector3 EntityPosition;

        public WaypointEntity WaypointEntity = new WaypointEntity();

        public int Index;

        public Color LabelColor => WaypointEntity.LabelColor;

        public string Caption = "";

        public string FullCaption => $"{Index + 1}{(string.IsNullOrEmpty(Caption) ? "" : " ")}{Caption}";

        private bool _blocked;
        private Collider _collider;
        public Collider Collider => _collider ?? (_collider = GetComponent<Collider>());

        private Transform _player;

        void OnTriggerStay(Collider other)
        {
            if (!other.gameObject.CompareTag("Player")) return;
            if (_blocked) return;
            /*if (!(Vector2.Distance(other.transform.position.IgnoreYCoordinate(),
                          transform.position.IgnoreYCoordinate()) < 0.5f)) return;*/
            PlayerReachedThePoint.Invoke();
            _player = other.transform;
            StartCoroutine(WaitForPlayerToLeave());
        }

        void OnDrawGizmos()
        {
#if UNITY_EDITOR
            var sign = Mathf.Sign(Position.y);
            var distance = Position.y * sign;
            var ray = new Ray(Position, -Vector3.up * sign);
            var raycastHits = Physics.RaycastAll(ray, Position.y * sign);

            if (raycastHits.Length > 0)
            {
                var hit = raycastHits?.Where(x => x.collider != Collider)?.OrderBy(x => x.distance)?
                    .First();
                distance = hit.Value.distance;
            }

            Handles.color = LabelColor;

            if (sign > 0) Handles.DrawLine(Position, Position - Vector3.up * distance);
            else
            {
                Handles.DrawDottedLine(Position, Position + Vector3.up * distance, 2);
            }

            var col = LabelColor;
            col.a = 0.3f;
            Handles.color = col;
            Handles.DrawSolidDisc(Position - Vector3.up * sign * distance, Vector3.up, 0.5f);
#endif
        }

        //void OnCollisionEnter(Collision other)
        //{
        //    if (other.gameObject.CompareTag("Player"))
        //    {
        //        PlayerReachedThePoint.Invoke();
        //        player = other.transform;
        //        StartCoroutine(WaitForPlayerToLeave());
        //    }
        //}

        private IEnumerator WaitForPlayerToLeave()
        {
            _blocked = true;
            while (true)
            {
                if (Vector3.Distance(_player.position, transform.position) < 2f)
                {
                    yield return null;
                }
                else break;
            }
            _blocked = false;
        }

        private Vector2 ProjectOnXZPlane(Vector3 source)
        {
            return new Vector2(source.x, source.z);
        }
    }
}

