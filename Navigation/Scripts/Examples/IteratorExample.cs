using UnityEngine;

namespace Trismegistus.Navigation.Iterator.Examples
{
    public class IteratorExample : MonoBehaviour
    {
        [SerializeField] private NavigationManager manager;
        [SerializeField] private Material material;
        [SerializeField] [Range(0, 100)] private float stoppingDistance = 5;
        
        private INavigationIteratorCreator Creator => manager;
        private INavigationIterator _iterator;
        private Vector3 _destination = Vector3.zero;
        private LineRenderer _lineRenderer;
        private GameObject _sphere;

        void Awake()
        {
            _iterator = Creator.GetNavigationIterator(gameObject, stoppingDistance);
            _iterator.DestinationChanged.AddListener(React);
            SetupLine();
            PlaceSphere();
        }

        private void SetupLine()
        {
            _lineRenderer = gameObject.AddComponent<LineRenderer>();
            
            _lineRenderer.material = material;
            var gradient = new Gradient();
            gradient.SetKeys(new[]
            {
                new GradientColorKey(Color.grey, 0),
                new GradientColorKey(Color.black, 0.5f),
                new GradientColorKey(Color.grey, 1f)
            }, new[]
            {
                new GradientAlphaKey(0f, 0),
                new GradientAlphaKey(0.8f, 0.5f),
                new GradientAlphaKey(0f, 1)
            });
            _lineRenderer.colorGradient = gradient;
            
            _lineRenderer.numCornerVertices = 2;
            _lineRenderer.widthMultiplier = 0.1f;
            _lineRenderer.loop = true;
        }

        void Update()
        {
            UpdateLine();
        }
        private void UpdateLine()
        {
            var segments = 24;
            var pos = _destination;
            _lineRenderer.positionCount = segments;
            var circlePoint = pos + Vector3.ProjectOnPlane(transform.position - _destination, Vector3.up).normalized *
                              stoppingDistance * 1.1f;
            for (int i = 0; i < segments; i++)
            {
                _lineRenderer.SetPosition(i, circlePoint);
                circlePoint = Quaternion.AngleAxis(360f / segments, Vector3.up) *
                              (circlePoint - pos) + pos;
            }
        }

        private void PlaceSphere()
        {
            if (!_sphere)
            {
                _sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                _sphere.GetComponent<Renderer>().material = material;
                _sphere.GetComponent<Renderer>().material.color = new Color(0, 0, 0, 0.5f);

            }

            _sphere.transform.position = _destination;
            _sphere.transform.localScale = Vector3.one * stoppingDistance * 2;
        }

        private void React()
        {
            Debug.Log($"Reached {_destination}");
            _destination = _iterator.Destination;
            PlaceSphere();
        }
    }
}