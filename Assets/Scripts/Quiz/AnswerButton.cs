using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AnswerButton : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Button button;
    [SerializeField] private Image background;
    [SerializeField] private TMP_Text label;

    private System.Action<int> _onClick;
    private int _index;

    // Farben (optional im Inspector überschreiben)
    [Header("Colors")]
    public Color neutral = Color.white;
    public Color correct = new Color(0.78f, 0.93f, 0.80f); // grünlich
    public Color wrong   = new Color(0.98f, 0.79f, 0.79f); // rötlich

    public void Init(string text, int index, System.Action<int> onClick)
    {
        _index   = index;
        _onClick = onClick;

        if (label) label.text = text;

        SetInteractable(true);
        SetStateNeutral();

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => _onClick?.Invoke(_index));
    }

    public void SetInteractable(bool interactable)
    {
        if (button) button.interactable = interactable;
    }

    public void SetStateNeutral()
    {
        if (background) background.color = neutral;
    }

    public void SetStateCorrect()
    {
        if (background) background.color = correct;
    }

    public void SetStateWrong()
    {
        if (background) background.color = wrong;
    }
}
