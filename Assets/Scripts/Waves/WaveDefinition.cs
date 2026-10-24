using System;
using UnityEngine;

[Serializable]
public class WaveDefinition
{
    public Enemy enemyPrefab;
    [Min(1)] public int enemyCount = 5;
    [Min(0.1f)] public float spawnInterval = 1.5f;   // seconds between each enemy
    [Min(0.1f)] public float healthMultiplier = 1f;  // escalating difficulty
}