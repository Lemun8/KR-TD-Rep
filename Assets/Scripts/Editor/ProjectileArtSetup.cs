using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.U2D.Sprites;
using UnityEngine;

// TD > Setup Projectile Art
// 1) Slices the projectile sheet (import settings only, the PNG itself is never touched)
// 2) Builds the animations: mage bolt loop, explosion, mage impact
// 3) Creates the prefabs: Arrow, Cannonball, MageBolt, ExplosionEffect, MageImpactEffect
// Existing animations/prefabs are refreshed or left as they are, so your own tweaks are never overwritten.
public static class ProjectileArtSetup
{
    // Change this if you keep the sheet somewhere else
    private const string SheetPath = "Assets/Art/Projectiles_Sheet.png";
    private const string AnimFolder = "Assets/Animations/Projectiles";
    private const string PrefabFolder = "Assets/Prefabs/Projectiles";

    private const int SheetWidth = 902;
    private const int SheetHeight = 276;

    // One explosion frame is 112 px wide, so at 112 pixels per unit it is exactly 1 unit wide.
    // That is what the Projectile script assumes when it scales an impact effect to the splash radius.
    private const float PixelsPerUnit = 112f;

    private const int ProjectileSortingOrder = 20;

    private struct SliceInfo
    {
        public string name;
        public int x, y, w, h;      // pixels, measured from the TOP-LEFT of the image
        public Vector2 pivot;       // 0-1 inside the slice

        public SliceInfo(string name, int x, int y, int w, int h, Vector2 pivot)
        {
            this.name = name;
            this.x = x;
            this.y = y;
            this.w = w;
            this.h = h;
            this.pivot = pivot;
        }
    }

