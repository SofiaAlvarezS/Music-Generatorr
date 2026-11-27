using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Toggle))]
public class ProceduralToggleVisual : MonoBehaviour
{
    [SerializeField] private Text label;
    [SerializeField] private Image background;
    [SerializeField] private string instrumentName;
    private Toggle toggle;
    public void Configure()
    {
        toggle = GetComponent<Toggle>();
        if (label == null) label = GetComponentInChildren<Text>(true);
        if (background == null && transform.parent != null)
            background = transform.parent.GetComponent<Image>();
        if (string.IsNullOrEmpty(instrumentName) && label != null)
            instrumentName = label.text.Split('\n')[0];
    }
    void Awake() { Configure(); }
    void LateUpdate()
    {
        if (toggle == null || label == null) return;
        bool allowed = toggle.IsInteractable();
        bool selected = toggle.isOn;
        label.text = instrumentName + "\n" + (!allowed ? "Bloqueado" : selected ? "[X] Seleccionado" : "[ ] Disponible");
        label.color = !allowed ? new Color32(160,160,170,255) : Color.white;
        label.resizeTextForBestFit = true;
        label.resizeTextMinSize = 10;
        label.resizeTextMaxSize = 18;
        label.alignment = TextAnchor.MiddleCenter;
        if (background != null) background.color = !allowed ? new Color32(32,32,39,255) : selected ? new Color32(125,35,80,255) : new Color32(24,24,33,255);
    }
}
