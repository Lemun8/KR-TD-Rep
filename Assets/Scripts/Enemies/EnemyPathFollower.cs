using System;
using UnityEngine;

public class EnemyPathFollower : MonoBehaviour
{
    [SerializeField] private WaypointPath path;   // Only for testing. A spawner will assign this later.
    [SerializeField] private float moveSpeed = 2f;

    // Other scripts (village health, wave manager) can subscribe to this
    public event Action<EnemyPathFollower> ReachedEnd;

    // Set to true while the enemy is stopped to fight a soldier
    public bool Paused { get; set; }

    // How far this enemy still has to walk to reach the village
    public float RemainingDistance
    {
        get
        {
            if (!initialized) return float.MaxValue;

            float distance = Vector3.Distance(transform.position, path.GetWaypoint(targetIndex).position);

            for (int i = targetIndex; i < path.Count - 1; i++)
            {
                distance += Vector3.Distance(path.GetWaypoint(i).position, path.GetWaypoint(i + 1).position);
            }

            return distance;
        }
    }

    private int targetIndex;
    private bool initialized;

    private void Start()
    {
        // If a path was assigned in the Inspector, start immediately (handy for testing)
        if (path != null && !initialized)
        {
            Initialize(path);
        }
    }

    // The spawner will call this when it creates an enemy
    public void Initialize(WaypointPath newPath)
    {
        path = newPath;

        if (path == null || path.Count < 2)
        {
            Debug.LogWarning("EnemyPathFollower needs a path with at least 2 waypoints.", this);
            return;
        }

        transform.position = path.GetWaypoint(0).position;
        targetIndex = 1;
        initialized = true;
    }

    private void Update()
    {
        if (!initialized || Paused) return;

        Vector3 target = path.GetWaypoint(targetIndex).position;
        transform.position = Vector3.MoveTowards(transform.position, target, moveSpeed * Time.deltaTime);

        if (Vector3.Distance(transform.position, target) < 0.01f)
        {
            targetIndex++;

            if (targetIndex >= path.Count)
            {
                ReachedEnd?.Invoke(this);
                initialized = false;
            }
        }
    }
}