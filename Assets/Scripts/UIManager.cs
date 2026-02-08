using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using System.Collections.Generic;

public class UIManager : MonoBehaviour
{
    [Header("Main Overlay")]
    [SerializeField] private GameObject popupOverlay; // Der Hintergrund-Container für alle Popups
    [SerializeField] private Button sharedConfirmButton; // Button zum Bestätigen (für Action/Bank/Quiz)
    
    [Header("Content Views")]
    [SerializeField] private GameObject companyContent;      // Ersetzt companyPanel
    [SerializeField] private GameObject actionContent;       // NEU: Für Aktionskarten
    [SerializeField] private GameObject bankContent;         // NEU: Für Bankkarten
    [SerializeField] private GameObject quizContent;         // (Platzhalter für später)
    [SerializeField] private GameObject bankruptcyContent;   // Ersetzt bankruptcyPanel
    [SerializeField] private GameObject gameOverContent;     // Ersetzt gameOverPanel
    [SerializeField] private GameObject initiativeContent;   // Ersetzt initiativePanel

    [Header("Action Card Content")]
    [SerializeField] private TextMeshProUGUI actionTitleText;
    [SerializeField] private TextMeshProUGUI actionIdText;
    [SerializeField] private TextMeshProUGUI actionBodyText;
    [SerializeField] private Button actionBackgroundButton; // NEU: Unsichtbarer Button für "Click to Continue" (nur Action)

    [Header("Bank Card Content")]
    [SerializeField] private TextMeshProUGUI bankTitleText;
    [SerializeField] private TextMeshProUGUI bankIdText;
    [SerializeField] private TextMeshProUGUI bankBodyText;

