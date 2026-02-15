using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Steuert das SchoolGames Regeln-Panel (Accordion-Logik + Öffnen/Schließen).
/// Auf das InfoPanel-GameObject legen und die Felder im Inspector werden
/// automatisch gefunden wenn du auf 'Auto-Setup' im Kontextmenü klickst.
/// </summary>
public class RulesPanelController : MonoBehaviour
{
    [Header("Panel Referenzen")]
    public GameObject infoPanel;
    public ScrollRect scrollRect;
    public Button closeButton;
    public Button darkBackground;

    [Header("Accordion")]
    public GameObject[] contentPanels;
    public TextMeshProUGUI[] arrowTexts;
    public Image[] itemBackgrounds;
    public Image[] iconBackgrounds;

    private int currentOpen = -1;

    // ─── Farben ──────────────────────────────────────
    private static readonly Color COLOR_PRIMARY = new Color32(0, 172, 172, 255);
    private static readonly Color COLOR_ITEM_BG = new Color32(240, 247, 247, 255);
    private static readonly Color COLOR_OPEN_BG = new Color32(240, 250, 250, 255);
    private static readonly Color COLOR_WHITE = Color.white;
    private static readonly Color COLOR_ARROW = new Color32(178, 190, 195, 255);
    private static readonly Color COLOR_DARK_TEAL = new Color32(0, 128, 127, 255);

    void Start()
    {
        // Auto-Setup wenn Felder leer
        if (infoPanel == null) AutoSetup();

        // Events verbinden
        if (closeButton != null) closeButton.onClick.AddListener(ClosePanel);
        if (darkBackground != null) darkBackground.onClick.AddListener(ClosePanel);

        // Accordion-Buttons verbinden
        for (int i = 0; i < contentPanels.Length; i++)
        {
            int index = i; // Closure
            Transform headerBtn = contentPanels[i].transform.parent.Find("HeaderButton");
            if (headerBtn != null)
            {
                Button btn = headerBtn.GetComponent<Button>();
                if (btn != null) btn.onClick.AddListener(() => ToggleAccordion(index));
            }
        }
    }

    public void OpenPanel()
    {
        if (infoPanel != null)
        {
            infoPanel.SetActive(true);
            if (scrollRect != null) scrollRect.verticalNormalizedPosition = 1f;
        }
    }

    public void ClosePanel()
    {
        if (infoPanel != null) infoPanel.SetActive(false);
        CloseAllAccordions();
    }

    public void ToggleAccordion(int index)
    {
        if (currentOpen == index)
        {
            // Schließen
            CloseAccordionAt(index);
            currentOpen = -1;
        }
        else
        {
            // Vorheriges schließen
            if (currentOpen >= 0) CloseAccordionAt(currentOpen);
            // Neues öffnen
            OpenAccordionAt(index);
            currentOpen = index;
        }
    }

    void OpenAccordionAt(int index)
    {
        if (index < 0 || index >= contentPanels.Length) return;
        contentPanels[index].SetActive(true);
        if (arrowTexts != null && index < arrowTexts.Length)
            arrowTexts[index].text = "▴";
        if (itemBackgrounds != null && index < itemBackgrounds.Length)
            itemBackgrounds[index].color = COLOR_OPEN_BG;
        if (iconBackgrounds != null && index < iconBackgrounds.Length)
            iconBackgrounds[index].color = COLOR_PRIMARY;
    }

    void CloseAccordionAt(int index)
    {
        if (index < 0 || index >= contentPanels.Length) return;
        contentPanels[index].SetActive(false);
        if (arrowTexts != null && index < arrowTexts.Length)
            arrowTexts[index].text = "▾";
        if (itemBackgrounds != null && index < itemBackgrounds.Length)
            itemBackgrounds[index].color = COLOR_WHITE;
        if (iconBackgrounds != null && index < iconBackgrounds.Length)
            iconBackgrounds[index].color = COLOR_ITEM_BG;
    }

    void CloseAllAccordions()
    {
        for (int i = 0; i < contentPanels.Length; i++)
            CloseAccordionAt(i);
        currentOpen = -1;
    }

    /// <summary>
    /// Findet alle Referenzen automatisch anhand der Hierarchy-Struktur.
    /// </summary>
    [ContextMenu("Auto-Setup")]
    public void AutoSetup()
    {
        infoPanel = gameObject;
        darkBackground = transform.Find("DarkBackground")?.GetComponent<Button>();
        closeButton = transform.Find("PanelContainer/Header/CloseButton")?.GetComponent<Button>();
        scrollRect = GetComponentInChildren<ScrollRect>();

        // Accordion Items finden
        Transform content = transform.Find("PanelContainer/ScrollArea/Viewport/Content");
        if (content == null) return;

        var panels = new System.Collections.Generic.List<GameObject>();
        var arrows = new System.Collections.Generic.List<TextMeshProUGUI>();
        var backgrounds = new System.Collections.Generic.List<Image>();
        var icons = new System.Collections.Generic.List<Image>();

        for (int i = 0; i < 20; i++)
        {
            Transform item = content.Find($"AccordionItem_{i}");
            if (item == null) break;

            Transform cp = item.Find("ContentPanel");
            if (cp != null) panels.Add(cp.gameObject);

            Transform arrow = item.Find("HeaderButton/ArrowText");
            if (arrow != null) arrows.Add(arrow.GetComponent<TextMeshProUGUI>());

            backgrounds.Add(item.GetComponent<Image>());

            Transform iconBG = item.Find("HeaderButton/IconBG");
            if (iconBG != null) icons.Add(iconBG.GetComponent<Image>());
        }

        contentPanels = panels.ToArray();
        arrowTexts = arrows.ToArray();
        itemBackgrounds = backgrounds.ToArray();
        iconBackgrounds = icons.ToArray();

        Debug.Log($"Auto-Setup abgeschlossen: {contentPanels.Length} Accordion-Items gefunden.");
    }
}
