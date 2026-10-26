using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

[Serializable]
public class ResourceDisplay
{
    public ResourceType type;
    public TMP_Text valueText;
}

public class ResourceBarUI : MonoBehaviour
{
    [SerializeField] private List<ResourceDisplay> displays = new List<ResourceDisplay>();

    private void Start()
    {
        if (ResourceManager.Instance == null)
        {
            Debug.LogWarning("ResourceBarUI: no ResourceManager in the scene.", this);
            return;
        }

        ResourceManager.Instance.ResourceChanged += HandleChanged;

        foreach (ResourceDisplay display in displays)
        {
            display.valueText.text = ResourceManager.Instance.Get(display.type).ToString();
        }
    }

    private void OnDestroy()
    {
        if (ResourceManager.Instance != null)
        {
            ResourceManager.Instance.ResourceChanged -= HandleChanged;
        }
    }

    private void HandleChanged(ResourceType type, int newAmount)
    {
        foreach (ResourceDisplay display in displays)
        {
            if (display.type == type) display.valueText.text = newAmount.ToString();
        }
    }
}