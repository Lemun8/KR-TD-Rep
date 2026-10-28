using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// TD > Create Audio Manager
// Adds the AudioManager to the scene (or finds the one that is already there) and makes sure the Sounds list
// has one empty slot for every SfxEvent. Run it again after adding a new SfxEvent: it only adds what is missing.
public static class AudioSetup
{
    [MenuItem("TD/Create Audio Manager")]
    private static void Create()
    {
        AudioManager manager = UnityEngine.Object.FindFirstObjectByType<AudioManager>();

        if (manager == null)
        {
            GameObject go = new GameObject("AudioManager");
            manager = go.AddComponent<AudioManager>();
            Undo.RegisterCreatedObjectUndo(go, "Create Audio Manager");
        }

        SerializedObject so = new SerializedObject(manager);

        // Wire the WaveManager if there is one
        WaveManager waveManager = UnityEngine.Object.FindFirstObjectByType<WaveManager>();
        if (waveManager != null) so.FindProperty("waveManager").objectReferenceValue = waveManager;

        // One slot per event, skipping the ones that already exist
        SerializedProperty list = so.FindProperty("sounds");
        HashSet<int> existing = new HashSet<int>();

        for (int i = 0; i < list.arraySize; i++)
        {
            existing.Add(list.GetArrayElementAtIndex(i).FindPropertyRelative("sfxEvent").enumValueIndex);
        }

        int added = 0;

        foreach (SfxEvent sfxEvent in Enum.GetValues(typeof(SfxEvent)))
        {
            if (sfxEvent == SfxEvent.None || existing.Contains((int)sfxEvent)) continue;

            list.arraySize++;
            SerializedProperty element = list.GetArrayElementAtIndex(list.arraySize - 1);

            element.FindPropertyRelative("sfxEvent").enumValueIndex = (int)sfxEvent;
            element.FindPropertyRelative("clips").arraySize = 0;
            element.FindPropertyRelative("volume").floatValue = 1f;
            element.FindPropertyRelative("pitchVariation").floatValue = 0.05f;
            added++;
        }

        so.ApplyModifiedProperties();

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Selection.activeGameObject = manager.gameObject;
        Debug.Log($"Audio Manager ready ({added} sound slots added). Drop your clips into its Sounds list.");
    }
}