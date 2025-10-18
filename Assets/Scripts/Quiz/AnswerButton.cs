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
public Color neutral = new Color32(0x2A, 0x7C, 0xA6, 0xFF); // Hex: #2A7CA6 (kräftiges Blau)
public Color correct = new Color(0.35f, 0.85f, 0.45f);     // stärkeres Grün
public Color wrong   = new Color(0.9f, 0.25f, 0.25f);      // stärkeres Rot


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
