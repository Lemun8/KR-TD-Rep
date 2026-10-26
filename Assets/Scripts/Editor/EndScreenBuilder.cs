using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// One-click builder: TD > Build End Screen. Creates the victory/defeat overlay under the Canvas and wires it up.
public static class EndScreenBuilder
{
    private static readonly Color PanelColor = new Color(0.07f, 0.09f, 0.13f, 0.95f);

    [MenuItem("TD/Build End Screen")]
    private static void Build()
    {
        Canvas canvas = Object.FindFirstObjectByType<Canvas>();

        if (canvas == null)
        {
            Debug.LogError("Build End Screen: no Canvas in the scene.");
            return;
        }

        if (canvas.transform.Find("EndScreen") != null)
        {
            Debug.LogWarning("Build End Screen: an EndScreen already exists. Delete it to rebuild.");
            return;
        }

        // Last child of the canvas, so it draws on top of everything
        RectTransform root = NewRect("EndScreen", canvas.transform);
        Stretch(root);
        root.SetAsLastSibling();
        Undo.RegisterCreatedObjectUndo(root.gameObject, "Build End Screen");

        // Dark overlay that also blocks clicks on the game behind it
        Image overlay = NewImage("Overlay", root, new Color(0f, 0f, 0f, 0.78f));
        overlay.sprite = null;
        overlay.type = Image.Type.Simple;
        overlay.raycastTarget = true;
        Stretch(overlay.rectTransform);
        CanvasGroup group = overlay.gameObject.AddComponent<CanvasGroup>();

        Image card = NewImage("Card", overlay.transform, PanelColor);
        Anchor(card.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(720f, 560f));

        TextMeshProUGUI title = NewText("Title", card.transform, "VICTORY!", 96f, TextAlignmentOptions.Center, Color.white, true);
        Region(title.rectTransform, 0f, 0.78f, 1f, 0.97f);

        TextMeshProUGUI subtitle = NewText("Subtitle", card.transform, "", 30f, TextAlignmentOptions.Center, new Color(1f, 1f, 1f, 0.8f), false);
        Region(subtitle.rectTransform, 0.05f, 0.68f, 0.95f, 0.78f);

        TextMeshProUGUI stats = NewText("Stats", card.transform, "", 30f, TextAlignmentOptions.Center, Color.white, false);
        Region(stats.rectTransform, 0.08f, 0.28f, 0.92f, 0.66f);

        Button restart = NewButton("RestartButton", card.transform, "RESTART", new Color(0.3f, 0.75f, 0.4f), new Color(0.04f, 0.12f, 0.06f));
        Anchor((RectTransform)restart.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-160f, 40f), new Vector2(300f, 64f));

        Button quit = NewButton("QuitButton", card.transform, "QUIT", new Color(0.45f, 0.5f, 0.6f), Color.white);
        Anchor((RectTransform)quit.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(160f, 40f), new Vector2(300f, 64f));

        EndScreenUI endScreen = root.gameObject.AddComponent<EndScreenUI>();
        Assign(endScreen, "waveManager", Object.FindFirstObjectByType<WaveManager>());
        Assign(endScreen, "screen", overlay.gameObject);
        Assign(endScreen, "screenGroup", group);
        Assign(endScreen, "titleText", title);
        Assign(endScreen, "subtitleText", subtitle);
        Assign(endScreen, "statsText", stats);
        Assign(endScreen, "restartButton", restart);
        Assign(endScreen, "quitButton", quit);

        if (Object.FindFirstObjectByType<WaveManager>() == null)
        {
            Debug.LogWarning("Build End Screen: no WaveManager found. Assign it on the EndScreen's EndScreenUI component.");
        }

        // Hidden while editing; the script also hides it when the game starts
        overlay.gameObject.SetActive(false);

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Selection.activeGameObject = root.gameObject;
        Debug.Log("End screen built. It stays hidden until the game is won or lost.");
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

    private static Button NewButton(string objectName, Transform parent, string label, Color color, Color textColor)
    {
        Image image = NewImage(objectName, parent, color);
        image.raycastTarget = true;

        Button button = image.gameObject.AddComponent<Button>();
        button.targetGraphic = image;

        TextMeshProUGUI text = NewText("Label", image.transform, label, 28f, TextAlignmentOptions.Center, textColor, true);
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
            Debug.LogWarning($"Build End Screen: field '{fieldName}' not found on {target.GetType().Name}.");
            return;
        }

        property.objectReferenceValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }
}