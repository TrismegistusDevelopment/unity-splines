using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;

namespace Trismegistus.Navigation
{
    public class WaypointBehaviour : MonoBehaviour
    {
        public UnityEvent PlayerReachedThePoint;

        public Vector3 Position { get { return transform.position; } set { transform.position = value; } }

        public Vector3 EntityPosition;

        public WaypointEntity WaypointEntity = new WaypointEntity();

        public int Index;

        public Color LabelColor => WaypointEntity.LabelColor;

        public string Caption = "";

        public string FullCaption
        {
            get
            {
                var separator = string.IsNullOrEmpty(Caption) ? "" : " ";
                var text = $"{Index + 1}{separator}{Caption}";
                return text;
            }
        }

        private bool _blocked;
        private Collider _collider;
        public Collider Collider => _collider ?? (_collider = GetComponent<Collider>());

        private Transform _player;

        void OnTriggerStay(Collider other)
        {
            if (other.gameObject.CompareTag("Player"))
            {
                if (_blocked) return;
                if (!(Vector2.Distance(ProjectOnXZPlane(other.transform.position),
                          ProjectOnXZPlane(transform.position)) < 0.1f)) return;
                PlayerReachedThePoint.Invoke();
                _player = other.transform;
                StartCoroutine(WaitForPlayerToLeave());
            }
        }

        void OnDrawGizmos()
        {
#if UNITY_EDITOR
            var sign = Mathf.Sign(Position.y);
            float distance = Position.y * sign;
            Ray ray = new Ray(Position, -Vector3.up * sign);
            RaycastHit[] raycastHits = Physics.RaycastAll(ray, Position.y * sign);

            if (raycastHits.Length > 0)
            {
                RaycastHit? hit = raycastHits.Where(x => x.collider != Collider)
                    .OrderBy(x => x.distance)
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

