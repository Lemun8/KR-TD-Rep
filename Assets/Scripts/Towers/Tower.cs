using UnityEngine;

public class Tower : MonoBehaviour
{
    [Header("Stats")]
    [SerializeField] private float range = 3.5f;
    [SerializeField] private float attacksPerSecond = 1f;
    [SerializeField] private float damage = 10f;
    [SerializeField] private DamageType damageType = DamageType.Physical;

    [Header("References")]
    [SerializeField] private Projectile projectilePrefab;
    [SerializeField] private Transform firePoint;   // optional: where projectiles spawn

    private float cooldown;

    private void Update()
    {
        if (cooldown > 0f)
        {
            cooldown -= Time.deltaTime;
            return;
        }

        Enemy target = FindTarget();
        if (target == null) return;

        Shoot(target);
        cooldown = 1f / attacksPerSecond;
    }

    // Targeting: the enemy in range that is closest to the village ("first")
    private Enemy FindTarget()
    {
        Enemy best = null;
        float bestRemaining = float.MaxValue;
        float rangeSqr = range * range;

        foreach (Enemy enemy in Enemy.Active)
        {
            if (enemy == null || enemy.IsDead) continue;

            Vector2 offset = enemy.transform.position - transform.position;
            if (offset.sqrMagnitude > rangeSqr) continue;

            float remaining = enemy.RemainingPathDistance;
            if (remaining < bestRemaining)
            {
                bestRemaining = remaining;
                best = enemy;
            }
        }

        return best;
    }

    private void Shoot(Enemy target)
    {
        if (projectilePrefab == null)
        {
            Debug.LogError("Tower has no projectile prefab assigned.", this);
            return;
        }

        Vector3 origin = firePoint != null ? firePoint.position : transform.position;
        Projectile projectile = Instantiate(projectilePrefab, origin, Quaternion.identity);
        projectile.Initialize(target, damage, damageType);
    }

    // Draws the range circle when the tower is selected in the Scene view
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, range);
    }
}