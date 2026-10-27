using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

// TD > Setup Tower Animations
// Builds the clips and Animator Controller for EVERY tower in the list below, from the SLICED sprite sheets.
// It never touches the PNGs, it only reads the sprites that were sliced from them.
// Towers whose sheet is missing or not sliced yet are skipped with a warning.
//
// Adding a future tower = add one entry to the Towers list, slice its sheet, run the menu item again.
public static class TowerAnimationSetup
{
    private class TowerAnim
    {
        public string name;              // used for the clip names and the output folder
        public string controllerName;
        public string sheetPath;
        public int spriteCount;          // how many sprites the sliced sheet must have
        public int[] idleFrames;
        public int[] shootFrames;        // null = this building never shoots (no Shoot state)
        public float idleFps = 6f;
        public float shootFps = 12f;
    }

    private const string OutputRoot = "Assets/Animations/Towers";

    // Frame numbers are the sliced sprite numbers: ..._Sheet_0, _1, _2 ...
    // Top row first, then the bottom row (empty cells are skipped when 'Keep Empty Rects' is off).
    private static readonly TowerAnim[] Towers =
    {
        new TowerAnim
        {
            name = "Archer", controllerName = "ArcherTower",
            sheetPath = "Assets/Art/ArcherTower_Sheet.png",
            spriteCount = 9,
            idleFrames = new[] { 0, 1, 2, 3 },
            shootFrames = new[] { 4, 5, 6, 7, 8 }
        },
        new TowerAnim
        {
            name = "Mage", controllerName = "MageTower",
            sheetPath = "Assets/Art/MageTower_Sheet.png",
            spriteCount = 10,
            idleFrames = new[] { 0, 1, 2, 3 },
            shootFrames = new[] { 4, 5, 6, 7, 8, 9 }
        },
        new TowerAnim
        {
            name = "Artillery", controllerName = "ArtilleryTower",
            sheetPath = "Assets/Art/ArtilleryTower_Sheet.png",
            spriteCount = 10,
            idleFrames = new[] { 0, 1, 2, 3 },
            shootFrames = new[] { 4, 5, 6, 7, 8, 9 }
        },
        new TowerAnim
        {
            // Barracks only has an idle loop (waving flag), so it gets no Shoot state
            name = "Barracks", controllerName = "Barracks",
            sheetPath = "Assets/Art/Barracks_Sheet.png",
            spriteCount = 4,
            idleFrames = new[] { 0, 1, 2, 3 },
            shootFrames = null
        }
    };

    [MenuItem("TD/Setup Tower Animations")]
    private static void SetupAll()
    {
        int built = 0;

        foreach (TowerAnim tower in Towers)
        {
            if (Build(tower)) built++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"Tower animations: {built} of {Towers.Length} built. Output is in {OutputRoot}.");
    }

    private static bool Build(TowerAnim tower)
    {
        Sprite[] sprites = AssetDatabase.LoadAllAssetsAtPath(tower.sheetPath)
            .OfType<Sprite>()
            .OrderBy(s => Number(s.name))
            .ToArray();

        if (sprites.Length == 0)
        {
            Debug.LogWarning($"{tower.name}: no sliced sprites found at {tower.sheetPath}. Skipped (wrong path, or the sheet is not sliced yet).");
            return false;
        }

        if (sprites.Length != tower.spriteCount)
        {
            Debug.LogError($"{tower.name}: found {sprites.Length} sprites, expected {tower.spriteCount}. " +
                           "Slice with Grid By Cell Size 800 x 1160 and 'Keep Empty Rects' OFF. Skipped.");
            return false;
        }

        string folder = $"{OutputRoot}/{tower.name}";
        EnsureFolder(folder);

        AnimationClip idle = MakeClip(folder, $"{tower.name}_Idle", sprites, tower.idleFrames, tower.idleFps, true);
        AnimationClip shoot = tower.shootFrames != null
            ? MakeClip(folder, $"{tower.name}_Shoot", sprites, tower.shootFrames, tower.shootFps, false)
            : null;

        MakeController(folder, tower.controllerName, idle, shoot);
        return true;
    }

    // ---------- Clips ----------

    private static AnimationClip MakeClip(string folder, string clipName, Sprite[] sprites, int[] frames, float fps, bool loop)
    {
        string path = $"{folder}/{clipName}.anim";
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);

        if (clip == null)
        {
            clip = new AnimationClip();
            AssetDatabase.CreateAsset(clip, path);
        }

        clip.frameRate = fps;

        EditorCurveBinding binding = new EditorCurveBinding
        {
            type = typeof(SpriteRenderer),
            path = "",
            propertyName = "m_Sprite"
        };

        // One extra key at the very end, so the last frame is shown for its full duration
        ObjectReferenceKeyframe[] keys = new ObjectReferenceKeyframe[frames.Length + 1];

        for (int i = 0; i < frames.Length; i++)
        {
            keys[i] = new ObjectReferenceKeyframe { time = i / fps, value = sprites[frames[i]] };
        }

        keys[frames.Length] = new ObjectReferenceKeyframe
        {
            time = frames.Length / fps,
            value = sprites[frames[frames.Length - 1]]
        };

        AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);

        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = loop;
        settings.startTime = 0f;
        settings.stopTime = frames.Length / fps;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        EditorUtility.SetDirty(clip);
        return clip;
    }

    // ---------- Controller ----------

    private static void MakeController(string folder, string controllerName, AnimationClip idle, AnimationClip shoot)
    {
        string path = $"{folder}/{controllerName}.controller";

        if (AssetDatabase.LoadAssetAtPath<AnimatorController>(path) != null)
        {
            Debug.LogWarning($"{controllerName}.controller already exists, so it was left as it is (its clips were refreshed). Delete it to rebuild it.");
            return;
        }

        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(path);
        AnimatorStateMachine machine = controller.layers[0].stateMachine;

        AnimatorState idleState = machine.AddState("Idle");
        idleState.motion = idle;
        machine.defaultState = idleState;

        if (shoot != null)
        {
            controller.AddParameter("Shoot", AnimatorControllerParameterType.Trigger);

            AnimatorState shootState = machine.AddState("Shoot");
            shootState.motion = shoot;

            // Idle -> Shoot the moment the trigger is set
            AnimatorStateTransition toShoot = idleState.AddTransition(shootState);
            toShoot.hasExitTime = false;
            toShoot.duration = 0f;
            toShoot.AddCondition(AnimatorConditionMode.If, 0f, "Shoot");

            // Shoot -> Idle when the shoot clip finishes
            AnimatorStateTransition toIdle = shootState.AddTransition(idleState);
            toIdle.hasExitTime = true;
            toIdle.exitTime = 1f;
            toIdle.duration = 0f;
        }

        EditorUtility.SetDirty(controller);
    }

    // ---------- Helpers ----------

    // "MageTower_Sheet_7" -> 7
    private static int Number(string spriteName)
    {
        int underscore = spriteName.LastIndexOf('_');

        if (underscore >= 0 && int.TryParse(spriteName.Substring(underscore + 1), out int number))
        {
            return number;
        }

        return -1;
    }

    // Creates every missing folder in a path like "Assets/Animations/Towers/Mage"
    private static void EnsureFolder(string folderPath)
    {
        string[] parts = folderPath.Split('/');
        string current = parts[0];

        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];

            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(current, parts[i]);
            }

            current = next;
        }
    }
}