using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [Header("Company Panel")]
    [SerializeField] private GameObject companyPanel;

    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI bodyText;

    // Button 1..4 in dieser Reihenfolge im Inspector zuweisen
    [SerializeField] private Button primaryButton;     // Button 1 = Kaufen/Gründen ODER Investieren (bei Upgrades)
    [SerializeField] private Button secondaryButton;   // Button 2 = Verzichten ODER AG (bei Upgrades)
    [SerializeField] private Button tertiaryButton;    // Button 3 (ungenuzt im Kauf-Popup)
    [SerializeField] private Button cancelButton;      // Button 4 (ungenuzt im Kauf-/Upgrade-Popup)

    [Header("Money Display")]
    [SerializeField] private TextMeshProUGUI moneyDisplayText; // Display für Geld

    private GameManager gm;

    private void Awake()
    {
        gm = GetComponent<GameManager>(); // alle Manager am selben GO
        if (companyPanel != null) companyPanel.SetActive(false);
    }

    private void Start()
    {
        // Initial money display update
        UpdateMoneyDisplay();
    }

    private void LateUpdate()
    {
        // Update money display at end of frame
        // This ensures the current player is always correct after turn changes
        //UpdateMoneyDisplay();
    }

    public void UpdateMoneyDisplay()
    {
        if (moneyDisplayText == null || gm == null) return;

        var currentPlayer = gm.GetCurrentPlayer();
        if (currentPlayer != null)
        {
            moneyDisplayText.text = $"Spieler {currentPlayer.PlayerID}: {currentPlayer.Money}€";
        }
        else
        {
            moneyDisplayText.text = "--- €";
        }
    }

    // Freies Feld → Kaufen oder Verzichten (Buttons 1/2)
    public void ShowCompanyPurchase(CompanyConfigData company, CompanyField field, PlayerData player)
    {
        if (!companyPanel) { Debug.LogError("CompanyPanel fehlt!"); return; }

        companyPanel.SetActive(true);
        titleText.text = $"{company.companyName} — Gründung";
        bodyText.text =
            $"Kosten: {company.costFound}€\n" +
            $"Ertrag pro Runde: {company.revenueFound}€\n\n" +
            "Möchtest du gründen? (Quiz erforderlich)";

        // Button 1 = Kaufen/Gründen
        Wire(primaryButton, "Gründen", () =>
        {
            Close();
            gm.StartQuizForCompany(company, field, player, CompanyLevel.Founded);
        });

        // Button 2 = Verzichten → Zug endet sofort
        Wire(secondaryButton, "Verzichten", () =>
        {
            Close();
            gm.EndTurn();
        });

        // Rest ausblenden
        if (tertiaryButton) tertiaryButton.gameObject.SetActive(false);
        if (cancelButton)   cancelButton.gameObject.SetActive(false);
    }

    public void ShowUpgradeOptions(CompanyConfigData company, CompanyField field, PlayerData player)
    {
        if (!companyPanel) { Debug.LogError("CompanyPanel fehlt!"); return; }

        companyPanel.SetActive(true);
        titleText.text = $"{company.companyName} — Upgrade";
        bodyText.text =
            $"Aktueller Status: {field.level}\n\n" +
            $"• Investieren: {company.costInvest}€ → Ertrag {company.revenueInvest}€\n" +
            $"• AG gründen: {company.costAG}€ → Ertrag {company.revenueAG}€\n\n" +
            "Wähle ein Upgrade (Quiz erforderlich):";

        var gm = GetComponent<GameManager>(); // alle Manager am selben GO

        // Alles ausblenden, dann gezielt einblenden
        if (tertiaryButton) tertiaryButton.gameObject.SetActive(false);
        if (cancelButton)   cancelButton.gameObject.SetActive(false);

        // Reset Button-Listener
        primaryButton.onClick.RemoveAllListeners();
        secondaryButton.onClick.RemoveAllListeners();

        switch (field.level)
        {
            case CompanyLevel.Founded:
                // Button 1 = Investieren
                Wire(primaryButton, "Investieren", () =>
                {
                    Close();
                    gm.StartQuizForCompany(company, field, player, CompanyLevel.Invested);
                });
                // Button 2 = Später
                Wire(secondaryButton, "Später", () =>
                {
                    Close();
                    gm.EndTurn();
                });
                break;

            case CompanyLevel.Invested:
                // Button 1 = AG gründen
                Wire(primaryButton, "AG gründen", () =>
                {
                    Close();
                    gm.StartQuizForCompany(company, field, player, CompanyLevel.AG);
                });
                // Button 2 = Später
                Wire(secondaryButton, "Später", () =>
                {
                    Close();
                    gm.EndTurn();
                });
                break;

            case CompanyLevel.AG:
            default:
                // Nichts mehr möglich
                Close();
                gm.EndTurn();
                break;
        }
    }


    private void Wire(Button btn, string label, System.Action onClick)
    {
        if (!btn) return;
        btn.gameObject.SetActive(true);
        var txt = btn.GetComponentInChildren<TextMeshProUGUI>();
        if (txt) txt.text = label;
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(() => onClick?.Invoke());
    }

    private void Close()
    {
        if (companyPanel) companyPanel.SetActive(false);
    }
}
