using UnityEngine;

// Draws the enemy path as a road with start/end markers. Placeholder until real track art exists.
[RequireComponent(typeof(WaypointPath))]
public class PathVisualizer : MonoBehaviour
{
    [Header("Road")]
    [Tooltip("Keep this about as wide as twice the barracks' Track Click Tolerance.")]
    [SerializeField] private float width = 0.8f;
    [SerializeField] private float borderThickness = 0.1f;
    [SerializeField] private Color roadColor = new Color(0.78f, 0.66f, 0.45f);
    [SerializeField] private Color borderColor = new Color(0.36f, 0.26f, 0.16f);
    [SerializeField] private int sortingOrder = -10;

    [Header("Start / End markers")]
    [SerializeField] private bool showMarkers = true;
    [SerializeField] private float markerRadius = 0.5f;
    [SerializeField] private Color startColor = new Color(0.3f, 0.9f, 0.3f);
    [SerializeField] private Color endColor = new Color(0.95f, 0.25f, 0.25f);

    private void Start()
    {
        WaypointPath path = GetComponent<WaypointPath>();

        if (path.Count < 2)
        {
            Debug.LogWarning("PathVisualizer needs a path with at least 2 waypoints.", this);
            return;
        }

        Vector3[] points = new Vector3[path.Count];
        for (int i = 0; i < points.Length; i++)
        {
            points[i] = path.GetWaypoint(i).position;
        }

        // The visuals must NOT be children of the path object, because the path treats every child as a waypoint
        Transform visuals = new GameObject("Path Visuals").transform;
        visuals.SetParent(transform.parent, false);

        DrawRoad(visuals, "Road Border", width + borderThickness * 2f, borderColor, sortingOrder, points);
        DrawRoad(visuals, "Road", width, roadColor, sortingOrder + 1, points);

        if (showMarkers)
        {
            DrawMarker(visuals, "Start Marker", points[0], startColor);
            DrawMarker(visuals, "End Marker", points[points.Length - 1], endColor);
        }
    }

    private void DrawRoad(Transform parent, string objectName, float lineWidth, Color color, int order, Vector3[] points)
    {
        LineRenderer line = LineUtil.Create(parent, objectName, lineWidth, color, order);
        line.positionCount = points.Length;
        line.SetPositions(points);
    }

    private void DrawMarker(Transform parent, string objectName, Vector3 center, Color color)
    {
        LineRenderer ring = LineUtil.Create(parent, objectName, 0.15f, color, sortingOrder + 2);
        LineUtil.SetCircle(ring, center, markerRadius);
    }
}