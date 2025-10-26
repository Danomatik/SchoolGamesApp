using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class BankCardPopup : MonoBehaviour, IPointerClickHandler
{
    [Header("UI")]
    [SerializeField] private GameObject root;          // Fullscreen Panel (Image)
    [SerializeField] private TextMeshProUGUI idText;
    [SerializeField] private TextMeshProUGUI bodyText;
    [SerializeField] private CanvasGroup cg;           // optional (falls vorhanden)

    private Action onDismiss;

    void Awake()
    {
        if (!root) root = gameObject;
        if (!cg) cg = root.GetComponent<CanvasGroup>();
        // NICHT zwangsweise deaktivieren; wir respektieren Editor-Status
    }

    public void Show(int id, string text, Action onDismiss)
    {
        this.onDismiss = onDismiss;

        if (idText)   idText.text   = $"#{id}";
        if (bodyText) bodyText.text = text;

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
