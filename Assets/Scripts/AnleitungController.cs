using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Controls the Anleitung (rules) panel navigation.
/// Handles prev/next page, page indicator text, dot indicators, and page image display.
/// </summary>
public class AnleitungController : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("The Image component showing the current page")]
    public Image pageImage;
    [Tooltip("The page text (e.g. 'Seite 1 / 12')")]
    public TextMeshProUGUI pageText;
    [Tooltip("The Previous button")]
    public Button prevButton;
    [Tooltip("The Next button")]
    public Button nextButton;
    [Tooltip("The Close button")]
    public Button closeButton;
    [Tooltip("The DotsContainer holding the dot Image children")]
    public Transform dotsContainer;

    [Header("Pages")]
    [Tooltip("All page sprites in order (anleitung_seite_01 to 12)")]
    public Sprite[] pages;

    [Header("Dot Colors")]
    public Color activeDotColor = new Color(0.161f, 0.671f, 0.886f); // #29ABE2
    public Color inactiveDotColor = new Color(0.5f, 0.5f, 0.5f, 1f);  // Gray

    private int currentPage = 0;

    private void Start()
    {
        if (prevButton != null)
            prevButton.onClick.AddListener(PrevPage);
        if (nextButton != null)
            nextButton.onClick.AddListener(NextPage);
        if (closeButton != null)
            closeButton.onClick.AddListener(Close);

        // Show first page
        ShowPage(0);
    }

    /// <summary>
    /// Called when the panel is opened — resets to page 1.
    /// </summary>
    private void OnEnable()
    {
        ShowPage(0);
    }

    public void NextPage()
    {
        if (pages == null || pages.Length == 0) return;
        if (currentPage < pages.Length - 1)
        {
            ShowPage(currentPage + 1);
        }
    }

    public void PrevPage()
    {
        if (currentPage > 0)
        {
            ShowPage(currentPage - 1);
        }
    }

    public void ShowPage(int index)
    {
        if (pages == null || pages.Length == 0) return;

        currentPage = Mathf.Clamp(index, 0, pages.Length - 1);

        // Update image
        if (pageImage != null && pages[currentPage] != null)
        {
            pageImage.sprite = pages[currentPage];
        }

        // Update page text
        if (pageText != null)
        {
            pageText.text = $"Seite {currentPage + 1} / {pages.Length}";
        }

        // Update button states
        if (prevButton != null)
            prevButton.interactable = currentPage > 0;
        if (nextButton != null)
            nextButton.interactable = currentPage < pages.Length - 1;

        // Update dot indicators
        UpdateDots();
    }

    private void UpdateDots()
    {
        if (dotsContainer == null) return;

        for (int i = 0; i < dotsContainer.childCount; i++)
        {
            Image dot = dotsContainer.GetChild(i).GetComponent<Image>();
            if (dot != null)
            {
                dot.color = (i == currentPage) ? activeDotColor : inactiveDotColor;
            }
        }
    }

    private void Close()
    {
        // Let MainMenuController handle this, or just hide the panel
        gameObject.SetActive(false);

        // Show main menu again
        MainMenuController menu = FindFirstObjectByType<MainMenuController>();
        if (menu != null)
        {
            menu.ShowMainMenu();
        }
    }
}
