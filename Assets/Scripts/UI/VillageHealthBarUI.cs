using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class VillageHealthBarUI : MonoBehaviour
{
    [SerializeField] private RectTransform fill;
    [SerializeField] private Image fillImage;
    [SerializeField] private TMP_Text label;
    [SerializeField] private Color healthyColor = new Color(0.35f, 0.8f, 0.4f);
    [SerializeField] private Color lowColor = new Color(0.9f, 0.25f, 0.2f);

    private Health health;

    private void Start()
    {
        if (VillageHealth.Instance == null)
        {
            Debug.LogWarning("VillageHealthBarUI: no VillageHealth in the scene.", this);
            return;
        }

        health = VillageHealth.Instance.Health;
        health.Changed += HandleChanged;
        HandleChanged(health.Current, health.Max);
    }

    private void OnDestroy()
    {
        if (health != null) health.Changed -= HandleChanged;
    }

    private void HandleChanged(float current, float max)
    {
        float ratio = max > 0f ? current / max : 0f;

        fill.anchorMax = new Vector2(ratio, 1f);
        fillImage.color = Color.Lerp(lowColor, healthyColor, ratio);
        label.text = $"{Mathf.CeilToInt(current)} / {Mathf.CeilToInt(max)}";
    }
}