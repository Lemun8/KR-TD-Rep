using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// One-click HUD builder: TD > Build HUD. Creates the whole layout under the Canvas and wires every reference.
public static class HudBuilder
{
    private static readonly Color PanelColor = new Color(0.07f, 0.09f, 0.13f, 0.88f);
    private static readonly Color ChipColor = new Color(1f, 1f, 1f, 0.08f);
    private static readonly Color DimText = new Color(1f, 1f, 1f, 0.6f);

    // Gold, Wood, Rocks, Steel, Population (same order as the ResourceType enum)
    private static readonly Color[] ResourceColors =
    {
        new Color(1f, 0.82f, 0.2f),
        new Color(0.62f, 0.4f, 0.22f),
        new Color(0.62f, 0.64f, 0.68f),
        new Color(0.45f, 0.68f, 0.9f),
        new Color(0.45f, 0.85f, 0.5f)
    };

    [MenuItem("TD/Build HUD")]
    private static void Build()
    {
        Canvas canvas = Object.FindFirstObjectByType<Canvas>();

        if (canvas == null)
        {
            Debug.LogError("Build HUD: no Canvas in the scene. Create one first (right-click Hierarchy > UI > Canvas).");
            return;
        }

        if (canvas.transform.Find("HUD") != null)
        {
            Debug.LogWarning("Build HUD: a HUD already exists. Delete the 'HUD' object under the Canvas to rebuild it.");
            return;
        }

        // The HUD is the FIRST child of the canvas, so menus and overlays always draw on top of it
        RectTransform hud = NewRect("HUD", canvas.transform);
        Stretch(hud);
        hud.SetAsFirstSibling();
        Undo.RegisterCreatedObjectUndo(hud.gameObject, "Build HUD");

        Image topBar = NewImage("TopBar", hud, PanelColor);
        Anchor(topBar.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, 90f));

        BuildResources(topBar.transform);
        BuildVillageHealth(topBar.transform);
        BuildWavePanel(hud);

