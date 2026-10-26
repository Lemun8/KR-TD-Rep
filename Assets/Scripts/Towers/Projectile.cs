using UnityEngine;

public class Projectile : MonoBehaviour
{
    [Header("Flight")]
    [SerializeField] private float speed = 8f;
    [SerializeField] private float hitDistance = 0.1f;
    [Tooltip("ON: follows the enemy and never misses (mage). OFF: flies to where the enemy was when fired and can miss (archer, artillery).")]
    [SerializeField] private bool homing = true;

    [Header("Single target, not homing (arrows)")]
    [Tooltip("An arrow that lands within this distance of an enemy hits it.")]
    [Min(0f)]
    [SerializeField] private float hitRadius = 0.45f;

    [Header("Splash (0 = single target)")]
    [Min(0f)]
    [SerializeField] private float splashRadius = 0f;
    [Tooltip("Optional. Spawned on impact and scaled to the splash radius (assumes a 1-unit-wide sprite).")]
    [SerializeField] private GameObject impactEffectPrefab;
    [SerializeField] private float impactEffectDuration = 0.3f;

    private Enemy target;
    private float damage;
    private DamageType damageType;
    private Vector3 lastKnownPosition;   // for a non-homing projectile this never changes: it is the fixed destination

    public void Initialize(Enemy newTarget, float newDamage, DamageType newDamageType)
    {
        target = newTarget;
        damage = newDamage;
        damageType = newDamageType;
        lastKnownPosition = target.transform.position;
    }

    private void Update()
    {
        // Only a homing projectile keeps following the enemy
        if (homing && target != null)
        {
            lastKnownPosition = target.transform.position;
        }

        Vector3 toTarget = lastKnownPosition - transform.position;
        float step = speed * Time.deltaTime;

        if (toTarget.magnitude <= step + hitDistance)
        {
            Hit();
            return;
        }

        transform.position += toTarget.normalized * step;

        // Point the sprite in the direction of travel (assumes the sprite faces right)
        float angle = Mathf.Atan2(toTarget.y, toTarget.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    private void Hit()
    {
        if (splashRadius > 0f)
        {
            DealSplashDamage();
        }
        else if (homing)
        {
            DamageTarget();
        }
        else
        {
            DamageNearestAtImpact();
        }

        SpawnImpactEffect();
        Destroy(gameObject);
    }

    private void DamageTarget()
    {
        if (target != null && !target.IsDead)
        {
            target.TakeDamage(damage, damageType);
        }
    }

    // Fixed-spot arrow: hits whoever is standing at the landing spot (if anyone is)
    private void DamageNearestAtImpact()
    {
        Enemy best = null;
        float bestSqr = hitRadius * hitRadius;

        for (int i = Enemy.Active.Count - 1; i >= 0; i--)
        {
            Enemy enemy = Enemy.Active[i];
            if (enemy == null || enemy.IsDead) continue;

            Vector2 offset = enemy.transform.position - lastKnownPosition;
            float sqr = offset.sqrMagnitude;

            if (sqr <= bestSqr)
            {
                bestSqr = sqr;
                best = enemy;
            }
        }

        if (best != null) best.TakeDamage(damage, damageType);
    }

    // Hurts every living enemy near the impact point (armor still applies to each one)
    private void DealSplashDamage()
    {
        float radiusSqr = splashRadius * splashRadius;

        for (int i = Enemy.Active.Count - 1; i >= 0; i--)
        {
            Enemy enemy = Enemy.Active[i];
            if (enemy == null || enemy.IsDead) continue;

            Vector2 offset = enemy.transform.position - lastKnownPosition;
            if (offset.sqrMagnitude <= radiusSqr)
            {
                enemy.TakeDamage(damage, damageType);
            }
        }
    }

    private void SpawnImpactEffect()
    {
        if (impactEffectPrefab == null) return;

        GameObject effect = Instantiate(impactEffectPrefab, lastKnownPosition, Quaternion.identity);

        if (splashRadius > 0f)
        {
            effect.transform.localScale = Vector3.one * splashRadius * 2f;
        }

        Destroy(effect, impactEffectDuration);
    }
}