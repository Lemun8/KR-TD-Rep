using UnityEngine;
using UnityEngine.EventSystems;

// A full-screen invisible overlay. While it is visible, the player is choosing a rally point.
public class RallyPointPlacer : MonoBehaviour, IPointerClickHandler
{
    private Barracks current;
    private Camera mainCamera;

    public void Begin(Barracks barracks)
    {
        current = barracks;
        if (mainCamera == null) mainCamera = Camera.main;

        current.ShowRallyIndicator(true);
        gameObject.SetActive(true);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (current == null)
        {
            gameObject.SetActive(false);
            return;
        }

        // Any button other than left click cancels
        if (eventData.button != PointerEventData.InputButton.Left)
        {
            End();
            return;
        }

        Vector3 screen = new Vector3(eventData.position.x, eventData.position.y, Mathf.Abs(mainCamera.transform.position.z));
        Vector3 world = mainCamera.ScreenToWorldPoint(screen);
        world.z = 0f;

        if (current.TrySetRallyPoint(world))
        {
            End();
        }
        else
        {
            // Not on the track or out of range: stay in placement mode so the player can try again
            current.FlashRallyIndicatorInvalid();
        }
    }

    private void End()
    {
        if (current != null) current.ShowRallyIndicator(false);

        current = null;
        gameObject.SetActive(false);
    }
}