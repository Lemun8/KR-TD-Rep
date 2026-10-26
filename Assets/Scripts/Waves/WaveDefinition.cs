using System;
using System.Collections.Generic;
using UnityEngine;

// One batch of enemies inside a wave. All groups of a wave run at the same time.
[Serializable]
public class SpawnGroup
{
    [Tooltip("Just a note for yourself, e.g. 'Goblin swarm'.")]
    public string label;
    public Enemy enemyPrefab;
    [Min(1)] public int count = 5;

    [Tooltip("Seconds after the wave starts before this group begins.")]
    [Min(0f)] public float startDelay = 0f;

    [Tooltip("How many enemies come out in one clump. 1 = a steady trickle, same as Count = one big swarm.")]
    [Min(1)] public int swarmSize = 1;

    [Tooltip("Seconds between enemies inside one clump. Small = tight clump.")]
    [Min(0.05f)] public float swarmSpacing = 0.25f;

    [Tooltip("Seconds between the end of one clump and the start of the next.")]
    [Min(0f)] public float timeBetweenSwarms = 3f;

    [Min(0.1f)] public float healthMultiplier = 1f;   // escalating difficulty
}

[Serializable]
public class WaveDefinition
{
    public List<SpawnGroup> groups = new List<SpawnGroup>();
}