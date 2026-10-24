using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Shows where a barracks' rally point can go. The Barracks creates and controls this itself.
public class RallyRangeIndicator : MonoBehaviour
{
    private static readonly Color RangeColor = new Color(0.3f, 0.9f, 1f, 0.95f);
    private static readonly Color InvalidColor = new Color(1f, 0.25f, 0.25f, 1f);
    private static readonly Color TrackColor = new Color(1f, 0.92f, 0.3f, 0.9f);
    private static readonly Color EngageColor = new Color(1f, 0.5f, 0.2f, 0.85f);

    private WaypointPath path;
    private float rallyRange;
    private float engageRadius;

    private LineRenderer rangeRing;
    private LineRenderer engageRing;
    private readonly List<LineRenderer> trackLines = new List<LineRenderer>();
    private readonly List<Vector3> run = new List<Vector3>();
    private bool visible;

    public void Setup(WaypointPath newPath, float newRallyRange, float newEngageRadius)
    {
        path = newPath;
        rallyRange = newRallyRange;
        engageRadius = newEngageRadius;
    }

    public void Show(Vector3 rallyPoint)
    {
        if (rangeRing == null)
        {
            rangeRing = LineUtil.Create(transform, "Rally Range Ring", 0.1f, RangeColor, -5);
            engageRing = LineUtil.Create(transform, "Engage Ring", 0.07f, EngageColor, -5);
        }

        visible = true;

        LineUtil.SetColor(rangeRing, RangeColor);
        LineUtil.SetCircle(rangeRing, transform.position, rallyRange);
        rangeRing.enabled = true;

        UpdateRallyPoint(rallyPoint);
        engageRing.enabled = true;

        RebuildTrackHighlight();
    }

    public void Hide()
    {
        visible = false;

        if (rangeRing != null) rangeRing.enabled = false;
        if (engageRing != null) engageRing.enabled = false;

        foreach (LineRenderer line in trackLines) line.enabled = false;
    }

    public void UpdateRallyPoint(Vector3 rallyPoint)
    {
        if (engageRing == null) return;

        LineUtil.SetCircle(engageRing, rallyPoint, engageRadius);
    }

    // Called when the player clicks somewhere that isn't allowed
    public void FlashInvalid()
    {
        if (!visible) return;

        StopAllCoroutines();
        StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        LineUtil.SetColor(rangeRing, InvalidColor);
        yield return new WaitForSeconds(0.25f);
        LineUtil.SetColor(rangeRing, RangeColor);
    }

    // Highlights the parts of the track that are inside the rally range (the legal spots)
    private void RebuildTrackHighlight()
    {
        foreach (LineRenderer line in trackLines) line.enabled = false;
        if (path == null || path.Count < 2) return;

        int used = 0;
        Vector2 center = transform.position;
        float rangeSqr = rallyRange * rallyRange;
        run.Clear();

        for (int i = 0; i < path.Count - 1; i++)
        {
            Vector3 a = path.GetWaypoint(i).position;
            Vector3 b = path.GetWaypoint(i + 1).position;

            int steps = Mathf.Max(1, Mathf.CeilToInt(Vector3.Distance(a, b) / 0.1f));

            for (int s = 0; s <= steps; s++)
            {
                Vector3 point = Vector3.Lerp(a, b, (float)s / steps);

                if (((Vector2)point - center).sqrMagnitude <= rangeSqr)
                {
                    run.Add(point);
                }
                else
                {
                    FlushRun(ref used);
                }
            }
        }

        FlushRun(ref used);
    }

    // Turns the collected points into one highlighted line
    private void FlushRun(ref int used)
    {
        if (run.Count >= 2)
        {
            if (used >= trackLines.Count)
            {
                trackLines.Add(LineUtil.Create(transform, "Valid Track Highlight", 0.35f, TrackColor, -6));
            }

            LineRenderer line = trackLines[used];
            line.positionCount = run.Count;
            line.SetPositions(run.ToArray());
            line.enabled = true;
            used++;
        }

        run.Clear();
    }
}