using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

// TD > Setup Unit Animations
// Builds the clips and the Animator Controller (Idle / Walk / Attack / Death) for every unit in the list below,
// from the SLICED sprite sheets. It never touches the PNGs, it only reads the sprites sliced from them.
// Units whose sheet is missing or not sliced yet are skipped with a warning.
//
// Adding a future unit = add one entry to the Units list, slice its sheet, run the menu item again.
public static class UnitAnimationSetup
{
    private class UnitAnim
    {
        public string name;          // used for the clip names, the controller name and the output folder
        public string sheetPath;
        public int spriteCount;      // how many sprites the sliced sheet must have
        public int[] idle, walk, attack, death;
        public float idleFps = 6f;
        public float walkFps = 10f;
        public float attackFps = 10f;
        public float deathFps = 8f;
    }

    private const string OutputRoot = "Assets/Animations/Units";

    // Frame numbers are the sliced sprite numbers: ..._0, _1, _2 ... counting row by row from the top-left.
    private static readonly UnitAnim[] Units =
    {
        // Soldier sheet: row 1 = idle (4), row 2 = walk (6), row 3 = attack (4), row 4 = hurt + death (4)
        new UnitAnim
        {
            name = "Soldier",
            sheetPath = "Assets/Art/Units/Soldier_Sheet.png",
            spriteCount = 18,
            idle = new[] { 0, 1, 2, 3 },
            walk = new[] { 4, 5, 6, 7, 8, 9 },
            attack = new[] { 10, 11, 12, 13 },
            death = new[] { 14, 15, 16, 17 }
        },

        // Goblin sheet: row 1 = walk (6), row 2 = attack (4), row 3 = hurt + death (4). There is no idle row,
        // so the idle is the first attack frame (the "weapon ready" pose), held still.
        new UnitAnim
        {
            name = "Goblin",
            sheetPath = "Assets/Art/Units/Goblin_Sheet.png",
            spriteCount = 14,
            idle = new[] { 6 },
            walk = new[] { 0, 1, 2, 3, 4, 5 },
            attack = new[] { 6, 7, 8, 9 },
            death = new[] { 10, 11, 12, 13 }
        },

        // Orc and Wulf: SAME layout as the goblin is assumed. If their sheets are laid out differently,
        // the sprite count check will complain and you change the frame numbers here.
        new UnitAnim
        {
            name = "Orc",
            sheetPath = "Assets/Art/Units/Orc_Sheet.png",
            spriteCount = 14,
            idle = new[] { 6 },
            walk = new[] { 0, 1, 2, 3, 4, 5 },
            attack = new[] { 6, 7, 8, 9 },
            death = new[] { 10, 11, 12, 13 }
        },
        new UnitAnim
        {
            name = "Wulf",
            sheetPath = "Assets/Art/Units/Wulf_Sheet.png",
            spriteCount = 14,
            idle = new[] { 6 },
            walk = new[] { 0, 1, 2, 3, 4, 5 },
            attack = new[] { 6, 7, 8, 9 },
            death = new[] { 10, 11, 12, 13 }
        }
    };

    [MenuItem("TD/Setup Unit Animations")]
    private static void SetupAll()
    {
        int built = 0;

        foreach (UnitAnim unit in Units)
        {
            if (Build(unit)) built++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"Unit animations: {built} of {Units.Length} built. Output is in {OutputRoot}.");
    }

