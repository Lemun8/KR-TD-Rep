using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// TD > Fill Default Waves: fills the WaveManager's 5 waves with goblin / orc / wulf groups.
public static class WaveSetup
{
    private const string EnemyFolder = "Assets/Prefabs/Enemies";

    [MenuItem("TD/Fill Default Waves")]
    private static void Fill()
    {
        WaveManager manager = Object.FindFirstObjectByType<WaveManager>();

        if (manager == null)
        {
            Debug.LogError("Fill Default Waves: no WaveManager in the scene.");
            return;
        }

        Enemy goblin = Load("Goblin");
        Enemy orc = Load("Orc");
        Enemy wulf = Load("Wulf");

        if (goblin == null || orc == null || wulf == null) return;

        SerializedObject so = new SerializedObject(manager);
        SerializedProperty waves = so.FindProperty("waves");
        waves.arraySize = 5;

        WriteWave(waves.GetArrayElementAtIndex(0),
            Group("Goblin swarm", goblin, 8, 0f, 4, 0.3f, 6f, 1f));

        WriteWave(waves.GetArrayElementAtIndex(1),
            Group("Goblin swarm", goblin, 12, 0f, 6, 0.25f, 6f, 1f),
            Group("Orc patrol", orc, 2, 10f, 1, 0.25f, 5f, 1f));

        WriteWave(waves.GetArrayElementAtIndex(2),
            Group("Goblin swarm", goblin, 16, 0f, 8, 0.25f, 6f, 1.1f),
            Group("Wulf pack", wulf, 4, 8f, 2, 0.4f, 5f, 1f));

        WriteWave(waves.GetArrayElementAtIndex(3),
            Group("Orc march", orc, 5, 0f, 1, 0.25f, 4f, 1.2f),
            Group("Goblin swarm", goblin, 20, 4f, 10, 0.25f, 6f, 1.15f),
            Group("Wulf pack", wulf, 6, 12f, 3, 0.3f, 5f, 1.1f));

        WriteWave(waves.GetArrayElementAtIndex(4),
            Group("Orc march", orc, 8, 0f, 1, 0.25f, 3.5f, 1.4f),
            Group("Goblin horde", goblin, 30, 3f, 10, 0.2f, 6f, 1.3f),
            Group("Wulf pack", wulf, 8, 10f, 4, 0.3f, 5f, 1.25f));

        so.ApplyModifiedProperties();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("Default waves filled in. Tweak them on the WaveManager in the Inspector.");
    }

    private static Enemy Load(string prefabName)
    {
        string path = $"{EnemyFolder}/{prefabName}.prefab";
        Enemy enemy = AssetDatabase.LoadAssetAtPath<Enemy>(path);

        if (enemy == null)
        {
            Debug.LogError($"Fill Default Waves: couldn't find an Enemy prefab at {path}. Check the name and folder.");
        }

        return enemy;
    }

    private static SpawnGroup Group(string label, Enemy prefab, int count, float startDelay,
        int swarmSize, float swarmSpacing, float timeBetweenSwarms, float healthMultiplier)
    {
        return new SpawnGroup
        {
            label = label,
            enemyPrefab = prefab,
            count = count,
            startDelay = startDelay,
            swarmSize = swarmSize,
            swarmSpacing = swarmSpacing,
            timeBetweenSwarms = timeBetweenSwarms,
            healthMultiplier = healthMultiplier
        };
    }

    private static void WriteWave(SerializedProperty wave, params SpawnGroup[] groups)
    {
        SerializedProperty list = wave.FindPropertyRelative("groups");
        list.arraySize = groups.Length;

        for (int i = 0; i < groups.Length; i++)
        {
            SerializedProperty element = list.GetArrayElementAtIndex(i);
            SpawnGroup group = groups[i];

            element.FindPropertyRelative("label").stringValue = group.label;
            element.FindPropertyRelative("enemyPrefab").objectReferenceValue = group.enemyPrefab;
            element.FindPropertyRelative("count").intValue = group.count;
            element.FindPropertyRelative("startDelay").floatValue = group.startDelay;
            element.FindPropertyRelative("swarmSize").intValue = group.swarmSize;
            element.FindPropertyRelative("swarmSpacing").floatValue = group.swarmSpacing;
            element.FindPropertyRelative("timeBetweenSwarms").floatValue = group.timeBetweenSwarms;
            element.FindPropertyRelative("healthMultiplier").floatValue = group.healthMultiplier;
        }
    }
}