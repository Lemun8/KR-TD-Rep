using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class VillagePanelUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private VillageUpgrades upgrades;
    [SerializeField] private RectTransform panel;
    [SerializeField] private Button toggleButton;     // the always-visible "Village" button
    [SerializeField] private Button closeButton;
    [SerializeField] private Button rowButtonPrefab;
    [SerializeField] private TMP_Text incomeLabel;    // optional summary line

    private readonly List<Button> rows = new List<Button>();

    private void Awake()
    {
        for (int i = 0; i < upgrades.Count; i++)
        {
            Button row = Instantiate(rowButtonPrefab, panel);

            int index = i;
            row.onClick.AddListener(() => upgrades.TryUpgrade(index));

            rows.Add(row);
        }

        closeButton.transform.SetAsLastSibling();

        toggleButton.onClick.AddListener(Toggle);
        closeButton.onClick.AddListener(Close);

        panel.gameObject.SetActive(false);
    }

    private void Start()
    {
        upgrades.Upgraded += HandleUpgraded;

        if (ResourceManager.Instance != null)
        {
            ResourceManager.Instance.ResourceChanged += HandleResourceChanged;
        }
    }

    private void OnDestroy()
    {
        if (upgrades != null) upgrades.Upgraded -= HandleUpgraded;

        if (ResourceManager.Instance != null)
        {
            ResourceManager.Instance.ResourceChanged -= HandleResourceChanged;
        }
    }

    private void Toggle()
    {
        if (panel.gameObject.activeSelf) Close();
        else Open();
    }

    private void Open()
    {
        panel.gameObject.SetActive(true);
        Refresh();
    }

    private void Close()
    {
        panel.gameObject.SetActive(false);
    }

    private void HandleUpgraded(int index)
    {
        Refresh();
    }

    private void HandleResourceChanged(ResourceType type, int newAmount)
    {
        // Keeps the greyed-out buttons accurate while the panel is open
        Refresh();
    }

    private void Refresh()
    {
        if (!panel.gameObject.activeSelf || ResourceManager.Instance == null) return;

        for (int i = 0; i < rows.Count; i++)
        {
            VillageUpgradeData data = upgrades.GetData(i);
            int level = upgrades.GetLevel(i);
            int income = ResourceManager.Instance.GetProduction(data.resource);
            TMP_Text label = rows[i].GetComponentInChildren<TMP_Text>();

            if (upgrades.IsMaxed(i))
            {
                label.text = $"{data.displayName}  MAX\n{data.resource} +{income} per wave";
                rows[i].interactable = false;
            }
            else
            {
                UpgradeLevel next = data.GetLevel(level);
                label.text = $"{data.displayName}  Lv {level}/{data.MaxLevel}   {data.resource} +{income} -> +{income + next.productionIncrease}\n{ResourceFormat.CostText(next.cost)}";
                rows[i].interactable = upgrades.CanUpgrade(i);
            }
        }

        if (incomeLabel != null) incomeLabel.text = BuildIncomeText();

        LayoutRebuilder.ForceRebuildLayoutImmediate(panel);
    }

    private string BuildIncomeText()
    {
        StringBuilder text = new StringBuilder("Income per wave:  ");

        foreach (ResourceType type in System.Enum.GetValues(typeof(ResourceType)))
        {
            int amount = ResourceManager.Instance.GetProduction(type);
            if (amount > 0) text.Append($"{type} +{amount}   ");
        }

        return text.ToString();
    }
}