    private static bool Build(UnitAnim unit)
    {
        Sprite[] sprites = AssetDatabase.LoadAllAssetsAtPath(unit.sheetPath)
            .OfType<Sprite>()
            .OrderBy(s => Number(s.name))
            .ToArray();

        if (sprites.Length == 0)
        {
            Debug.LogWarning($"{unit.name}: no sliced sprites found at {unit.sheetPath}. Skipped (wrong path, or the sheet is not sliced yet).");
            return false;
        }

        if (sprites.Length != unit.spriteCount)
        {
            Debug.LogError($"{unit.name}: found {sprites.Length} sprites, expected {unit.spriteCount}. " +
                           "Check the slice settings, and that 'Keep Empty Rects' is OFF. Skipped.");
            return false;
        }

        string folder = $"{OutputRoot}/{unit.name}";
        EnsureFolder(folder);

        AnimationClip idle = MakeClip(folder, $"{unit.name}_Idle", sprites, unit.idle, unit.idleFps, true);
        AnimationClip walk = MakeClip(folder, $"{unit.name}_Walk", sprites, unit.walk, unit.walkFps, true);
        AnimationClip attack = MakeClip(folder, $"{unit.name}_Attack", sprites, unit.attack, unit.attackFps, false);
        AnimationClip death = MakeClip(folder, $"{unit.name}_Death", sprites, unit.death, unit.deathFps, false);

        MakeController(folder, unit.name, idle, walk, attack, death);
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

    private static void MakeController(string folder, string unitName, AnimationClip idle, AnimationClip walk,
        AnimationClip attack, AnimationClip death)
    {
        string path = $"{folder}/{unitName}.controller";

        if (AssetDatabase.LoadAssetAtPath<AnimatorController>(path) != null)
        {
            Debug.LogWarning($"{unitName}.controller already exists, so it was left as it is (its clips were refreshed). Delete it to rebuild it.");
            return;
        }

        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(path);
        controller.AddParameter("Moving", AnimatorControllerParameterType.Bool);
        controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Death", AnimatorControllerParameterType.Trigger);

        AnimatorStateMachine machine = controller.layers[0].stateMachine;

        AnimatorState idleState = machine.AddState("Idle");
        idleState.motion = idle;
        machine.defaultState = idleState;

        AnimatorState walkState = machine.AddState("Walk");
        walkState.motion = walk;

        AnimatorState attackState = machine.AddState("Attack");
        attackState.motion = attack;

        AnimatorState deathState = machine.AddState("Death");
        deathState.motion = death;

        // Death is added first so it wins if an attack and a death happen in the same frame
        AnimatorStateTransition anyToDeath = machine.AddAnyStateTransition(deathState);
        anyToDeath.hasExitTime = false;
        anyToDeath.duration = 0f;
        anyToDeath.canTransitionToSelf = false;
        anyToDeath.AddCondition(AnimatorConditionMode.If, 0f, "Death");

        // Attack can interrupt idle / walk, and restarts itself for the next hit
        AnimatorStateTransition anyToAttack = machine.AddAnyStateTransition(attackState);
        anyToAttack.hasExitTime = false;
        anyToAttack.duration = 0f;
        anyToAttack.canTransitionToSelf = true;
        anyToAttack.AddCondition(AnimatorConditionMode.If, 0f, "Attack");

        // Idle <-> Walk follows the Moving flag
        AnimatorStateTransition toWalk = idleState.AddTransition(walkState);
        toWalk.hasExitTime = false;
        toWalk.duration = 0f;
        toWalk.AddCondition(AnimatorConditionMode.If, 0f, "Moving");

        AnimatorStateTransition toIdle = walkState.AddTransition(idleState);
        toIdle.hasExitTime = false;
        toIdle.duration = 0f;
        toIdle.AddCondition(AnimatorConditionMode.IfNot, 0f, "Moving");

        // Attack plays once, then back to idle (idle moves on to walk by itself if the unit is moving)
        AnimatorStateTransition attackToIdle = attackState.AddTransition(idleState);
        attackToIdle.hasExitTime = true;
        attackToIdle.exitTime = 1f;
        attackToIdle.duration = 0f;

        // Death has no way out: the last frame stays on the ground until the body is removed

        EditorUtility.SetDirty(controller);
    }

    // ---------- Helpers ----------

    // "Goblin_Sheet_7" -> 7
    private static int Number(string spriteName)
    {
        int underscore = spriteName.LastIndexOf('_');

        if (underscore >= 0 && int.TryParse(spriteName.Substring(underscore + 1), out int number))
        {
            return number;
        }

        return -1;
    }

    // Creates every missing folder in a path like "Assets/Animations/Units/Goblin"
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