        DisableDebugHuds();

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Selection.activeGameObject = hud.gameObject;
        Debug.Log("HUD built. Press Play to see it.");
    }

    // ---------- Top bar: resources ----------

    private static void BuildResources(Transform topBar)
    {
        RectTransform container = NewRect("Resources", topBar);
        container.anchorMin = new Vector2(0f, 0f);
        container.anchorMax = new Vector2(0.66f, 1f);
        container.offsetMin = new Vector2(20f, 13f);
        container.offsetMax = new Vector2(0f, -13f);

        HorizontalLayoutGroup layout = container.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 16f;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        int count = System.Enum.GetValues(typeof(ResourceType)).Length;
        TextMeshProUGUI[] valueTexts = new TextMeshProUGUI[count];

        for (int i = 0; i < count; i++)
        {
            ResourceType type = (ResourceType)i;

            Image chip = NewImage("Res_" + type, container, ChipColor);
            chip.rectTransform.sizeDelta = new Vector2(190f, 64f);

            Image dot = NewImage("Dot", chip.transform, ResourceColors[Mathf.Min(i, ResourceColors.Length - 1)]);
            Anchor(dot.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(14f, 0f), new Vector2(26f, 26f));

            TextMeshProUGUI caption = NewText("Name", chip.transform, type.ToString().ToUpper(), 15f, TextAlignmentOptions.TopLeft, DimText, false);
            Stretch(caption.rectTransform, 52f, 0f, 8f, 7f);

            TextMeshProUGUI value = NewText("Value", chip.transform, "0", 32f, TextAlignmentOptions.BottomLeft, Color.white, true);
            Stretch(value.rectTransform, 52f, 5f, 8f, 0f);

            valueTexts[i] = value;
        }

        ResourceBarUI bar = container.gameObject.AddComponent<ResourceBarUI>();

        SerializedObject so = new SerializedObject(bar);
        SerializedProperty list = so.FindProperty("displays");
        list.arraySize = count;

        for (int i = 0; i < count; i++)
        {
            SerializedProperty element = list.GetArrayElementAtIndex(i);
            element.FindPropertyRelative("type").enumValueIndex = i;
            element.FindPropertyRelative("valueText").objectReferenceValue = valueTexts[i];
        }

        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // ---------- Top bar: village health ----------

    private static void BuildVillageHealth(Transform topBar)
    {
        RectTransform container = NewRect("VillageHealth", topBar);
        container.anchorMin = new Vector2(0.7f, 0f);
        container.anchorMax = new Vector2(1f, 1f);
        container.offsetMin = new Vector2(0f, 13f);
        container.offsetMax = new Vector2(-20f, -13f);

        TextMeshProUGUI caption = NewText("Caption", container, "VILLAGE", 15f, TextAlignmentOptions.TopLeft, DimText, false);
        Stretch(caption.rectTransform, 2f, 0f, 0f, 0f);

        Image barBackground = NewImage("BarBackground", container, new Color(0f, 0f, 0f, 0.55f));
        Region(barBackground.rectTransform, 0f, 0f, 1f, 0.72f);

        Image fill = NewImage("Fill", barBackground.transform, new Color(0.35f, 0.8f, 0.4f));
        Stretch(fill.rectTransform);

        TextMeshProUGUI value = NewText("Value", barBackground.transform, "0 / 0", 24f, TextAlignmentOptions.Center, Color.white, true);
        Stretch(value.rectTransform);

        VillageHealthBarUI healthBar = container.gameObject.AddComponent<VillageHealthBarUI>();
        Assign(healthBar, "fill", fill.rectTransform);
        Assign(healthBar, "fillImage", fill);
        Assign(healthBar, "label", value);
    }

    // ---------- Wave panel, start button and banner ----------

    private static void BuildWavePanel(Transform hud)
    {
        Image panel = NewImage("WavePanel", hud, PanelColor);
        Anchor(panel.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -102f), new Vector2(480f, 122f));

        TextMeshProUGUI waveText = NewText("WaveText", panel.transform, "WAVE 1 / 5", 36f, TextAlignmentOptions.Center, Color.white, true);
        Region(waveText.rectTransform, 0f, 0.5f, 1f, 1f);

        TextMeshProUGUI statusText = NewText("StatusText", panel.transform, "", 24f, TextAlignmentOptions.Center, new Color(1f, 1f, 1f, 0.85f), false);
        Region(statusText.rectTransform, 0f, 0.26f, 1f, 0.54f);

        Image prepBar = NewImage("PrepBar", panel.transform, new Color(0f, 0f, 0f, 0.55f));
        Region(prepBar.rectTransform, 0.06f, 0.09f, 0.94f, 0.24f);

        Image prepFill = NewImage("Fill", prepBar.transform, new Color(1f, 0.78f, 0.25f));
        Stretch(prepFill.rectTransform);

        // Start Wave Now button, just below the panel
        Image buttonImage = NewImage("StartWaveButton", hud, new Color(0.95f, 0.65f, 0.15f));
        buttonImage.raycastTarget = true;
        Button button = buttonImage.gameObject.AddComponent<Button>();
        button.targetGraphic = buttonImage;
        Anchor(buttonImage.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -236f), new Vector2(280f, 56f));

        TextMeshProUGUI buttonText = NewText("Label", buttonImage.transform, "START WAVE NOW", 24f, TextAlignmentOptions.Center, new Color(0.12f, 0.08f, 0.02f), true);
        Stretch(buttonText.rectTransform);

        // Banner in the middle of the screen
        Image banner = NewImage("Banner", hud, new Color(0f, 0f, 0f, 0.6f));
        Anchor(banner.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 140f), new Vector2(760f, 130f));

        CanvasGroup group = banner.gameObject.AddComponent<CanvasGroup>();
        group.alpha = 0f;
        group.blocksRaycasts = false;
        group.interactable = false;

        TextMeshProUGUI bannerText = NewText("Text", banner.transform, "", 64f, TextAlignmentOptions.Center, Color.white, true);
        Stretch(bannerText.rectTransform);

        WaveHudUI waveHud = panel.gameObject.AddComponent<WaveHudUI>();
        Assign(waveHud, "waveManager", Object.FindFirstObjectByType<WaveManager>());
        Assign(waveHud, "waveText", waveText);
        Assign(waveHud, "statusText", statusText);
        Assign(waveHud, "prepBarRoot", prepBar.gameObject);
        Assign(waveHud, "prepFill", prepFill.rectTransform);
        Assign(waveHud, "startWaveButton", button);
        Assign(waveHud, "banner", group);
        Assign(waveHud, "bannerText", bannerText);

        if (Object.FindFirstObjectByType<WaveManager>() == null)
        {
            Debug.LogWarning("Build HUD: no WaveManager found in the scene. Assign it on the WavePanel's WaveHudUI component.");
        }
    }

    // The old temporary text HUDs are no longer needed
    private static void DisableDebugHuds()
    {
        WaveManager waveManager = Object.FindFirstObjectByType<WaveManager>();
        if (waveManager != null) SetBool(waveManager, "showDebugHUD", false);

        ResourceManager resourceManager = Object.FindFirstObjectByType<ResourceManager>();
        if (resourceManager != null) SetBool(resourceManager, "showDebugHUD", false);
    }

    // ---------- Small helpers ----------

    private static RectTransform NewRect(string objectName, Transform parent)
    {
        GameObject go = new GameObject(objectName, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    private static Image NewImage(string objectName, Transform parent, Color color)
    {
        RectTransform rect = NewRect(objectName, parent);
        Image image = rect.gameObject.AddComponent<Image>();

        // Unity's built-in rounded rectangle, so panels get soft corners without any art
        image.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        image.type = Image.Type.Sliced;
        image.color = color;
        image.raycastTarget = false;

        return image;
    }

    private static TextMeshProUGUI NewText(string objectName, Transform parent, string text, float size,
        TextAlignmentOptions alignment, Color color, bool bold)
    {
        RectTransform rect = NewRect(objectName, parent);
        TextMeshProUGUI tmp = rect.gameObject.AddComponent<TextMeshProUGUI>();

        tmp.text = text;
        tmp.fontSize = size;
        tmp.alignment = alignment;
        tmp.color = color;
        tmp.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
        tmp.raycastTarget = false;

        return tmp;
    }

    // Fills the parent, with optional margins
    private static void Stretch(RectTransform rect, float left = 0f, float bottom = 0f, float right = 0f, float top = 0f)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(left, bottom);
        rect.offsetMax = new Vector2(-right, -top);
    }

    // Fills a fraction of the parent (0 to 1 on each side)
    private static void Region(RectTransform rect, float minX, float minY, float maxX, float maxY)
    {
        rect.anchorMin = new Vector2(minX, minY);
        rect.anchorMax = new Vector2(maxX, maxY);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void Anchor(RectTransform rect, Vector2 min, Vector2 max, Vector2 pivot, Vector2 position, Vector2 size)
    {
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.pivot = pivot;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private static void Assign(Object target, string fieldName, Object value)
    {
        SerializedObject so = new SerializedObject(target);
        SerializedProperty property = so.FindProperty(fieldName);

        if (property == null)
        {
            Debug.LogWarning($"Build HUD: field '{fieldName}' not found on {target.GetType().Name}.");
            return;
        }

        property.objectReferenceValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetBool(Object target, string fieldName, bool value)
    {
        SerializedObject so = new SerializedObject(target);
        SerializedProperty property = so.FindProperty(fieldName);

        if (property == null) return;

        property.boolValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }
}