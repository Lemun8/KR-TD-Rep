using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class BuildSpot : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private GameObject emptyVisual;   // the "empty pad" graphic, hidden when built on

    [Tooltip("Leave empty to allow every building. Fill it to restrict this spot.")]
    [SerializeField] private List<BuildingData> allowedBuildings = new List<BuildingData>();

    public bool IsOccupied => currentBuilding != null;
    public BuildingData CurrentData { get; private set; }

    private GameObject currentBuilding;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (BuildMenuUI.Instance != null)
        {
            BuildMenuUI.Instance.Open(this);
        }
    }

    public bool Allows(BuildingData data)
    {
        return allowedBuildings.Count == 0 || allowedBuildings.Contains(data);
    }

    // Lets the menu ask the built building for a component (for example a Barracks)
    public T GetBuildingComponent<T>() where T : Component
    {
        return currentBuilding != null ? currentBuilding.GetComponent<T>() : null;
    }

    public bool TryBuild(BuildingData data)
    {
        if (IsOccupied || data == null || data.prefab == null) return false;
        if (!Allows(data)) return false;
        if (ResourceManager.Instance == null || !ResourceManager.Instance.TrySpend(data.Cost)) return false;

        currentBuilding = Instantiate(data.prefab, transform.position, Quaternion.identity, transform);
        CurrentData = data;

        if (emptyVisual != null) emptyVisual.SetActive(false);
        return true;
    }

    public void Sell()
    {
        if (!IsOccupied) return;

        ResourceManager.Instance.Refund(CurrentData.Cost, CurrentData.sellRefundPercent);

        Destroy(currentBuilding);
        currentBuilding = null;
        CurrentData = null;

        if (emptyVisual != null) emptyVisual.SetActive(true);
    }
}