using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// One-click builder: TD > Build Main Menu. Creates the whole main menu under the Canvas and wires it up.
public static class MainMenuBuilder
{
    private static readonly Color PanelColor = new Color(0.07f, 0.09f, 0.13f, 0.92f);
    private static readonly Color AccentColor = new Color(0.95f, 0.65f, 0.15f);
    private static readonly Color AccentText = new Color(0.12f, 0.08f, 0.02f);

    [MenuItem("TD/Build Main Menu")]
    private static void Build()
    {
        Canvas canvas = Object.FindFirstObjectByType<Canvas>();

        if (canvas == null)
        {
            Debug.LogError("Build Main Menu: no Canvas in the scene. Right-click the Hierarchy > UI > Canvas first.");
            return;
        }

        if (canvas.transform.Find("MainMenu") != null)
        {
            Debug.LogWarning("Build Main Menu: a MainMenu already exists. Delete it to rebuild.");
            return;
        }

        // Makes the menu scale with the screen size
        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler != null)
        {
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
        }

        RectTransform root = NewRect("MainMenu", canvas.transform);
        Stretch(root);
        Undo.RegisterCreatedObjectUndo(root.gameObject, "Build Main Menu");

        // Splash art goes here. Drag your sprite into this Image's Source Image later.
        Image background = NewImage("Background", root, new Color(0.1f, 0.12f, 0.16f));
        background.sprite = null;
        background.type = Image.Type.Simple;
        Stretch(background.rectTransform);

        // ----- Main panel: Start / Options / Quit -----
        RectTransform mainPanel = NewRect("MainPanel", root);
        Anchor(mainPanel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -190f), new Vector2(380f, 300f));

        Button start = NewMenuButton("StartButton", mainPanel, "START", 0f);
        Button options = NewMenuButton("OptionsButton", mainPanel, "OPTIONS", -110f);
        Button quit = NewMenuButton("QuitButton", mainPanel, "QUIT", -220f);

        // ----- Options panel: three volume sliders -----
        Image card = NewImage("OptionsPanel", root, PanelColor);
        Anchor(card.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(800f, 600f));

        TextMeshProUGUI title = NewText("Title", card.transform, "OPTIONS", 56f, TextAlignmentOptions.Center, Color.white, true);
        Region(title.rectTransform, 0f, 0.84f, 1f, 0.98f);

        BuildVolumeRow(card.transform, "MASTER", VolumeChannel.Master, -150f);
        BuildVolumeRow(card.transform, "MUSIC", VolumeChannel.Bgm, -250f);
        BuildVolumeRow(card.transform, "SFX", VolumeChannel.Sfx, -350f);

        Button back = NewButton("BackButton", card.transform, "BACK", AccentColor, AccentText, 32f);
        Anchor((RectTransform)back.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 40f), new Vector2(300f, 64f));

        // ----- Wire the controller -----
        MainMenuUI menu = root.gameObject.AddComponent<MainMenuUI>();
        Assign(menu, "startButton", start);
        Assign(menu, "optionsButton", options);
        Assign(menu, "quitButton", quit);
        Assign(menu, "backButton", back);
        Assign(menu, "mainPanel", mainPanel.gameObject);
        Assign(menu, "optionsPanel", card.gameObject);

        card.gameObject.SetActive(false);

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Selection.activeGameObject = root.gameObject;
        Debug.Log("Main menu built. Next: put your splash art on MainMenu/Background and type your game scene's name on the MainMenuUI component.");
    }

    // ---------- Pieces ----------

    private static Button NewMenuButton(string objectName, Transform parent, string label, float y)
    {
        Button button = NewButton(objectName, parent, label, AccentColor, AccentText, 36f);
        Anchor((RectTransform)button.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(380f, 84f));
        return button;
    }

    private static void BuildVolumeRow(Transform parent, string label, VolumeChannel channel, float y)
    {
        RectTransform row = NewRect(label + "_Row", parent);
        Anchor(row, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(680f, 70f));

        TextMeshProUGUI caption = NewText("Label", row, label, 30f, TextAlignmentOptions.MidlineLeft, Color.white, true);
        Region(caption.rectTransform, 0f, 0f, 0.24f, 1f);

        Slider slider = NewSlider("Slider", row);
        Region((RectTransform)slider.transform, 0.26f, 0f, 0.82f, 1f);

        TextMeshProUGUI value = NewText("Value", row, "100%", 28f, TextAlignmentOptions.MidlineRight, new Color(1f, 1f, 1f, 0.85f), false);
        Region(value.rectTransform, 0.84f, 0f, 1f, 1f);

        VolumeSliderUI ui = slider.gameObject.AddComponent<VolumeSliderUI>();

        SerializedObject so = new SerializedObject(ui);
        so.FindProperty("channel").enumValueIndex = (int)channel;
        so.FindProperty("slider").objectReferenceValue = slider;
        so.FindProperty("valueLabel").objectReferenceValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // A slider made from code: track, fill and handle
    private static Slider NewSlider(string objectName, Transform parent)
    {
        RectTransform root = NewRect(objectName, parent);

        Image track = NewImage("Background", root, new Color(0f, 0f, 0f, 0.55f));
        track.raycastTarget = true;
        Region(track.rectTransform, 0f, 0.35f, 1f, 0.65f);

        RectTransform fillArea = NewRect("Fill Area", root);
        Region(fillArea, 0f, 0.35f, 1f, 0.65f);

        Image fill = NewImage("Fill", fillArea, AccentColor);
        Stretch(fill.rectTransform);

        RectTransform handleArea = NewRect("Handle Slide Area", root);
        Stretch(handleArea);
        handleArea.offsetMin = new Vector2(14f, 0f);
        handleArea.offsetMax = new Vector2(-14f, 0f);

        Image handle = NewImage("Handle", handleArea, Color.white);
        handle.raycastTarget = true;
        handle.rectTransform.anchorMin = new Vector2(0f, 0.15f);
        handle.rectTransform.anchorMax = new Vector2(0f, 0.85f);
        handle.rectTransform.sizeDelta = new Vector2(28f, 0f);

        Slider slider = root.gameObject.AddComponent<Slider>();
        slider.fillRect = fill.rectTransform;
        slider.handleRect = handle.rectTransform;
        slider.targetGraphic = handle;
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 1f;

        return slider;
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

    private static Button NewButton(string objectName, Transform parent, string label, Color color, Color textColor, float textSize)
    {
        Image image = NewImage(objectName, parent, color);
        image.raycastTarget = true;

        Button button = image.gameObject.AddComponent<Button>();
        button.targetGraphic = image;

        TextMeshProUGUI text = NewText("Label", image.transform, label, textSize, TextAlignmentOptions.Center, textColor, true);
        Stretch(text.rectTransform);

        return button;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

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
            Debug.LogWarning($"Build Main Menu: field '{fieldName}' not found on {target.GetType().Name}.");
            return;
        }

        property.objectReferenceValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }
}