using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [SerializeField] private WaypointPath path;

    public Enemy Spawn(Enemy prefab, float healthMultiplier = 1f)
    {
        if (prefab == null || path == null)
        {
            Debug.LogError("EnemySpawner: missing enemy prefab or path.", this);
            return null;
        }

        Enemy enemy = Instantiate(prefab, path.GetWaypoint(0).position, Quaternion.identity);

        enemy.GetComponent<EnemyPathFollower>().Initialize(path);

        Health health = enemy.GetComponent<Health>();
        health.SetMaxHealth(health.Max * healthMultiplier);

        return enemy;
    }
}