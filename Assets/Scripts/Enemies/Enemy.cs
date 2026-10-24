using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Health))]
[RequireComponent(typeof(EnemyPathFollower))]
public class Enemy : MonoBehaviour
{
    // Every living enemy in the scene. Towers and soldiers read this to find targets.
    public static readonly List<Enemy> Active = new List<Enemy>();

    [Header("Stats")]
    [SerializeField] private float damageToVillage = 5f;
    [SerializeField] private int goldReward = 10;

    [Header("Melee (used when a soldier is fighting this enemy)")]
    [SerializeField] private float meleeDamage = 4f;
    [SerializeField] private float meleeAttacksPerSecond = 1f;
    [Tooltip("The enemy stops and fights when its soldier is this close.")]
    [SerializeField] private float contactDistance = 0.6f;

    [Header("Defense")]
    [Tooltip("Physical damage reduction. 0 = none, 0.5 = takes 50% less physical damage.")]
    [Range(0f, 0.9f)]
    [SerializeField] private float armor = 0f;

    [Tooltip("Magic damage reduction. 0 = none, 0.5 = takes 50% less magic damage.")]
    [Range(0f, 0.9f)]
    [SerializeField] private float magicResistance = 0f;

    public int GoldReward => goldReward;
    public bool IsDead => health.IsDead;
    public bool IsClaimed => blocker != null;           // a soldier has picked this enemy
    public bool IsClaimedBy(Soldier soldier) => blocker == soldier;
    public float RemainingPathDistance => follower.RemainingDistance; // smaller = closer to the village

    public event Action<Enemy> Killed;          // died to a tower or soldier, so give gold
    public event Action<Enemy> ReachedVillage;  // got through, so no gold

    private Health health;
    private EnemyPathFollower follower;
    private Soldier blocker;
    private float attackTimer;

    private void Awake()
    {
        health = GetComponent<Health>();
        follower = GetComponent<EnemyPathFollower>();

        health.Died += HandleDied;
        follower.ReachedEnd += HandleReachedEnd;
    }

    private void OnEnable()
    {
        Active.Add(this);
    }

    private void OnDisable()
    {
        Active.Remove(this);
    }

    private void OnDestroy()
    {
        health.Died -= HandleDied;
        follower.ReachedEnd -= HandleReachedEnd;
    }

    private void Update()
    {
        // No soldier on us (or it died): keep walking
        if (blocker == null || blocker.IsDead)
        {
            blocker = null;
            follower.Paused = false;
            return;
        }

        // Stop and fight once the soldier is close enough
        float distance = Vector2.Distance(transform.position, blocker.transform.position);
        bool inContact = distance <= contactDistance;
        follower.Paused = inContact;

        if (!inContact) return;

        attackTimer -= Time.deltaTime;
        if (attackTimer <= 0f)
        {
            blocker.TakeDamage(meleeDamage);
            attackTimer = 1f / meleeAttacksPerSecond;
        }
    }

    // A soldier calls this to reserve this enemy. Only one soldier can fight an enemy at a time.
    public bool TryClaim(Soldier soldier)
    {
        if (blocker != null || IsDead) return false;

        blocker = soldier;
        return true;
    }

    public void Release(Soldier soldier)
    {
        if (blocker != soldier) return;

        blocker = null;
        follower.Paused = false;
    }

    // Everything that hurts an enemy (arrows, spells, soldiers) goes through here
    public void TakeDamage(float amount, DamageType type)
    {
        float reduction = type == DamageType.Physical ? armor : magicResistance;

        health.TakeDamage(amount * (1f - reduction));
    }

    private void HandleDied()
    {
        Killed?.Invoke(this);
        Destroy(gameObject);
    }

    private void HandleReachedEnd(EnemyPathFollower _)
    {
        if (VillageHealth.Instance != null)
        {
            VillageHealth.Instance.TakeDamage(damageToVillage);
        }

        ReachedVillage?.Invoke(this);
        Destroy(gameObject);
    }
}