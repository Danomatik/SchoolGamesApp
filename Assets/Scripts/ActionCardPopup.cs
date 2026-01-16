using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class ActionCardPopup : MonoBehaviour, IPointerClickHandler
{
    [Header("UI")]
    [SerializeField] private GameObject root;          // Fullscreen Panel (Image)
    [SerializeField] private TextMeshProUGUI questionText;  // Zeigt "AktionFrage" oder "Aktionskarte"
    [SerializeField] private TextMeshProUGUI idText;        // Zeigt "Frage 23" oder "Karte 5"
    [SerializeField] private TextMeshProUGUI bodyText;      // Zeigt den eigentlichen Text
    [SerializeField] private CanvasGroup cg;           // optional (falls vorhanden)

    private Action onDismiss;

    void Awake()
    {
        if (!root) root = gameObject;
        if (!cg) cg = root.GetComponent<CanvasGroup>();
    }

    public void Show(int id, string text, Action onDismiss)
    {
        this.onDismiss = onDismiss;
        
        // ✅ Modernes Formatting mit Farbschema
        // questionText zeigt "AktionFrage" oder "Aktionskarte"
        if (questionText)
        {
            questionText.text = $"<b><color=#3EBCD5>Aktion Frage</color></b>";
            questionText.enableWordWrapping = true;
            questionText.overflowMode = TextOverflowModes.Page;
            questionText.alignment = TextAlignmentOptions.Center;
        }
        
        // idText zeigt nur die ID (z.B. "Frage 23")
        if (idText)
        {
            idText.text = $"<size=90%><color=#C6E6F0>Frage {id}</color></size>";
            idText.enableWordWrapping = true;
            idText.overflowMode = TextOverflowModes.Page;
            idText.alignment = TextAlignmentOptions.Center;
        }
        
        // bodyText zeigt den eigentlichen Text
        if (bodyText)
        {
            bodyText.text = $"<color=#FFFFFF>{text}</color>";
            bodyText.enableWordWrapping = true;
            bodyText.overflowMode = TextOverflowModes.Page;
            bodyText.alignment = TextAlignmentOptions.Center;
        }

        if (!root.activeSelf) root.SetActive(true);

        if (cg)
        {
            cg.alpha = 1f;
            cg.interactable = true;
            cg.blocksRaycasts = true;
        }

        // Sicherheit: ganz nach vorne
        var canvas = root.GetComponentInParent<Canvas>();
        if (canvas != null) canvas.sortingOrder = Mathf.Max(canvas.sortingOrder, 1000);
    }

    public void Hide()
    {
        if (cg)
        {
            cg.alpha = 0f;
            cg.interactable = false;
            cg.blocksRaycasts = false;
        }

        if (root.activeSelf) root.SetActive(false);
        onDismiss = null;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        var cb = onDismiss;
        Hide();
        cb?.Invoke();
    }
}