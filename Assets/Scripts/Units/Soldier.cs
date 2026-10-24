using System;
using UnityEngine;

[RequireComponent(typeof(Health))]
public class Soldier : MonoBehaviour
{
    [Header("Stats")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float damage = 5f;
    [SerializeField] private float attacksPerSecond = 1f;
    [Tooltip("The soldier attacks when it is this close to its enemy.")]
    [SerializeField] private float contactDistance = 0.5f;

    public bool IsDead => health.IsDead;
    public event Action<Soldier> Died;

    private Health health;
    private Barracks owner;
    private Vector3 homePosition;
    private Enemy target;
    private float attackTimer;
    private bool movingToRally;   // true while walking to the rally point: ignores all enemies

    // True when this soldier is the one fighting the enemy. False means it is only helping another soldier.
    private bool HoldsClaim => target != null && target.IsClaimedBy(this);

    private void Awake()
    {
        health = GetComponent<Health>();
        health.Died += HandleDied;
    }

    private void OnDestroy()
    {
        health.Died -= HandleDied;
        ReleaseTarget();
    }

    // The barracks calls this right after creating the soldier
    public void Initialize(Barracks newOwner, Vector3 newHome)
    {
        owner = newOwner;
        homePosition = newHome;
        movingToRally = true;
    }

    // The barracks calls this when the rally point moves: drop everything and go there
    public void OrderMoveTo(Vector3 newHome)
    {
        homePosition = newHome;
        movingToRally = true;
        ReleaseTarget();
    }

    public void TakeDamage(float amount)
    {
        health.TakeDamage(amount);
    }

    private void Update()
    {
        if (owner == null || IsDead) return;

        // Rally order: walk to the rally point first and ignore every enemy on the way
        if (movingToRally)
        {
            MoveTo(homePosition);

            if (Vector3.Distance(transform.position, homePosition) <= 0.02f)
            {
                movingToRally = false;
            }

            return;
        }

        if (target == null || target.IsDead)
        {
            // Forget a target that died or disappeared and look for a new one
            ReleaseTarget();
            AcquireTarget();
        }
        else if (!HoldsClaim)
        {
            // We are only helping another soldier, so see if something better came up
            ImproveTarget();
        }

        if (target != null)
        {
            ChaseAndFight();
        }
        else
        {
            MoveTo(homePosition);
        }
    }

    private void AcquireTarget()
    {
        // 1) Prefer an enemy nobody is fighting yet
        Enemy free = FindEnemy(false);
        if (free != null && free.TryClaim(this))
        {
            target = free;
            attackTimer = 0f;
            return;
        }

        // 2) No free enemy nearby: gang up on one that another soldier is already fighting
        Enemy busy = FindEnemy(true);
        if (busy != null)
        {
            target = busy;
            attackTimer = 0f;
        }
    }

    // Used while helping: take over the fight if the other soldier is gone, or switch to a new free enemy
    private void ImproveTarget()
    {
        if (!target.IsClaimed && target.TryClaim(this)) return;

        Enemy free = FindEnemy(false);
        if (free != null && free.TryClaim(this))
        {
            target = free;
            attackTimer = 0f;
        }
    }

    // Among the enemies near the rally point, returns the one closest to the village.
    // wantClaimed = false: only enemies nobody is fighting. true: only enemies another soldier is fighting.
    private Enemy FindEnemy(bool wantClaimed)
    {
        Enemy best = null;
        float bestRemaining = float.MaxValue;
        float radiusSqr = owner.EngageRadius * owner.EngageRadius;

        foreach (Enemy enemy in Enemy.Active)
        {
            if (enemy == null || enemy.IsDead) continue;
            if (enemy.IsClaimed != wantClaimed) continue;

            Vector2 offset = enemy.transform.position - owner.RallyPoint;
            if (offset.sqrMagnitude > radiusSqr) continue;

            float remaining = enemy.RemainingPathDistance;
            if (remaining < bestRemaining)
            {
                bestRemaining = remaining;
                best = enemy;
            }
        }

        return best;
    }

    private void ChaseAndFight()
    {
        float distance = Vector2.Distance(transform.position, target.transform.position);

        if (distance > contactDistance)
        {
            MoveTo(target.transform.position);
            return;
        }

        attackTimer -= Time.deltaTime;
        if (attackTimer <= 0f)
        {
            target.TakeDamage(damage, DamageType.Physical);
            attackTimer = 1f / attacksPerSecond;
        }
    }

    private void MoveTo(Vector3 destination)
    {
        transform.position = Vector3.MoveTowards(transform.position, destination, moveSpeed * Time.deltaTime);
    }

    // Lets go of the enemy. If we were the one fighting it, it starts walking again.
    private void ReleaseTarget()
    {
        if (target != null) target.Release(this);
        target = null;
    }

    private void HandleDied()
    {
        ReleaseTarget();
        Died?.Invoke(this);
        Destroy(gameObject);
    }
}