    [Header("Company Content")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI bodyText;
    [SerializeField] private Button primaryButton;     // Button 1
    [SerializeField] private Button secondaryButton;   // Button 2
    [SerializeField] private Button tertiaryButton;    // Button 3
    [SerializeField] private Button cancelButton;      // Button 4

    [Header("Money Display")]
    [SerializeField] private TextMeshProUGUI playerNameText; // Display für Spielernamen
    [SerializeField] private TextMeshProUGUI playerNameText2; // Zweites Display für Spielernamen (optional)
    [SerializeField] private TextMeshProUGUI playerIDText; // Display für Spieler ID (P1, P2...)
    [SerializeField] private TextMeshProUGUI moneyDisplayText; // Display für Geld

    [Header("Timer Display")]
    [SerializeField] private TextMeshProUGUI timerDisplayText; // Display für Timer (optional)

    [Header("Initiative Content")]
    // [SerializeField] private GameObject initiativeContent; // Removed duplicate
    [SerializeField] private GameObject initiativeCurrentPlayerCard;
    [SerializeField] private TextMeshProUGUI initiativePlayerNameText;
    [SerializeField] private TextMeshProUGUI initiativeRollResultText;
    [SerializeField] private Transform initiativeResultsContainer;
    [SerializeField] private GameObject initiativeResultRowPrefab; // Prefab für Liste
    [SerializeField] private Button initiativeStartButton;

    [Header("Bankruptcy Content")]
    [SerializeField] private TextMeshProUGUI bankruptcyTitleText;
    [SerializeField] private TextMeshProUGUI bankruptcyBodyText;
    [SerializeField] private Transform auctionButtonContainer; // Container für Versteigerungs-Buttons
    [SerializeField] private GameObject auctionButtonPrefab; // Prefab für Versteigerungs-Button
    [SerializeField] private Button bankruptcyCancelButton; // Button zum Abbrechen

    [Header("Game Over Content")]
    [SerializeField] private TextMeshProUGUI gameOverTitleText;
    [SerializeField] private TextMeshProUGUI gameOverBodyText;
    [SerializeField] private Transform rankingContainer; // Container für Ranking-Einträge
    [SerializeField] private Button gameOverMenuButton; // Button zum Zurück zum Menü
    [SerializeField] private Button gameOverNewGameButton; // Button für Neues Spiel
    [SerializeField] private string gameSceneName = "Demo"; // Name der Spielszene für Neues Spiel
    [SerializeField] private List<GameObject> gameOverObjects = new List<GameObject>();

    private GameManager gm;
    private GameObject currentContent; // Aktuell angezeigter Content
    private System.Action onConfirmAction; // Callback für den Confirm Button

    private void Awake()
    {
        gm = GetComponent<GameManager>();
        
        // Ensure everything is hidden at start
        if (popupOverlay != null) popupOverlay.SetActive(false);
        HideAllContent();

        // Setup shared confirm button
        if (sharedConfirmButton != null)
        {
            sharedConfirmButton.onClick.AddListener(() => 
            {
                onConfirmAction?.Invoke();
                ClosePopup();
            });
        }
    }

    private void Start()
    {
        // Initial money display update
        UpdateMoneyDisplay();
        UpdatePlayerNameDisplay();
        UpdatePlayerIDDisplay();
        
        // Initialisiere Timer-Anzeige (falls Timer bereits läuft)
        if (gm != null && gm.gameTimerManager != null)
        {
            float timeRemaining = gm.gameTimerManager.GetTimeRemaining();
            if (timeRemaining > 0)
            {
                UpdateTimerDisplay(timeRemaining);
            }
        }
    }

    private void LateUpdate()
    {
        // Update money display at end of frame
    }

    // ============================================================
    // 📺 CORE POPUP SYSTEM
    // ============================================================

    public void ShowContent(GameObject content)
    {
        if (popupOverlay == null)
        {
            Debug.LogError("UIManager: PopupOverlay is not assigned!");
            return;
        }

        HideAllContent();
        
        popupOverlay.SetActive(true);
        currentContent = content;
        
        if (currentContent != null)
        {
            currentContent.SetActive(true);
        }
    }

    public void ClosePopup()
    {
        if (popupOverlay != null) popupOverlay.SetActive(false);
        HideAllContent();
        onConfirmAction = null;
    }

    private void HideAllContent()
    {
        if (companyContent) companyContent.SetActive(false);
        if (actionContent) actionContent.SetActive(false);
        if (bankContent) bankContent.SetActive(false);
        if (quizContent) quizContent.SetActive(false);
        if (bankruptcyContent) bankruptcyContent.SetActive(false);
        if (gameOverContent) gameOverContent.SetActive(false);
        if (initiativeContent) initiativeContent.SetActive(false);
        
        if (currentContent != null)
        {
            currentContent.SetActive(false);
            currentContent = null;
        }
    }

    // ============================================================
    // 🃏 ACTION & BANK CARDS
    // ============================================================

    public void ShowActionCard(int id, string text, System.Action onDismiss)
    {
        if (actionContent == null) return;

        if (actionTitleText) actionTitleText.text = "Aktionskarte";
        if (actionIdText) actionIdText.text = $"Karte {id}";
        if (actionBodyText) actionBodyText.text = text;

        onConfirmAction = onDismiss;

        // Wire up specific click-to-dismiss behavior for Action Cards
        if (actionBackgroundButton != null)
        {
            actionBackgroundButton.onClick.RemoveAllListeners();
            actionBackgroundButton.onClick.AddListener(() => 
            {
                onConfirmAction?.Invoke();
                ClosePopup();
            });
        }

        ShowContent(actionContent);
    }

    public void ShowBankCard(int id, string text, System.Action onDismiss)
    {
        if (bankContent == null) return;

        if (bankTitleText) bankTitleText.text = "Bankkarte";
        if (bankIdText) bankIdText.text = $"Karte {id}";
        if (bankBodyText) bankBodyText.text = text;

        onConfirmAction = onDismiss;
        ShowContent(bankContent);
    }

    public void ShowQuiz()
    {
        if (quizContent == null)
        {
            Debug.LogError("UIManager: quizContent is not assigned!");
            return;
        }
        ShowContent(quizContent);
    }
    
    // ============================================================
    // 💰 MONEY & PLAYER DISPLAY
    // ============================================================

    public void UpdateMoneyDisplay()
    {
        if (moneyDisplayText == null || gm == null) return;

        var currentPlayer = gm.GetCurrentPlayer();
        if (currentPlayer != null)
        {
            moneyDisplayText.text = $"{currentPlayer.Money:N0}€";
        }
        else
        {
            moneyDisplayText.text = "--- €";
        }
    }

    public void UpdatePlayerNameDisplay()
    {
        if (gm == null) return;

        string displayName = "";
        var currentPlayer = gm.GetCurrentPlayer();
        
        if (currentPlayer != null)
        {
            const int maxLen = 15;
            displayName = currentPlayer.PlayerName; // fallback handled inside PlayerName prop usually, or check null
            if (string.IsNullOrEmpty(displayName)) displayName = $"Spieler {currentPlayer.PlayerID}";

            if (displayName.Length > maxLen)
                displayName = displayName.Substring(0, maxLen) + "...";
        }

        if (playerNameText != null) playerNameText.text = displayName;
        if (playerNameText2 != null) playerNameText2.text = displayName;
    }

    public void UpdatePlayerIDDisplay()
    {
        if (playerIDText == null || gm == null) return;

        var currentPlayer = gm.GetCurrentPlayer();
        if (currentPlayer != null)
        {
            playerIDText.text = $"P{currentPlayer.PlayerID}";
        }
        else
        {
            playerIDText.text = "";
        }
    }
    
    public void UpdateTimerDisplay(float timeRemainingInSeconds)
    {
        if (timerDisplayText == null) return;

        int minutes = Mathf.Max(0, Mathf.FloorToInt(timeRemainingInSeconds / 60f));
        int seconds = Mathf.Max(0, Mathf.FloorToInt(timeRemainingInSeconds % 60f));

        timerDisplayText.text = $"{minutes:D2}:{seconds:D2}";
    }

    // ============================================================
    // 🎲 INITIATIVE
    // ============================================================

    public void SetupInitiative()
    {
        if (!initiativeContent) return;
        
        // Clear results list
        if (initiativeResultsContainer != null)
        {
            foreach (Transform child in initiativeResultsContainer)
            {
                Destroy(child.gameObject);
            }
        }

        // Hide Start Button initially
        if (initiativeStartButton != null)
        {
            initiativeStartButton.gameObject.SetActive(false);
        }

        // Hide Current Player Card initially
        if (initiativeCurrentPlayerCard != null)
        {
            initiativeCurrentPlayerCard.SetActive(false);
        }

        ShowContent(initiativeContent);
    }

    public void ShowInitiativeResult(int rank, string playerName, int rollResult)
    {
        if (!initiativeContent) return;

        // Show/Update Current Player Card
        if (initiativeCurrentPlayerCard != null)
        {
            initiativeCurrentPlayerCard.SetActive(true);
            
            if (initiativePlayerNameText != null)
                initiativePlayerNameText.text = playerName;
            
            if (initiativeRollResultText != null)
                initiativeRollResultText.text = rollResult.ToString();
        }

        // Add to Result List
        if (initiativeResultsContainer != null && initiativeResultRowPrefab != null)
        {
            GameObject row = Instantiate(initiativeResultRowPrefab, initiativeResultsContainer);
            
            // Find child components by name (as requested by user structure)
            var rankText = row.transform.Find("RankText")?.GetComponent<TextMeshProUGUI>();
            var nameText = row.transform.Find("NameText")?.GetComponent<TextMeshProUGUI>();
            var scoreText = row.transform.Find("ScoreText")?.GetComponent<TextMeshProUGUI>();

            if (rankText != null) rankText.text = $"{rank}.";
            if (nameText != null) nameText.text = playerName;
            if (scoreText != null) scoreText.text = rollResult.ToString();
        }
        
        ShowContent(initiativeContent);
    }

    public void ShowInitiativeStartButton(System.Action onStartGame)
    {
        if (!initiativeContent) return;

        ShowContent(initiativeContent);

        if (initiativeStartButton != null)
        {
            initiativeStartButton.gameObject.SetActive(true);
            initiativeStartButton.onClick.RemoveAllListeners();
            initiativeStartButton.onClick.AddListener(() =>
            {
                onStartGame?.Invoke();
            });
        }
    }

    public void HideInitiative()
    {
        ClosePopup();
    }

    // ============================================================
    // 🏢 COMPANY PANEL
    // ============================================================

    // Freies Feld → Kaufen oder Verzichten (Buttons 1/2)
    public void ShowCompanyPurchase(CompanyConfigData company, CompanyField field, PlayerData player)
    {
        if (!companyContent) { Debug.LogError("CompanyContent fehlt!"); return; }

        if (titleText) titleText.text = $"{company.companyName}\nGründung";
        if (bodyText) bodyText.text =
            $"Kosten: {company.costFound:N0}€\n" +
            $"Ertrag pro Runde: {company.revenueFound:N0}€\n\n" +
            $"Möchtest du gründen?\n(Quiz erforderlich)";

        // Button 1 = Kaufen/Gründen
        Wire(primaryButton, "Gründen", () =>
        {
            ClosePopup();
            gm.StartQuizForCompany(company, field, player, CompanyLevel.Founded);
        });

        // Button 2 = Verzichten → Zug endet sofort
        Wire(secondaryButton, "Verzichten", () =>
        {
            ClosePopup();
            gm.EndTurn();
        });

        // Rest ausblenden
        if (tertiaryButton) tertiaryButton.gameObject.SetActive(false);
        if (cancelButton)   cancelButton.gameObject.SetActive(false);

        ShowContent(companyContent);
    }

    public void ShowUpgradeOptions(CompanyConfigData company, CompanyField field, PlayerData player)
    {
        if (!companyContent) { Debug.LogError("CompanyContent fehlt!"); return; }

        if (titleText) titleText.text = $"{company.companyName}\nUpgrade";
        
        if (bodyText) bodyText.text =
            $"Aktueller Status: {field.level}\n\n" +
            $"Investieren: {company.costInvest:N0}€ → Ertrag {company.revenueInvest:N0}€\n" +
            $"AG gründen: {company.costAG:N0}€ → Ertrag {company.revenueAG:N0}€\n\n" +
            $"Wähle ein Upgrade:\n(Quiz erforderlich)";

        // Alles ausblenden, dann gezielt einblenden
        if (tertiaryButton) tertiaryButton.gameObject.SetActive(false);
        if (cancelButton)   cancelButton.gameObject.SetActive(false);

        // Reset Button-Listener
        if (primaryButton) primaryButton.onClick.RemoveAllListeners();
        if (secondaryButton) secondaryButton.onClick.RemoveAllListeners();

        bool showPanel = true;

        switch (field.level)
        {
            case CompanyLevel.Founded:
                // Button 1 = Investieren
                Wire(primaryButton, "Investieren", () =>
                {
                    ClosePopup();
                    gm.StartQuizForCompany(company, field, player, CompanyLevel.Invested);
                });
                // Button 2 = Später
                Wire(secondaryButton, "Später", () =>
                {
                    ClosePopup();
                    gm.EndTurn();
                });
                break;

            case CompanyLevel.Invested:
                // Button 1 = AG gründen
                Wire(primaryButton, "AG gründen", () =>
                {
                    ClosePopup();
                    gm.StartQuizForCompany(company, field, player, CompanyLevel.AG);
                });
                // Button 2 = Später
                Wire(secondaryButton, "Später", () =>
                {
                    ClosePopup();
                    gm.EndTurn();
                });
                break;

            case CompanyLevel.AG:
            default:
                // Nichts mehr möglich
                showPanel = false;
                ClosePopup();
                gm.EndTurn();
                break;
        }

        if (showPanel)
        {
            ShowContent(companyContent);
        }
    }

    private void Wire(Button btn, string label, System.Action onClick)
    {
        if (!btn) return;
        btn.gameObject.SetActive(true);
        
        var txt = btn.GetComponentInChildren<TextMeshProUGUI>();
        if (txt)
        {
            txt.text = label;
        }
        
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(() => onClick?.Invoke());
    }

    // ============================================================
    // 💰 INSOLVENZ & VERSTEIGERUNG UI
    // ============================================================

    public void ShowBankruptcyAuction(PlayerData player, int missingAmount, string reason)
    {
        if (!bankruptcyContent)
        {
            Debug.LogError("BankruptcyContent fehlt im UIManager!");
            return;
        }

        // Titel und Beschreibung
        if (bankruptcyTitleText)
        {
            bankruptcyTitleText.text = $"🚨 Zahlungsunfähigkeit";
        }

        if (bankruptcyBodyText)
        {
            string playerName = string.IsNullOrEmpty(player.PlayerName) ? $"Spieler {player.PlayerID}" : player.PlayerName;
            bankruptcyBodyText.text =
                $"{playerName} kann {missingAmount}€ nicht bezahlen.\n" +
                $"Grund: {reason}\n\n" +
                $"Bargeld: {player.Money}€\n" +
                $"Fehlend: {missingAmount}€\n\n" +
                $"Wähle ein Unternehmen zum Versteigern:\n" +
                $"(Versteigerungspreis = 50% der Gründungskosten)";
        }

        // Lösche alte Buttons
        if (auctionButtonContainer != null)
        {
            foreach (Transform child in auctionButtonContainer)
            {
                Destroy(child.gameObject);
            }
        }

        // Erstelle Buttons für alle versteigerbaren Unternehmen
        var auctionableCompanies = gm.GetAuctionableCompanies(player);
        if (auctionableCompanies == null || auctionableCompanies.Count == 0)
        {
            Debug.LogWarning("Keine Unternehmen zum Versteigern gefunden!");
            if (bankruptcyBodyText)
            {
                bankruptcyBodyText.text += "\n\n⚠️ Keine Unternehmen verfügbar!";
            }
            // Zeige Panel trotzdem, damit man sieht dass man pleite ist
            ShowContent(bankruptcyContent);
            return;
        }

        foreach (var field in auctionableCompanies)
        {
            var company = gm.gameInitiator.companyConfigs?.companies?.FirstOrDefault(c => c.companyID == field.companyID);
            if (company == null) continue;

            int auctionPrice = company.costFound / 2;
            string companyName = company.companyName;
            string levelText = field.level.ToString();

            // Erstelle Button
            if (auctionButtonContainer != null)
            {
                GameObject buttonObj;
                
                // Wenn Prefab vorhanden, verwende es, sonst erstelle Button zur Laufzeit
                if (auctionButtonPrefab != null)
                {
                    buttonObj = Instantiate(auctionButtonPrefab, auctionButtonContainer);
                }
                else
                {
                    // Erstelle Button zur Laufzeit ohne Prefab
                    buttonObj = new GameObject($"AuctionButton_{companyName}");
                    buttonObj.transform.SetParent(auctionButtonContainer, false);
                    
                    // RectTransform hinzufügen
                    RectTransform rectTransform = buttonObj.AddComponent<RectTransform>();
                    rectTransform.sizeDelta = new Vector2(300, 60);
                    
                    // Image Component für Button-Hintergrund
                    UnityEngine.UI.Image buttonImage = buttonObj.AddComponent<UnityEngine.UI.Image>();
                    buttonImage.color = new Color(0.2f, 0.2f, 0.2f, 1f);
                    
                    // Button Component
                    Button buttonComponent = buttonObj.AddComponent<Button>();
                    buttonComponent.targetGraphic = buttonImage;
                    
                    // TextMeshPro Text hinzufügen
                    GameObject textObj = new GameObject("Text (TMP)");
                    textObj.transform.SetParent(buttonObj.transform, false);
                    RectTransform textRect = textObj.AddComponent<RectTransform>();
                    textRect.anchorMin = Vector2.zero;
                    textRect.anchorMax = Vector2.one;
                    textRect.sizeDelta = Vector2.zero;
                    textRect.anchoredPosition = Vector2.zero;
                    
                    TextMeshProUGUI textComponent = textObj.AddComponent<TextMeshProUGUI>();
                    textComponent.text = $"{companyName} ({levelText})\n{auctionPrice}€";
                    textComponent.fontSize = 16;
                    textComponent.alignment = TextAlignmentOptions.Center;
                    textComponent.color = Color.white;
                }
                
                Button button = buttonObj.GetComponent<Button>();
                TextMeshProUGUI buttonText = buttonObj.GetComponentInChildren<TextMeshProUGUI>();

                if (buttonText)
                {
                    buttonText.text = $"{companyName} ({levelText})\n{auctionPrice}€";
                }

                if (button)
                {
                    // Speichere field für den Callback
                    CompanyField capturedField = field;
                    button.onClick.RemoveAllListeners();
                    button.onClick.AddListener(() =>
                    {
                        gm.StartAuctionForCompany(capturedField);
                    });
                }
            }
        }

        // Cancel Button
        if (bankruptcyCancelButton)
        {
            bankruptcyCancelButton.onClick.RemoveAllListeners();
            bankruptcyCancelButton.onClick.AddListener(() =>
            {
                gm.CancelBankruptcy();
            });
        }

        ShowContent(bankruptcyContent);
    }

    public void HideBankruptcyAuction()
    {
        ClosePopup();

        // Lösche alle Buttons
        if (auctionButtonContainer != null)
        {
            foreach (Transform child in auctionButtonContainer)
            {
                Destroy(child.gameObject);
            }
        }
    }

    // ============================================================
    // 🏁 GAME OVER UI
    // ============================================================

    public void ShowGameOver(List<PlayerRanking> rankings)
    {
        if (!gameOverContent)
        {
            Debug.LogError("GameOverContent fehlt im UIManager!");
            return;
        }

        for(int i = 0; i < gameOverObjects.Count; i++)
        {
            gameOverObjects[i].SetActive(false);
        }

        if (gameOverTitleText)
        {
            gameOverTitleText.text = "Spiel beendet!";
        }

        if (gameOverBodyText && rankings != null && rankings.Count > 0)
        {
            var winner = rankings[0];
            string winnerName = string.IsNullOrEmpty(winner.player.PlayerName) 
                ? $"Spieler {winner.player.PlayerID}" 
                : winner.player.PlayerName;
            
            gameOverBodyText.text = 
                $"Gewinner: {winnerName}\n" +
                $"Vermögen: {winner.totalAssets:N0}€\n" +
                $"(Bargeld: {winner.money:N0}€, Unternehmen: {winner.companyCount})";
        }

        // Lösche alte Ranking-Einträge
        if (rankingContainer != null)
        {
            foreach (Transform child in rankingContainer)
            {
                Destroy(child.gameObject);
            }
        }

        // Erstelle Ranking-Einträge
        if (rankings != null && rankings.Count > 0 && rankingContainer != null)
        {
            // Hole Font von einem existierenden TMP Text (z.B. vom Body Text)
            TMP_FontAsset fontToUse = null;
            if (gameOverBodyText != null)
            {
                fontToUse = gameOverBodyText.font;
            }

            for (int i = 0; i < rankings.Count; i++)
            {
                var ranking = rankings[i];
                string playerName = string.IsNullOrEmpty(ranking.player.PlayerName) 
                    ? $"Spieler {ranking.player.PlayerID}" 
                    : ranking.player.PlayerName;

                // Erstelle Entry zur Laufzeit
                GameObject entryObj = new GameObject($"RankingEntry_{i + 1}");
                entryObj.transform.SetParent(rankingContainer, false);
                
                RectTransform rectTransform = entryObj.AddComponent<RectTransform>();
                rectTransform.sizeDelta = new Vector2(600, 60);
                
                TextMeshProUGUI textComponent = entryObj.AddComponent<TextMeshProUGUI>();
                textComponent.fontSize = 60; // Moderate Schriftgröße
                textComponent.alignment = TextAlignmentOptions.Center;
                
                // Font zuweisen
                if (fontToUse != null)
                {
                    textComponent.font = fontToUse;
                }
                
                textComponent.text = 
                    $"{i + 1}. {playerName}\n" +
                    $"Vermögen: {ranking.totalAssets:N0}€ " +
                    $"(Bargeld: {ranking.money:N0}€, Unternehmen: {ranking.companyCount})";
            }
        }

        // Menu Button
        if (gameOverMenuButton)
        {
            gameOverMenuButton.onClick.RemoveAllListeners();
            gameOverMenuButton.onClick.AddListener(() =>
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene("Demo 2");
            });
        }

        // Neues Spiel Button
        if (gameOverNewGameButton)
        {
            gameOverNewGameButton.onClick.RemoveAllListeners();
            gameOverNewGameButton.onClick.AddListener(() =>
            {
                // Lösche gespeichertes Spiel und starte neu
                PlayerPrefs.SetInt("LoadSavedGame", 0);
                PlayerPrefs.Save();
                
                // Lade die Spielszene neu
                if (!string.IsNullOrEmpty(gameSceneName))
                {
                    UnityEngine.SceneManagement.SceneManager.LoadScene(gameSceneName);
                }
                else
                {
                    Debug.LogError("GameSceneName ist nicht gesetzt! Kann neues Spiel nicht starten.");
                }
            });
        }

        ShowContent(gameOverContent);
    }

    public void HideGameOver()
    {
        ClosePopup();

        // Lösche alle Ranking-Einträge
        if (rankingContainer != null)
        {
            foreach (Transform child in rankingContainer)
            {
                Destroy(child.gameObject);
            }
        }
    }
}
