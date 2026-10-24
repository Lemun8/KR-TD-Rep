using UnityEngine;

public class WaypointPath : MonoBehaviour
{
    [Header("Editor Visuals")]
    [SerializeField] private Color gizmoColor = Color.yellow;
    [SerializeField] private float waypointRadius = 0.2f;

    // Waypoints are simply the child objects, in Hierarchy order (top = start, bottom = end)
    public int Count => transform.childCount;

    public Transform GetWaypoint(int index)
    {
        return transform.GetChild(index);
    }

    // The point on the path closest to a position (used to place a barracks' default rally point)
    public Vector3 GetClosestPoint(Vector3 position)
    {
        Vector3 best = GetWaypoint(0).position;
        float bestSqr = float.MaxValue;

        for (int i = 0; i < Count - 1; i++)
        {
            Vector3 a = GetWaypoint(i).position;
            Vector3 b = GetWaypoint(i + 1).position;
            Vector3 ab = b - a;

            float lengthSqr = ab.sqrMagnitude;
            float t = lengthSqr > 0f ? Mathf.Clamp01(Vector3.Dot(position - a, ab) / lengthSqr) : 0f;

            Vector3 point = a + ab * t;
            float sqr = (point - position).sqrMagnitude;

            if (sqr < bestSqr)
            {
                bestSqr = sqr;
                best = point;
            }
        }

        return best;
    }

    // Starts at the spot on the path closest to 'position', then walks 'offset' units along the path
    // (negative = back toward the start). Used to line soldiers up ON the track, even around bends.
    public Vector3 GetPointAlongPath(Vector3 position, float offset)
    {
        if (Count < 2) return position;

        // 1) Find the closest point on the path and which segment it is on
        int segment = 0;
        Vector3 current = GetWaypoint(0).position;
        float bestSqr = float.MaxValue;

        for (int i = 0; i < Count - 1; i++)
        {
            Vector3 a = GetWaypoint(i).position;
            Vector3 b = GetWaypoint(i + 1).position;
            Vector3 ab = b - a;

            float lengthSqr = ab.sqrMagnitude;
            float t = lengthSqr > 0f ? Mathf.Clamp01(Vector3.Dot(position - a, ab) / lengthSqr) : 0f;

            Vector3 point = a + ab * t;
            float sqr = (point - position).sqrMagnitude;

            if (sqr < bestSqr)
            {
                bestSqr = sqr;
                current = point;
                segment = i;
            }
        }

        // 2) Walk along the path from there
        float remaining = Mathf.Abs(offset);

        if (offset >= 0f)
        {
            for (int i = segment; i < Count - 1; i++)
            {
                Vector3 end = GetWaypoint(i + 1).position;
                float distance = Vector3.Distance(current, end);

                if (remaining <= distance) return Vector3.MoveTowards(current, end, remaining);

                remaining -= distance;
                current = end;
            }
        }
        else
        {
            for (int i = segment; i >= 0; i--)
            {
                Vector3 start = GetWaypoint(i).position;
                float distance = Vector3.Distance(current, start);

                if (remaining <= distance) return Vector3.MoveTowards(current, start, remaining);

                remaining -= distance;
                current = start;
            }
        }

        return current;   // ran off the end of the path, so stop at the last point
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = gizmoColor;

        for (int i = 0; i < Count; i++)
        {
            Vector3 current = GetWaypoint(i).position;
            Gizmos.DrawSphere(current, waypointRadius);

            if (i < Count - 1)
            {
                Gizmos.DrawLine(current, GetWaypoint(i + 1).position);
            }
        }
    }
}