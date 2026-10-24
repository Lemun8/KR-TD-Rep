using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BuildMenuUI : MonoBehaviour
{
    public static BuildMenuUI Instance { get; private set; }

    [Header("References")]
    [SerializeField] private RectTransform panel;
    [SerializeField] private Button closeBlocker;        // invisible full-screen button behind the panel
    [SerializeField] private Button optionButtonPrefab;
    [SerializeField] private Button rallyButton;         // only shown for barracks
    [SerializeField] private Button sellButton;
    [SerializeField] private RallyPointPlacer rallyPlacer;

    [Header("Buildings")]
    [SerializeField] private List<BuildingData> availableBuildings = new List<BuildingData>();

    [Header("Layout")]
    [SerializeField] private Vector2 panelOffset = new Vector2(0f, 110f);

    [SerializeField] private float screenMargin = 20f;

    private Canvas rootCanvas;

    private class Option
    {
        public BuildingData data;
        public Button button;
    }

    private readonly List<Option> options = new List<Option>();
    private BuildSpot currentSpot;
    private Camera mainCamera;

    private void Awake()
    {
        Instance = this;

        rootCanvas = GetComponentInParent<Canvas>().rootCanvas;

        foreach (BuildingData data in availableBuildings)
        {
            Button button = Instantiate(optionButtonPrefab, panel);

            TMP_Text label = button.GetComponentInChildren<TMP_Text>();
            label.text = $"{data.displayName}\n{data.GetCostText()}";

            BuildingData captured = data;
            button.onClick.AddListener(() => OnOptionClicked(captured));

            options.Add(new Option { data = data, button = button });
        }

        // Rally button above the sell button, both below the build options
        rallyButton.transform.SetAsLastSibling();
        sellButton.transform.SetAsLastSibling();

        rallyButton.onClick.AddListener(OnRallyClicked);
        sellButton.onClick.AddListener(OnSellClicked);
        closeBlocker.onClick.AddListener(Close);

        if (rallyPlacer != null) rallyPlacer.gameObject.SetActive(false);

        SetVisible(false);
    }

    private void Start()
    {
        mainCamera = Camera.main;

        if (ResourceManager.Instance != null)
        {
            ResourceManager.Instance.ResourceChanged += HandleResourceChanged;
        }
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;

        if (ResourceManager.Instance != null)
        {
            ResourceManager.Instance.ResourceChanged -= HandleResourceChanged;
        }
    }

    public void Open(BuildSpot spot)
    {
        currentSpot = spot;
        SetVisible(true);
        Refresh();   // also rebuilds the layout, so the panel size is correct below

        Vector2 screenPos = mainCamera.WorldToScreenPoint(spot.transform.position);
        float scale = rootCanvas.scaleFactor;

        // How far the panel extends from its pivot on each side, in screen pixels
        float left   = panel.rect.width  * panel.pivot.x * scale;
        float right  = panel.rect.width  * (1f - panel.pivot.x) * scale;
        float bottom = panel.rect.height * panel.pivot.y * scale;
        float top    = panel.rect.height * (1f - panel.pivot.y) * scale;

        Vector2 target = screenPos + panelOffset * scale;

        // Not enough room above the spot, so show the menu below it instead
        if (target.y + top > Screen.height - screenMargin)
        {
            target = screenPos + new Vector2(panelOffset.x, -panelOffset.y) * scale;
        }

        // Final safety clamp so no edge of the panel leaves the screen
        target.x = Mathf.Clamp(target.x, left + screenMargin, Screen.width - right - screenMargin);
        target.y = Mathf.Clamp(target.y, bottom + screenMargin, Screen.height - top - screenMargin);

        panel.position = target;
    }

    public void Close()
    {
        currentSpot = null;
        SetVisible(false);
    }

    private void Refresh()
    {
        if (currentSpot == null) return;

        foreach (Option option in options)
        {
            bool show = !currentSpot.IsOccupied && currentSpot.Allows(option.data);
            option.button.gameObject.SetActive(show);

            if (show)
            {
                option.button.interactable =
                    ResourceManager.Instance != null && ResourceManager.Instance.CanAfford(option.data.Cost);
            }
        }

        bool hasBarracks = currentSpot.IsOccupied && currentSpot.GetBuildingComponent<Barracks>() != null;
        rallyButton.gameObject.SetActive(hasBarracks);

        sellButton.gameObject.SetActive(currentSpot.IsOccupied);

        if (currentSpot.IsOccupied)
        {
            BuildingData data = currentSpot.CurrentData;
            TMP_Text label = sellButton.GetComponentInChildren<TMP_Text>();
            label.text = $"Sell {data.displayName}\n+{data.GetCostText(data.sellRefundPercent)}";
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(panel);
    }

    private void OnOptionClicked(BuildingData data)
    {
        if (currentSpot != null && currentSpot.TryBuild(data))
        {
            Close();
        }
        else
        {
            Refresh();
        }
    }

    private void OnRallyClicked()
    {
        Barracks barracks = currentSpot != null ? currentSpot.GetBuildingComponent<Barracks>() : null;

        Close();

        if (barracks != null && rallyPlacer != null)
        {
            rallyPlacer.Begin(barracks);
        }
    }

    private void OnSellClicked()
    {
        if (currentSpot != null)
        {
            currentSpot.Sell();
        }

        Close();
    }

    private void HandleResourceChanged(ResourceType type, int newAmount)
    {
        // Keeps the greyed-out buttons accurate if gold arrives while the menu is open
        if (currentSpot != null) Refresh();
    }

    private void SetVisible(bool visible)
    {
        panel.gameObject.SetActive(visible);
        closeBlocker.gameObject.SetActive(visible);
    }
}