    [MenuItem("TD/Setup Projectile Art")]
    private static void SetupAll()
    {
        if (!SliceSheet()) return;

        AssetDatabase.Refresh();

        Dictionary<string, Sprite> sprites = AssetDatabase.LoadAllAssetsAtPath(SheetPath)
            .OfType<Sprite>()
            .ToDictionary(s => s.name, s => s);

        EnsureFolder(AnimFolder);
        EnsureFolder(PrefabFolder);

        // Animations
        AnimationClip boltClip = MakeClip("MageBolt", sprites, "MageBolt_", 4, 12f, true);
        AnimationClip explosionClip = MakeClip("Explosion", sprites, "Explosion_", 6, 14f, false);
        AnimationClip impactClip = MakeClip("MageImpact", sprites, "MageImpact_", 5, 14f, false);

        RuntimeAnimatorController boltController = MakeController("MageBolt", boltClip);
        RuntimeAnimatorController explosionController = MakeController("Explosion", explosionClip);
        RuntimeAnimatorController impactController = MakeController("MageImpact", impactClip);

        // Impact effects first, because the projectiles point at them
        GameObject explosionEffect = MakeEffectPrefab("ExplosionEffect", sprites["Explosion_0"], explosionController);
        GameObject impactEffect = MakeEffectPrefab("MageImpactEffect", sprites["MageImpact_0"], impactController);

        // Projectiles:            name,         sprite,                 animator,       scale, speed, homing, splash, effect,          effect time
        MakeProjectilePrefab("Arrow",      sprites["Arrow"],      null,           0.4f, 9f, false, 0f, null,            0f);
        MakeProjectilePrefab("Cannonball", sprites["Cannonball"], null,           0.6f, 6f, false, 1f, explosionEffect, 0.45f);
        MakeProjectilePrefab("MageBolt",   sprites["MageBolt_0"], boltController, 0.6f, 8f, true,  0f, impactEffect,    0.4f);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"Projectile art ready. Prefabs are in {PrefabFolder}, animations in {AnimFolder}.");
    }

    // ---------- 1) Slicing ----------

    private static List<SliceInfo> BuildSlices()
    {
        List<SliceInfo> slices = new List<SliceInfo>();
        Vector2 center = new Vector2(0.5f, 0.5f);

        // Pivot on the tip of the arrow, so the tip is what reaches the enemy
        slices.Add(new SliceInfo("Arrow", 10, 18, 132, 38, new Vector2(0.95f, 0.5f)));
        slices.Add(new SliceInfo("Cannonball", 157, 12, 50, 50, center));

        // Pivot on the glowing head of the bolt (the trail is behind it)
        int[] boltX = { 228, 399, 571, 743 };
        for (int i = 0; i < boltX.Length; i++)
        {
            slices.Add(new SliceInfo($"MageBolt_{i}", boltX[i], 4, 102, 66, new Vector2(72f / 102f, 0.5f)));
        }

        // Explosion: 6 equal squares, the effect stays centered while it grows
        for (int i = 0; i < 6; i++)
        {
            slices.Add(new SliceInfo($"Explosion_{i}", 112 * i, 74, 112, 112, center));
        }

        // Mage impact rings: 5 squares centered on each ring
        int[] impactX = { 1, 90, 180, 270, 359 };
        for (int i = 0; i < impactX.Length; i++)
        {
            slices.Add(new SliceInfo($"MageImpact_{i}", impactX[i], 187, 88, 88, center));
        }

        return slices;
    }

    private static bool SliceSheet()
    {
        TextureImporter importer = AssetImporter.GetAtPath(SheetPath) as TextureImporter;

        if (importer == null)
        {
            Debug.LogError($"Projectile art setup: no texture found at {SheetPath}. Put the sheet there (or change SheetPath in the script).");
            return false;
        }

        // Import settings only. The PNG itself is not changed.
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = PixelsPerUnit;
        importer.mipmapEnabled = false;
        importer.filterMode = FilterMode.Bilinear;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();

        SpriteDataProviderFactories factory = new SpriteDataProviderFactories();
        factory.Init();

        ISpriteEditorDataProvider provider = factory.GetSpriteEditorDataProviderFromObject(importer);
        provider.InitSpriteEditorDataProvider();

        ITextureDataProvider textureProvider = provider.GetDataProvider<ITextureDataProvider>();
        textureProvider.GetTextureActualWidthAndHeight(out int width, out int height);

        if (width != SheetWidth || height != SheetHeight)
        {
            Debug.LogError($"Projectile art setup: the sheet is {width}x{height} but {SheetWidth}x{SheetHeight} was expected. " +
                           "This tool is made for the original projectile sheet.");
            return false;
        }

        // Keep the ids of sprites that already exist, so animations and prefabs using them do not break
        Dictionary<string, UnityEditor.GUID> existing = new Dictionary<string, UnityEditor.GUID>();
        foreach (SpriteRect old in provider.GetSpriteRects()) existing[old.name] = old.spriteID;

        List<SpriteRect> rects = new List<SpriteRect>();

        foreach (SliceInfo s in BuildSlices())
        {
            rects.Add(new SpriteRect
            {
                name = s.name,
                // Unity measures from the BOTTOM-left, so flip the y
                rect = new Rect(s.x, height - (s.y + s.h), s.w, s.h),
                alignment = SpriteAlignment.Custom,
                pivot = s.pivot,
                spriteID = existing.TryGetValue(s.name, out UnityEditor.GUID id) ? id : UnityEditor.GUID.Generate()
            });
        }

        provider.SetSpriteRects(rects.ToArray());

        ISpriteNameFileIdDataProvider nameProvider = provider.GetDataProvider<ISpriteNameFileIdDataProvider>();
        nameProvider.SetNameFileIdPairs(rects.Select(r => new SpriteNameFileIdPair(r.name, r.spriteID)).ToList());

        provider.Apply();
        importer.SaveAndReimport();

        Debug.Log($"Projectile sheet sliced into {rects.Count} sprites.");
        return true;
    }

    // ---------- 2) Animations ----------

    private static AnimationClip MakeClip(string clipName, Dictionary<string, Sprite> sprites, string prefix, int count, float fps, bool loop)
    {
        string path = $"{AnimFolder}/{clipName}.anim";
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
        ObjectReferenceKeyframe[] keys = new ObjectReferenceKeyframe[count + 1];

        for (int i = 0; i < count; i++)
        {
            keys[i] = new ObjectReferenceKeyframe { time = i / fps, value = sprites[prefix + i] };
        }

        keys[count] = new ObjectReferenceKeyframe { time = count / fps, value = sprites[prefix + (count - 1)] };

        AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);

        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = loop;
        settings.startTime = 0f;
        settings.stopTime = count / fps;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        EditorUtility.SetDirty(clip);
        return clip;
    }

    // A controller with one single state that plays the clip as soon as the object appears
    private static RuntimeAnimatorController MakeController(string controllerName, AnimationClip clip)
    {
        string path = $"{AnimFolder}/{controllerName}.controller";
        AnimatorController existing = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
        if (existing != null) return existing;

        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(path);
        AnimatorState state = controller.layers[0].stateMachine.AddState("Play");
        state.motion = clip;
        controller.layers[0].stateMachine.defaultState = state;

        EditorUtility.SetDirty(controller);
        return controller;
    }

    // ---------- 3) Prefabs ----------

    private static GameObject MakeEffectPrefab(string prefabName, Sprite firstFrame, RuntimeAnimatorController controller)
    {
        string path = $"{PrefabFolder}/{prefabName}.prefab";
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);

        if (existing != null)
        {
            Debug.LogWarning($"{prefabName}.prefab already exists, so it was left as it is.");
            return existing;
        }

        GameObject go = new GameObject(prefabName);

        SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = firstFrame;
        renderer.sortingOrder = ProjectileSortingOrder;

        Animator animator = go.AddComponent<Animator>();
        animator.runtimeAnimatorController = controller;

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
        return prefab;
    }

    private static void MakeProjectilePrefab(string prefabName, Sprite firstFrame, RuntimeAnimatorController controller,
        float scale, float speed, bool homing, float splashRadius, GameObject impactEffect, float impactEffectDuration)
    {
        string path = $"{PrefabFolder}/{prefabName}.prefab";

        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
        {
            Debug.LogWarning($"{prefabName}.prefab already exists, so it was left as it is.");
            return;
        }

        GameObject go = new GameObject(prefabName);
        go.transform.localScale = Vector3.one * scale;

        SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = firstFrame;
        renderer.sortingOrder = ProjectileSortingOrder;

        if (controller != null)
        {
            Animator animator = go.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;
        }

        Projectile projectile = go.AddComponent<Projectile>();

        SerializedObject so = new SerializedObject(projectile);
        so.FindProperty("speed").floatValue = speed;
        so.FindProperty("homing").boolValue = homing;
        so.FindProperty("splashRadius").floatValue = splashRadius;
        so.FindProperty("impactEffectPrefab").objectReferenceValue = impactEffect;
        so.FindProperty("impactEffectDuration").floatValue = impactEffectDuration;
        so.ApplyModifiedPropertiesWithoutUndo();

        PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
    }

    // ---------- Helpers ----------

    // Creates every missing folder in a path like "Assets/Prefabs/Projectiles"
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