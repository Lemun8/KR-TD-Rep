using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Put this on a Slider. Pick which volume it controls (Master, Bgm or Sfx).
[RequireComponent(typeof(Slider))]
public class VolumeSliderUI : MonoBehaviour, IPointerUpHandler
{
    [SerializeField] private VolumeChannel channel;
    [SerializeField] private Slider slider;
    [Tooltip("Optional: shows the value as a percentage.")]
    [SerializeField] private TMP_Text valueLabel;

    private void Awake()
    {
        if (slider == null) slider = GetComponent<Slider>();

        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.wholeNumbers = false;
    }

    // Runs every time the options panel opens, so the slider always shows the saved value
    private void OnEnable()
    {
        slider.SetValueWithoutNotify(VolumeSettings.Get(channel));
        Refresh(slider.value);
        slider.onValueChanged.AddListener(OnSliderChanged);
    }

    private void OnDisable()
    {
        slider.onValueChanged.RemoveListener(OnSliderChanged);
        VolumeSettings.Save();
    }

    private void OnSliderChanged(float value)
    {
        VolumeSettings.Set(channel, value);
        Refresh(value);
    }

    // When you let go of the slider, play a click so you can hear the new level
    // (BGM is already audible while you drag, so it doesn't need one)
    public void OnPointerUp(PointerEventData eventData)
    {
        if (channel != VolumeChannel.Bgm) AudioManager.Play(SfxEvent.ButtonClick);
    }

    private void Refresh(float value)
    {
        if (valueLabel != null) valueLabel.text = Mathf.RoundToInt(value * 100f) + "%";
    }
}