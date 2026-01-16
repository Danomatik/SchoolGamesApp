using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using System.Collections.Generic;

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

    [Header("Timer Display")]
    [SerializeField] private TextMeshProUGUI timerDisplayText; // Display für Timer (optional)

    [Header("Initiative Popup")]
    [SerializeField] private GameObject initiativePanel;
    [SerializeField] private TextMeshProUGUI initiativeText;

    [Header("Bankruptcy Auction Panel")]
    [SerializeField] private GameObject bankruptcyPanel;
    [SerializeField] private TextMeshProUGUI bankruptcyTitleText;
    [SerializeField] private TextMeshProUGUI bankruptcyBodyText;
    [SerializeField] private Transform auctionButtonContainer; // Container für Versteigerungs-Buttons
    [SerializeField] private GameObject auctionButtonPrefab; // Prefab für einen Versteigerungs-Button (OPTIONAL - wird zur Laufzeit erstellt falls nicht vorhanden)
    [SerializeField] private Button bankruptcyCancelButton; // Button zum Abbrechen

    [Header("Game Over Panel")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private TextMeshProUGUI gameOverTitleText;
    [SerializeField] private TextMeshProUGUI gameOverBodyText;
    [SerializeField] private Transform rankingContainer; // Container für Ranking-Einträge
    [SerializeField] private Button gameOverMenuButton; // Button zum Zurück zum Menü
    [SerializeField] private Button gameOverNewGameButton; // Button für Neues Spiel
    [SerializeField] private string gameSceneName = "Demo"; // Name der Spielszene für Neues Spiel
    [SerializeField] private List<GameObject> gameOverObjects = new List<GameObject>();

    private GameManager gm;

    private void Awake()
    {
        gm = GetComponent<GameManager>(); // alle Manager am selben GO
        if (companyPanel != null) companyPanel.SetActive(false);
        if (initiativePanel != null) initiativePanel.SetActive(false);
        if (bankruptcyPanel != null) bankruptcyPanel.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
    }

    private void Start()
    {
        // Initial money display update
        UpdateMoneyDisplay();
        
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
        // This ensures the current player is always correct after turn changes
        //UpdateMoneyDisplay();
    }

    public void UpdateMoneyDisplay()
    {
        if (moneyDisplayText == null || gm == null) return;

        var currentPlayer = gm.GetCurrentPlayer();
        if (currentPlayer != null)
        {
            // ✅ Verwende PlayerName statt "Spieler {ID}"
            string playerName = string.IsNullOrEmpty(currentPlayer.PlayerName) 
                ? $"Spieler {currentPlayer.PlayerID}" 
                : currentPlayer.PlayerName;
            
            // ✅ Modernes Design: PLATINUM für Name, MINT für Geld mit besserem Kontrast
            moneyDisplayText.text = $"<b><color=#FFFFFF>{playerName}</color></b>\n<color=#96C23D><size=+3><b>{currentPlayer.Money:N0}</b></size>€</color>";
        }
        else
        {
            moneyDisplayText.text = "<color=#FFFFFF>--- €</color>";
        }
    }
    
    /// <summary>
    /// Aktualisiert die Timer-Anzeige
    /// </summary>
    public void UpdateTimerDisplay(float timeRemainingInSeconds)
    {
        if (timerDisplayText == null) return;

        int minutes = Mathf.Max(0, Mathf.FloorToInt(timeRemainingInSeconds / 60f));
        int seconds = Mathf.Max(0, Mathf.FloorToInt(timeRemainingInSeconds % 60f));

        // ✅ Modernes Design: SKY BLUE normal, BUSINESS bei Warnung mit besserem Kontrast
        string color = timeRemainingInSeconds < 300f ? "#D79244" : "#3EBCD5";
        timerDisplayText.text = $"<color={color}><size=+2><b>{minutes:D2}:{seconds:D2}</b></size></color>";
        
        // ✅ Zentriere die Timer-Anzeige
        timerDisplayText.alignment = TextAlignmentOptions.Center;
    }

    public void ShowInitiativeRoll(string playerLabel, int roll)
    {
        if (!initiativePanel || !initiativeText) return;
        initiativePanel.SetActive(true);
        initiativeText.text = $"{playerLabel}: {roll}";
    }

    public void HideInitiative()
    {
        if (!initiativePanel) return;
        initiativePanel.SetActive(false);
    }


    // Freies Feld → Kaufen oder Verzichten (Buttons 1/2)
    public void ShowCompanyPurchase(CompanyConfigData company, CompanyField field, PlayerData player)
    {
        if (!companyPanel) { Debug.LogError("CompanyPanel fehlt!"); return; }

        companyPanel.SetActive(true);
        
        // ✅ Konfiguriere Text-Elemente für korrekte Anzeige (nur Word Wrapping)
        if (titleText != null)
        {
            titleText.enableWordWrapping = true;
            titleText.overflowMode = TextOverflowModes.Page;
        }
        if (bodyText != null)
        {
            bodyText.enableWordWrapping = true;
            bodyText.overflowMode = TextOverflowModes.Page;
        }
        
        // ✅ Modernes Design mit besserem Kontrast: SKY BLUE für Titel, BUSINESS für Kosten, MINT für Ertrag
        titleText.text = $"<b><color=#3EBCD5><size=+1>{company.companyName}</size></color></b>\n<size=90%><color=#FFFFFF>Gründung</color></size>";
        bodyText.text =
            $"<color=#FFFFFF>Kosten:</color> <b><color=#D79244>{company.costFound:N0}€</color></b>\n" +
            $"<color=#FFFFFF>Ertrag pro Runde:</color> <b><color=#96C23D>{company.revenueFound:N0}€</color></b>\n\n" +
            $"<color=#FFFFFF>Möchtest du gründen?</color>\n<size=90%><color=#C6E6F0>(Quiz erforderlich)</color></size>";

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
        
        // ✅ Konfiguriere Text-Elemente für korrekte Anzeige (nur Word Wrapping)
        if (titleText != null)
        {
            titleText.enableWordWrapping = true;
            titleText.overflowMode = TextOverflowModes.Page;
        }
        if (bodyText != null)
        {
            bodyText.enableWordWrapping = true;
            bodyText.overflowMode = TextOverflowModes.Page;
        }
        
        // ✅ Modernes Design mit besserem Kontrast: SKY BLUE für Titel, BUSINESS für Kosten, MINT für Ertrag
        titleText.text = $"<b><color=#3EBCD5><size=+1>{company.companyName}</size></color></b>\n<size=90%><color=#FFFFFF>Upgrade</color></size>";
        
        string statusColor = field.level == CompanyLevel.Founded ? "#96C23D" : "#D79244";
        bodyText.text =
            $"<color=#FFFFFF>Aktueller Status:</color> <b><color={statusColor}>{field.level}</color></b>\n\n" +
            $"<color=#FFFFFF>Investieren:</color> <b><color=#D79244>{company.costInvest:N0}€</color></b> → <color=#96C23D>Ertrag {company.revenueInvest:N0}€</color>\n" +
            $"<color=#FFFFFF>AG gründen:</color> <b><color=#D79244>{company.costAG:N0}€</color></b> → <color=#96C23D>Ertrag {company.revenueAG:N0}€</color>\n\n" +
            $"<color=#FFFFFF>Wähle ein Upgrade:</color>\n<size=90%><color=#C6E6F0>(Quiz erforderlich)</color></size>";

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
        
        // ✅ Styling für Button-Hintergrund mit Farbschema
        StyleButtonBackground(btn, label);
        
        // ✅ Modernes Styling für Button-Text mit besserem Kontrast
        var txt = btn.GetComponentInChildren<TextMeshProUGUI>();
        if (txt)
        {
            string formattedLabel = FormatButtonText(label);
            txt.text = formattedLabel;
            txt.fontStyle = FontStyles.Bold;
            txt.alignment = TextAlignmentOptions.Center;
            
            // Subtiler Outline für bessere Lesbarkeit und modernen Look
            txt.outlineWidth = 0.2f;
            txt.outlineColor = new Color(0, 0, 0, 0.4f); // Leichter schwarzer Outline
        }
        
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(() => onClick?.Invoke());
    }
    
    /// <summary>
    /// Stylt den Button-Hintergrund mit Farbschema und abgerundeten Ecken
    /// </summary>
    private void StyleButtonBackground(Button btn, string label)
    {
        var image = btn.GetComponent<UnityEngine.UI.Image>();
        if (image == null) return;
        
        // Moderne Farben: MINT für positive Aktionen, BUSINESS für negative/neutrale
        Color bgColor = new Color(0.3f, 0.3f, 0.33f, 1f); // Standard: PLATINUM ähnlich
        
        switch (label.ToLower())
        {
            case "gründen":
            case "investieren":
            case "ag gründen":
                // MINT (#96C23D) für positive Aktionen - etwas heller für moderneren Look
                bgColor = new Color(0.588f, 0.761f, 0.239f, 1f); // #96C23D
                break;
            case "verzichten":
            case "später":
                // BUSINESS (#D79244) für negative/neutrale Aktionen
                bgColor = new Color(0.843f, 0.573f, 0.267f, 1f); // #D79244
                break;
        }
        
        image.color = bgColor;
        
        // Abgerundete Ecken: Unity UI unterstützt keine direkten rounded corners,
        // aber wir können die Button-Größe anpassen für einen moderneren Look
        RectTransform rectTransform = btn.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            // Stelle sicher, dass der Button eine angemessene Größe hat
            // Die abgerundeten Ecken müssen im Unity Editor mit einem Sprite/Material gesetzt werden
            // Hier können wir nur die visuelle Erscheinung verbessern
        }
    }
    
    /// <summary>
    /// Formatiert Button-Text mit modernem Design
    /// </summary>
    private string FormatButtonText(string label)
    {
        // Weißer Text mit Outline für maximalen Kontrast und modernen Look
        string textColor = "#FFFFFF";
        
        // Title Case für professionellen Look
        string displayText = label;
        if (displayText.Length > 0)
        {
            displayText = char.ToUpper(displayText[0]) + (displayText.Length > 1 ? displayText.Substring(1).ToLower() : "");
        }
        
        // Moderner Text mit besserer Lesbarkeit
        return $"<size=+2><b><color={textColor}>{displayText}</color></b></size>";
    }

    private void Close()
    {
        if (companyPanel) companyPanel.SetActive(false);
    }

    // ============================================================
    // 💰 INSOLVENZ & VERSTEIGERUNG UI
    // ============================================================

    /// <summary>
    /// Zeigt das Versteigerungs-Panel an
    /// </summary>
    public void ShowBankruptcyAuction(PlayerData player, int missingAmount, string reason)
    {
        if (!bankruptcyPanel)
        {
            Debug.LogError("BankruptcyPanel fehlt im UIManager!");
            return;
        }

        bankruptcyPanel.SetActive(true);

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
            else
            {
                Debug.LogWarning("AuctionButtonContainer fehlt! Kann keine Buttons erstellen.");
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
    }

    /// <summary>
    /// Versteckt das Versteigerungs-Panel
    /// </summary>
    public void HideBankruptcyAuction()
    {
        if (bankruptcyPanel)
        {
            bankruptcyPanel.SetActive(false);
        }

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

    /// <summary>
    /// Zeigt das Game Over Panel mit Rankings an
    /// </summary>
    public void ShowGameOver(List<PlayerRanking> rankings)
    {
        if (!gameOverPanel)
        {
            Debug.LogError("GameOverPanel fehlt im UIManager!");
            return;
        }

        for(int i = 0; i < gameOverObjects.Count; i++)
        {
            gameOverObjects[i].SetActive(false);
        }

        gameOverPanel.SetActive(true);

        

        // ✅ Modernes Formatting für Titel mit Farbschema
        if (gameOverTitleText)
        {
            gameOverTitleText.text = $"<b><color=#3EBCD5><size=+2>Spiel beendet!</size></color></b>";
            gameOverTitleText.alignment = TextAlignmentOptions.Center;
        }

        // ✅ Modernes Formatting für Body Text mit Gewinner
        if (gameOverBodyText && rankings != null && rankings.Count > 0)
        {
            var winner = rankings[0];
            string winnerName = string.IsNullOrEmpty(winner.player.PlayerName) 
                ? $"Spieler {winner.player.PlayerID}" 
                : winner.player.PlayerName;
            
            gameOverBodyText.text = 
                $"<b><color=#96C23D>Gewinner:</color></b> <color=#FFFFFF>{winnerName}</color>\n" +
                $"<color=#FFFFFF>Vermögen:</color> <b><color=#96C23D>{winner.totalAssets:N0}€</color></b>\n" +
                $"<size=90%><color=#C6E6F0>(Bargeld: {winner.money:N0}€, Unternehmen: {winner.companyCount})</color></size>";
            gameOverBodyText.alignment = TextAlignmentOptions.Center;
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
                
                // ✅ Modernes Formatting mit Farbschema für Ranking-Einträge
                string rankColor = i == 0 ? "#96C23D" : (i == 1 ? "#3EBCD5" : "#FFFFFF"); // MINT für 1., SKY BLUE für 2., Weiß für Rest
                
                textComponent.text = 
                    $"<b><color={rankColor}>{i + 1}. {playerName}</color></b>\n" +
                    $"<size=85%><color=#FFFFFF>Vermögen: <b><color=#96C23D>{ranking.totalAssets:N0}€</color></b> " +
                    $"(Bargeld: {ranking.money:N0}€, Unternehmen: {ranking.companyCount})</color></size>";
            }
            
            Debug.Log($"[UIManager] {rankings.Count} Ranking-Einträge erstellt im RankingContainer.");
        }
        else
        {
            Debug.LogWarning($"[UIManager] Ranking-Einträge nicht erstellt. Rankings: {rankings?.Count ?? 0}, Container: {(rankingContainer != null ? "vorhanden" : "NULL")}");
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
    }

    /// <summary>
    /// Versteckt das Game Over Panel
    /// </summary>
    public void HideGameOver()
    {
        if (gameOverPanel)
        {
            gameOverPanel.SetActive(false);
        }

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
