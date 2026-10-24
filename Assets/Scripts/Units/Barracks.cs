using System;
using UnityEngine;

public class Barracks : MonoBehaviour
{
    [Header("Soldiers")]
    [SerializeField] private Soldier soldierPrefab;
    [SerializeField, Min(1)] private int soldierCount = 3;
    [SerializeField] private float respawnTime = 8f;
    [SerializeField] private float formationSpacing = 0.5f;

    [Header("Rally Point")]
    [Tooltip("How far from the barracks the rally point can be placed.")]
    [SerializeField] private float rallyRange = 3f;
    [Tooltip("Enemies closer than this to the rally point get engaged by the soldiers.")]
    [SerializeField] private float engageRadius = 1.5f;
    [Tooltip("How close to the track a click must be to count as 'on the track'.")]
    [SerializeField] private float trackClickTolerance = 0.75f;
    [SerializeField] private Transform rallyFlag;   // optional visual

    [Header("Runtime (debug view)")]
    [SerializeField] private float[] respawnTimers;

    public Vector3 RallyPoint { get; private set; }
    public float EngageRadius => engageRadius;

    private Soldier[] soldiers;
    private WaypointPath path;
    private RallyRangeIndicator indicator;

    private void Awake()
    {
        path = FindFirstObjectByType<WaypointPath>();

        // The range indicator adds itself, so the prefab needs no extra setup
        indicator = GetComponent<RallyRangeIndicator>();
        if (indicator == null) indicator = gameObject.AddComponent<RallyRangeIndicator>();

        indicator.Setup(path, rallyRange, engageRadius);
    }

    private void Start()
    {
        soldiers = new Soldier[soldierCount];
        respawnTimers = new float[soldierCount];

        // Default rally point: the spot on the enemy path closest to this barracks
        if (path != null)
        {
            Vector3 closest = path.GetClosestPoint(transform.position);

            if (Vector2.Distance(closest, transform.position) > rallyRange)
            {
                Debug.LogWarning("Barracks: the track is out of rally range. Move the build spot closer or raise Rally Range.", this);
            }

            ApplyRallyPoint(closest);
        }
        else
        {
            Debug.LogWarning("Barracks: no WaypointPath in the scene.", this);
            ApplyRallyPoint(transform.position);
        }

        for (int i = 0; i < soldierCount; i++)
        {
            SpawnSoldier(i);
        }
    }

    private void Update()
    {
        if (soldiers == null) return;

        // Count down the respawn timer of every empty slot
        for (int i = 0; i < soldierCount; i++)
        {
            if (soldiers[i] != null || respawnTimers[i] <= 0f) continue;

            respawnTimers[i] -= Time.deltaTime;

            if (respawnTimers[i] <= 0f)
            {
                SpawnSoldier(i);
            }
        }
    }

    private void OnDestroy()
    {
        // Selling the barracks removes its soldiers
        if (soldiers == null) return;

        foreach (Soldier soldier in soldiers)
        {
            if (soldier != null) Destroy(soldier.gameObject);
        }
    }

    // Tries to move the rally point to where the player clicked.
    // Only allowed ON the track and inside the rally range. Returns false if the click was not allowed.
    public bool TrySetRallyPoint(Vector3 clickedWorldPosition)
    {
        if (path == null) return false;

        Vector3 onTrack = path.GetClosestPoint(clickedWorldPosition);

        // The click must be on the track...
        if (Vector2.Distance(clickedWorldPosition, onTrack) > trackClickTolerance) return false;

        // ...and that spot on the track must be within this barracks' range
        if (Vector2.Distance(onTrack, transform.position) > rallyRange) return false;

        ApplyRallyPoint(onTrack);
        return true;
    }

    public void ShowRallyIndicator(bool show)
    {
        if (show) indicator.Show(RallyPoint);
        else indicator.Hide();
    }

    public void FlashRallyIndicatorInvalid()
    {
        indicator.FlashInvalid();
    }

    private void ApplyRallyPoint(Vector3 point)
    {
        point.z = transform.position.z;
        RallyPoint = point;

        if (rallyFlag != null) rallyFlag.position = RallyPoint;
        indicator.UpdateRallyPoint(RallyPoint);

        if (soldiers == null) return;

        for (int i = 0; i < soldiers.Length; i++)
        {
            if (soldiers[i] != null) soldiers[i].OrderMoveTo(GetSlotPosition(i));
        }
    }

    private void SpawnSoldier(int slot)
    {
        if (soldierPrefab == null)
        {
            Debug.LogError("Barracks has no soldier prefab assigned.", this);
            return;
        }

        Soldier soldier = Instantiate(soldierPrefab, transform.position, Quaternion.identity);
        soldier.Initialize(this, GetSlotPosition(slot));
        soldier.Died += HandleSoldierDied;

        soldiers[slot] = soldier;
        respawnTimers[slot] = 0f;
    }

    private void HandleSoldierDied(Soldier soldier)
    {
        soldier.Died -= HandleSoldierDied;

        int slot = Array.IndexOf(soldiers, soldier);
        if (slot < 0) return;

        soldiers[slot] = null;
        respawnTimers[slot] = respawnTime;
    }

    // Each soldier stands at its own spot around the rally point
    private Vector3 GetSlotPosition(int slot)
    {
        float angle = (360f / soldierCount) * slot * Mathf.Deg2Rad;
        Vector3 offset = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * formationSpacing;

        return RallyPoint + offset;
    }

    private void OnDrawGizmosSelected()
    {
        // Cyan = where the rally point can be placed, red = which enemies get engaged
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, rallyRange);

        Gizmos.color = Color.red;
        Vector3 rally = Application.isPlaying ? RallyPoint : transform.position;
        Gizmos.DrawWireSphere(rally, engageRadius);
    }
}