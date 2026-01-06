using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using System.Linq;

public class GameManager : MonoBehaviour
{
    // ============================================================
    // 🟩 GAME MANAGER SETTINGS
    // ============================================================

    [Header("GAME")]
    public List<PlayerCTRL> players;

    [HideInInspector] public GameInitiator gameInitiator;
    [HideInInspector] public QuestionManager questionManager;
    [HideInInspector] public BankCardManager bankCardManager;
    [HideInInspector] public UIManager uiManager;
    [HideInInspector] public BoardVisualsManager boardVisuals;
    [HideInInspector] public DiceManager diceManager;
    [HideInInspector] public PlayerMovement playerMovement;
    [HideInInspector] public CameraManager cameraManager;
    [HideInInspector] public MoneyManager moneyManager;
    [HideInInspector] public ActionCardManager actionCardManager;
    [HideInInspector] public ActionManager actionManager;
    [HideInInspector] public FieldSelector fieldSelector;
    [HideInInspector] public GameTimerManager gameTimerManager;



    [Header("UI")]

    [SerializeField]
    private GameObject moneyDisplay;

    [Header("Save System")]
    [SerializeField]
    private UnityEngine.UI.Button saveButton; // Assign your Save button in inspector
    [SerializeField]
    private string menuSceneName = "MenuScene"; // Scene name to return to (set in inspector)

    // Pending für Quiz-Kauf/Upgrade
    private struct PendingPurchase
    {
        public CompanyConfigData company;
        public CompanyField field;
        public PlayerData player;
        public CompanyLevel targetLevel;
        public bool isActive;
    }
    private PendingPurchase pending;

    // Spieler -> wie viele kommende Züge noch aussetzen


    // ============================================================
    // 🏁 UNITY METHODS
    // ============================================================
    public void Awake()
    {
        uiManager = GetComponent<UIManager>();
        // gameManager = GetComponent<GameManager>();
        diceManager = GetComponent<DiceManager>();
        cameraManager = GetComponent<CameraManager>();
        gameInitiator = GetComponent<GameInitiator>();
        bankCardManager = GetComponent<BankCardManager>();
        questionManager = GetComponent<QuestionManager>();
        boardVisuals = GetComponent<BoardVisualsManager>();
        moneyManager = GetComponent<MoneyManager>();
        playerMovement = GetComponent<PlayerMovement>();
        actionCardManager = GetComponent<ActionCardManager>();
        actionManager = GetComponent<ActionManager>();
        fieldSelector = GetComponent<FieldSelector>();
        gameTimerManager = GetComponent<GameTimerManager>();
        
        // Erstelle GameTimerManager falls nicht vorhanden
        if (gameTimerManager == null)
        {
            gameTimerManager = gameObject.AddComponent<GameTimerManager>();
            Debug.Log("[GameManager] GameTimerManager automatisch hinzugefügt.");
        }
    }

    void Start()
    {
        // Refresh board visuals (important for loaded games)
        if (boardVisuals != null) 
        {
            boardVisuals.RefreshAll(gameInitiator.GetCompanyFields());
        }

        // Update UI with current player data
        if (uiManager != null)
        {
            uiManager.UpdateMoneyDisplay();
        }

        // Setup save button
        if (saveButton != null)
        {
            saveButton.onClick.AddListener(OnSaveButtonClicked);
        }

        TestCurrencySystem();
    }

    /// <summary>
    /// Called when Save button is clicked
    /// This method saves the game and then uses SwitchScene to load the target scene
    /// </summary>
// ============================================================
// 💾 AUTO SAVE – APP LIFECYCLE
// ============================================================
// ============================================================
// 💾 TURN AUTO SAVE
// ============================================================

private float lastAutoSaveTime;
[SerializeField] private float autoSaveCooldown = 3f;

private void AutoSave()
{
    if (Time.time - lastAutoSaveTime < autoSaveCooldown)
        return;

    lastAutoSaveTime = Time.time;

    var saveManager = FindFirstObjectByType<GameSaveManager>();
    if (saveManager == null)
    {
        GameObject obj = new GameObject("GameSaveManager");
        saveManager = obj.AddComponent<GameSaveManager>();
    }

    saveManager.SaveGame(gameInitiator);
    Debug.Log("💾 Auto-save (EndTurn)");
}

private bool hasSavedOnQuit = false;

private void OnApplicationQuit()
{
    Debug.Log("🛑 Application quitting – auto-saving game...");
    SaveOnExit();
}

private void OnApplicationPause(bool pause)
{
    if (pause)
    {
        Debug.Log("⏸ Application paused – auto-saving game...");
        SaveOnExit();
    }
    else
    {
        // App resumed → allow future saves again
        hasSavedOnQuit = false;
    }
}


private void SaveOnExit()
{
    if (hasSavedOnQuit) return; // Prevent double-save
    hasSavedOnQuit = true;

    var saveManager = FindFirstObjectByType<GameSaveManager>();
    if (saveManager == null)
    {
        GameObject saveManagerObj = new GameObject("GameSaveManager");
        saveManager = saveManagerObj.AddComponent<GameSaveManager>();
    }

    bool saved = saveManager.SaveGame(gameInitiator);
    if (saved)
    {
        Debug.Log("✅ Auto-save successful");
    }
    else
    {
        Debug.LogError("❌ Auto-save failed!");
    }
}

    public void OnSaveButtonClicked()
    {
        SaveGameAndReturnToMenu();
    }

    /// <summary>
    /// Alternative: Save and then call SwitchScene directly
    /// Use this if your button only has SwitchScene script
    /// </summary>
    public void SaveAndSwitchScene()
    {
        // First save the game
        var saveManager = FindFirstObjectByType<GameSaveManager>();
        if (saveManager == null)
        {
            GameObject saveManagerObj = new GameObject("GameSaveManager");
            saveManager = saveManagerObj.AddComponent<GameSaveManager>();
        }

        bool saved = saveManager.SaveGame(gameInitiator);
        if (saved)
        {
            Debug.Log("✅ Spiel gespeichert! Wechsle zur nächsten Scene...");
            
            // Now find and use SwitchScene
            SwitchScene switchScene = FindFirstObjectByType<SwitchScene>();
            if (switchScene != null && !string.IsNullOrEmpty(switchScene.sceneToLoad))
            {
                switchScene.LoadTargetScene();
            }
            else
            {
                Debug.LogError("❌ SwitchScene nicht gefunden oder sceneToLoad ist nicht gesetzt!");
            }
        }
        else
        {
            Debug.LogError("❌ Fehler beim Speichern des Spiels!");
        }
    }

    /// <summary>
    /// Saves the current game and returns to menu using SwitchScene script
    /// </summary>
    public void SaveGameAndReturnToMenu()
    {
        var saveManager = FindFirstObjectByType<GameSaveManager>();
        if (saveManager == null)
        {
            GameObject saveManagerObj = new GameObject("GameSaveManager");
            saveManager = saveManagerObj.AddComponent<GameSaveManager>();
        }

        bool saved = saveManager.SaveGame(gameInitiator);
        if (saved)
        {
            Debug.Log("✅ Spiel gespeichert! Kehre zum Menü zurück...");
            
            // Try to find SwitchScene script (on save button or anywhere)
            SwitchScene switchScene = null;
            if (saveButton != null)
            {
                switchScene = saveButton.GetComponent<SwitchScene>();
            }
            
            // If not found on button, search in scene
            if (switchScene == null)
            {
                switchScene = FindFirstObjectByType<SwitchScene>();
            }
            
            if (switchScene != null && !string.IsNullOrEmpty(switchScene.sceneToLoad))
            {
                Debug.Log($"✅ Verwende SwitchScene zum Laden von: {switchScene.sceneToLoad}");
                // Use SwitchScene to load the scene
                switchScene.LoadTargetScene();
                return;
            }
            
            // Fallback: Direct scene loading if SwitchScene not found
            Debug.LogWarning("⚠️ SwitchScene nicht gefunden oder sceneToLoad ist leer. Verwende Fallback.");
            if (!string.IsNullOrEmpty(menuSceneName))
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene(menuSceneName);
            }
            else
            {
                Debug.LogError("❌ Menu scene name is not set! Bitte füge SwitchScene-Script zum Save-Button hinzu und setze 'Scene To Load' auf 'Demo 2'.");
            }
        }
        else
        {
            Debug.LogError("❌ Fehler beim Speichern des Spiels!");
        }
    }


    CompanyConfigData GetCompanyConfig(int id)
    {
        return gameInitiator.companyConfigs?.companies?.FirstOrDefault(c => c.companyID == id)
            ?? gameInitiator.companyConfigs?.companies?.FirstOrDefault();
    }

    public void HandleCompanyField(CompanyField field)
    {
        var current = GetCurrentPlayer();
        var company = GetCompanyConfig(field.companyID);

        if (field.ownerID == -1)
        {
            // frei -> Kauf anbieten
            uiManager.ShowCompanyPurchase(company, field, current);
            // Zug NICHT beenden – OnQuizResult übernimmt das
        }
        else if (field.ownerID == current.PlayerID)
        {
            // Nur zeigen, wenn noch Upgrades offen
            if (field.level == CompanyLevel.Founded || field.level == CompanyLevel.Invested)
                uiManager.ShowUpgradeOptions(company, field, current);
            else
                EndTurn(); // AG -> nichts mehr zu tun
        }
        else
        {
            // fremdes Feld -> Miete zahlen
            var owner = gameInitiator.CurrentGame.AllPlayers.FirstOrDefault(p => p.PlayerID == field.ownerID);
            moneyManager.PayRent(current, owner, company, field);
            // EndTurn() wird von PayRent/Insolvenz-Logik aufgerufen, wenn Zahlung erfolgreich
            // Wenn Insolvenz ausgelöst wird, wird EndTurn() nach Versteigerung aufgerufen
        }
    }

    private bool IsUpgradeAllowed(CompanyLevel current, CompanyLevel target)
    {
        // Nur stufenweise:
        // None -> Founded -> Invested -> AG
        if (target == CompanyLevel.Founded) return current == CompanyLevel.None;
        if (target == CompanyLevel.Invested) return current == CompanyLevel.Founded;
        if (target == CompanyLevel.AG) return current == CompanyLevel.Invested;
        return false;
    }

    public void StartQuizForCompany(CompanyConfigData company, CompanyField field, PlayerData player, CompanyLevel targetLevel)
    {
        // NEU: Stufen-Check
        if (!IsUpgradeAllowed(field.level, targetLevel))
        {
            Debug.LogWarning($"Upgrade nicht erlaubt: {field.level} -> {targetLevel}");
            // Optional: sofort beenden oder Upgrade-Panel erneut zeigen:
            // uiManager.ShowUpgradeOptions(company, field, player);
            EndTurn();
            return;
        }

        pending = new PendingPurchase
        {
            company = company,
            field = field,
            player = player,
            targetLevel = targetLevel,
            isActive = true
        };

        if (questionManager != null)
        {
            questionManager.PrintRandomQuestion();
            questionManager.ShowQuestionInUI();
        }
        else
        {
            Debug.LogWarning("QuestionManager fehlt – simuliere Erfolg.");
            OnQuizResult(true);
        }
    }

    public void StartQuizForAG()
    {
        var player = GetCurrentPlayer();
        if (player == null)
        {
            Debug.LogWarning("StartQuizForAG: No current player.");
            EndTurn();
            return;
        }

        if (!TryGetEligibleCompaniesForAG(player, out var eligible))
        {
            Debug.Log("StartQuizForAG: Player has no eligible companies → skipping AG quiz.");
            // Optional: show a small popup/toast in your UI here.
            EndTurn();
            return;
        }

        // If you reach here, player owns at least one eligible company.
        // Start your 3-question series (from earlier message):
        if (diceManager != null && diceManager.moveButton != null)
            diceManager.moveButton.SetActive(false);

        // total=3, require all 3 correct (or set to 2 if you prefer 2/3)
        int totalQuestions = 3;
        int requiredCorrect = 3;

        questionManager.StartQuizSeries(totalQuestions, requiredCorrect, success =>
        {
            if (success)
            {
                Debug.Log("AG Upgrade Quiz PASSED. Show selection UI to pick which owned company to upgrade to AG for free.");
                // TODO: uiManager.ShowAgUpgradeSelection(player, eligible, (chosenField) => { chosenField.level = CompanyLevel.AG; boardVisuals.UpdateFieldVisual(chosenField); ... });
            }
            else
            {
                Debug.Log("AG Upgrade Quiz FAILED.");
            }

            if (diceManager != null && diceManager.moveButton != null)
                diceManager.moveButton.SetActive(true);

            EndTurn();
        });
    }



    public void OnQuizResult(bool correct)
    {
        if (!pending.isActive)
        {
            EndTurn();
            return;
        }

        if (!correct)
        {
            Debug.Log("Quiz nicht bestanden. Kauf/Upgrade abgelehnt.");
            pending = default;
            EndTurn();
            return;
        }

        int cost = 0;
        switch (pending.targetLevel)
        {
            case CompanyLevel.Founded: cost = pending.company.costFound; break;
            case CompanyLevel.Invested: cost = pending.company.costInvest; break;
            case CompanyLevel.AG: cost = pending.company.costAG; break;
        }

        // Prüfe Insolvenz vor Kauf/Upgrade
        if (!moneyManager.TryPayAmount(pending.player, cost, $"Kauf/Upgrade von {pending.company.companyName}"))
        {
            // Insolvenz wird in TryPayAmount behandelt
            pending = default;
            return; // Versteigerung läuft, EndTurn wird später aufgerufen
        }

        // Zahlung erfolgreich - Unternehmen kaufen/upgraden
        pending.field.ownerID = pending.player.PlayerID;
        pending.field.level = pending.targetLevel;
        uiManager.UpdateMoneyDisplay();
        pending.player.companies.Add(pending.field.fieldIndex);
        Debug.Log("Added Field: " + pending.field.fieldIndex + " to Player: " + pending.player.PlayerID);

        // NEU: Visuals
        if (boardVisuals != null)
            boardVisuals.UpdateFieldVisual(pending.field);

        Debug.Log($"Spieler {pending.player.PlayerID} hat {pending.company.companyName} → {pending.targetLevel} gekauft/aufgerüstet (−{cost}€).");
        pending = default;
        EndTurn();
    }

    public PlayerData GetCurrentPlayer()
    {
        // 1. Check: Ist gameInitiator überhaupt da?
        if (gameInitiator == null)
        {
            Debug.LogError("GetCurrentPlayer: gameInitiator is NULL!");
            return null;
        }

        // 2. Check: Ist CurrentGame initialisiert?
        if (gameInitiator.CurrentGame == null)
        {
            Debug.LogError("GetCurrentPlayer: CurrentGame is NULL!");
            return null;
        }

        // 3. Check: Ist AllPlayers da?
        if (gameInitiator.CurrentGame.AllPlayers == null ||
            gameInitiator.CurrentGame.AllPlayers.Count == 0)
        {
            Debug.LogError("GetCurrentPlayer: AllPlayers is null or empty!");
            return null;
        }

        if (gameInitiator.CurrentGame.CurrentPlayerTurnID < 0 || gameInitiator.CurrentGame.CurrentPlayerTurnID >= gameInitiator.CurrentGame.AllPlayers.Count)
        {
            Debug.LogError($"GetCurrentPlayer: currentPlayerIndex {gameInitiator.CurrentGame.CurrentPlayerTurnID} is out of bounds! AllPlayers count: {gameInitiator.CurrentGame.AllPlayers.Count}");
            return null;
        }

        return gameInitiator.CurrentGame.AllPlayers[gameInitiator.CurrentGame.CurrentPlayerTurnID];
    }

    // ============================================================
    // 🏢 COMPANY FIELD METHODS
    // ============================================================

    // PUBLIC METHOD: Get all unowned company fields
    public List<CompanyField> GetUnownedCompanyFields()
    {
        List<CompanyField> unownedFields = new List<CompanyField>();

        if (gameInitiator == null || gameInitiator.CurrentGame == null)
        {
            Debug.LogWarning("GetUnownedCompanyFields: gameInitiator or CurrentGame is null");
            return unownedFields;
        }

        List<CompanyField> allFields = gameInitiator.GetCompanyFields();

        if (allFields == null || allFields.Count == 0)
        {
            Debug.LogWarning("GetUnownedCompanyFields: No company fields found");
            return unownedFields;
        }

        foreach (CompanyField field in allFields)
        {
            if (field.ownerID == -1)
            {
                unownedFields.Add(field);
            }
        }

        Debug.Log($"Found {unownedFields.Count} unowned company fields out of {allFields.Count} total fields");
        return unownedFields;
    }

    //-------------------------------------------------------------
    // Optional rule: allow skipping prerequisites (Invested -> AG)
    [Header("Rules")]
    [SerializeField] private bool agCardSkipsPrerequisites = true;

    // Returns a list of CompanyField objects owned by the player (by fieldIndex)
    private List<CompanyField> GetOwnedCompanyFields(PlayerData player)
    {
        var result = new List<CompanyField>();
        var all = gameInitiator?.GetCompanyFields();
        if (player == null || all == null) return result;

        // Player.companies stores field indices
        foreach (var fieldIndex in player.companies)
        {
            var cf = all.FirstOrDefault(f => f.fieldIndex == fieldIndex);
            if (cf != null && cf.ownerID == player.PlayerID)
                result.Add(cf);
        }
        return result;
    }


    // Filters owned companies for AG-eligibility
    private bool TryGetEligibleCompaniesForAG(PlayerData player, out List<CompanyField> eligible)
    {
        eligible = new List<CompanyField>();
        var owned = GetOwnedCompanyFields(player);
        if (owned.Count == 0) return false;

        foreach (var f in owned)
        {
            if (f.level == CompanyLevel.AG) continue; // already maxed

            // If you want strict ladder: only Invested -> AG
            // Otherwise (default) allow Founded/Invested -> AG (free upgrade card)
            bool ok = agCardSkipsPrerequisites ? true : (f.level == CompanyLevel.Invested);
            if (ok) eligible.Add(f);
        }
        return eligible.Count > 0;
    }
    //-------------------------------------------------------------




    // PUBLIC METHOD: Get field indices of unowned fields
    public List<int> GetUnownedFieldIndices()
    {
        List<int> indices = new List<int>();
        List<CompanyField> unownedFields = GetUnownedCompanyFields();

        foreach (CompanyField field in unownedFields)
        {
            indices.Add(field.fieldIndex);
        }

        return indices;
    }

    public List<int> GetBankAndActionFieldIndices()
    {
        List<int> bankAndActionFields = new List<int> { 5, 7, 10, 13, 20, 23, 27, 30, 37 };
        return bankAndActionFields;
    }

    public void EndTurn()
    {
        // zum nächsten Index
        gameInitiator.CurrentGame.CurrentPlayerTurnID++;
        if (gameInitiator.CurrentGame.CurrentPlayerTurnID >= gameInitiator.CurrentGame.AllPlayers.Count)
            gameInitiator.CurrentGame.CurrentPlayerTurnID = 0;

        uiManager.UpdateMoneyDisplay();

        var next = GetCurrentPlayer();
        if (next != null)
            Debug.Log($"Zug beendet. Spieler {next.PlayerID} ist jetzt an der Reihe.");
        else
            Debug.LogError("EndTurn: Could not get next player!");

        playerMovement.setIsTurnInProgress(false);  // wichtig

        if (next.hasToSkip)
        {
            Debug.Log($"Player {next.PlayerID} muss diesen Zug aussetzen!");
            next.hasToSkip = false; // zurücksetzen
            StartCoroutine(SkipTurnDelay());
            EndTurn();
            return;
        }

        // Kamera auf nächsten Spieler setzen
        PlayerCTRL activePlayer = players.Find(p => p.PlayerID == next.PlayerID);
        if (activePlayer != null)
        {
            Transform playerChild = activePlayer.transform.childCount > 0
                ? activePlayer.transform.GetChild(0)
                : activePlayer.transform;

            cameraManager.cam.Lens.OrthographicSize = cameraManager.defaultLens;
            cameraManager.cam.Follow = playerChild;
        }

        if (cameraManager.camBrain.IsBlending && cameraManager.camBrain.ActiveBlend != null)
        {
            GameObject moveButton = playerMovement.getMoveButton();
            moveButton.SetActive(true);
            moneyDisplay.SetActive(false);
        }
        else
        {
            uiManager.UpdateMoneyDisplay();
            GameObject moveButton = playerMovement.getMoveButton();
            moveButton.SetActive(true);
            moneyDisplay.SetActive(true);
        }

        AutoSave();
    }

    private IEnumerator SkipTurnDelay()
    {
        yield return new WaitForSeconds(1f); // kurze Pause, damit der Spieler den Text lesen kann
    }

    // ============================================================
    // 🧪 DEBUG & TESTING
    // ============================================================
    public void TestCurrencySystem()
    {
        Debug.Log("--- STARTE WÄHRUNGSSYSTEM-TEST ---");
        Debug.Log($"Anfangsgeld: {GetCurrentPlayer().Money}€");

        moneyManager.AddMoney(400);
        moneyManager.RemoveMoney(400);
        moneyManager.RemoveMoney(5000);

        Debug.Log($"--- TEST BEENDET --- Finaler Kontostand: {GetCurrentPlayer().Money}€");
    }

    public bool InitiativeInProgress { get; set; } = false;

    // ============================================================
    // 🧪 TEST FUNKTIONEN (für Insolvenz-Testing)
    // ============================================================

    void Update()
    {
        // Test-Shortcut: Drücke 'T' um Insolvenz zu testen
        if (Input.GetKeyDown(KeyCode.T))
        {
            TestBankruptcy();
        }
    }

    /// <summary>
    /// Test-Funktion: Simuliert eine Insolvenz-Situation
    /// Drücke 'T' während des Spiels um zu testen
    /// </summary>
    [ContextMenu("Test Bankruptcy")]
    public void TestBankruptcy()
    {
        var currentPlayer = GetCurrentPlayer();
        if (currentPlayer == null)
        {
            Debug.LogError("TestBankruptcy: Kein aktueller Spieler!");
            return;
        }

        Debug.Log($"🧪 TEST: Simuliere Insolvenz für Spieler {currentPlayer.PlayerID}");

        // Setze Spieler auf wenig Geld
        currentPlayer.Money = 100;
        uiManager.UpdateMoneyDisplay();

        // Versuche eine hohe Zahlung (z.B. 1000€)
        int testAmount = 1000;
        Debug.Log($"🧪 TEST: Versuche {testAmount}€ Zahlung...");

        // Simuliere eine Miete-Zahlung
        // Finde ein fremdes Unternehmen oder erstelle eine Test-Situation
        var allFields = gameInitiator.GetCompanyFields();
        var ownedByOthers = allFields.Where(f => f.ownerID != -1 && f.ownerID != currentPlayer.PlayerID).FirstOrDefault();

        if (ownedByOthers != null)
        {
            var company = GetCompanyConfig(ownedByOthers.companyID);
            var owner = gameInitiator.CurrentGame.AllPlayers.FirstOrDefault(p => p.PlayerID == ownedByOthers.ownerID);
            
            if (company != null && owner != null)
            {
                Debug.Log($"🧪 TEST: Zahle Miete für {company.companyName}...");
                moneyManager.PayRent(currentPlayer, owner, company, ownedByOthers);
            }
            else
            {
                // Fallback: Direkte Insolvenz auslösen
                Debug.Log($"🧪 TEST: Direkte Insolvenz auslösen...");
                HandleBankruptcy(currentPlayer, testAmount, "Test-Insolvenz");
            }
        }
        else
        {
            // Kein fremdes Unternehmen gefunden - direkte Insolvenz auslösen
            Debug.Log($"🧪 TEST: Kein fremdes Unternehmen gefunden. Direkte Insolvenz auslösen...");
            HandleBankruptcy(currentPlayer, testAmount, "Test-Insolvenz");
        }
    }

    /// <summary>
    /// Test-Funktion: Setzt Spieler auf wenig Geld
    /// </summary>
    [ContextMenu("Set Current Player Money to 100")]
    public void TestSetLowMoney()
    {
        var currentPlayer = GetCurrentPlayer();
        if (currentPlayer != null)
        {
            currentPlayer.Money = 100;
            uiManager.UpdateMoneyDisplay();
            Debug.Log($"🧪 TEST: Spieler {currentPlayer.PlayerID} Geld auf 100€ gesetzt");
        }
    }

    // ============================================================
    // 💰 INSOLVENZ & VERSTEIGERUNG
    // ============================================================

    private struct BankruptcyContext
    {
        public PlayerData bankruptPlayer;
        public int requiredAmount;
        public string reason;
        public PlayerData recipient; // Empfänger der Zahlung (z.B. bei Miete)
        public bool isActive;
    }
    private BankruptcyContext bankruptcyContext;

    /// <summary>
    /// Wird aufgerufen wenn ein Spieler eine Zahlung nicht leisten kann
    /// </summary>
    public void HandleBankruptcy(PlayerData player, int requiredAmount, string reason = "", PlayerData recipient = null)
    {
        if (player == null)
        {
            Debug.LogError("HandleBankruptcy: Player is null!");
            return;
        }

        Debug.Log($"🚨 INSOLVENZ: Spieler {player.PlayerID} ({player.PlayerName}) kann {requiredAmount}€ nicht bezahlen ({reason})");

        // Prüfe ob Spieler überhaupt Unternehmen besitzt
        if (player.companies == null || player.companies.Count == 0)
        {
            Debug.LogWarning($"Spieler {player.PlayerID} hat keine Unternehmen zum Versteigern. Zahlung kann nicht geleistet werden.");
            // Spieler bleibt im Spiel, aber kann nicht zahlen
            EndTurn();
            return;
        }

        // Berechne wie viel noch fehlt
        int missingAmount = requiredAmount - player.Money;
        if (missingAmount <= 0)
        {
            // Sollte nicht passieren, aber sicherheitshalber
            player.Money -= requiredAmount;
            uiManager.UpdateMoneyDisplay();
            EndTurn();
            return;
        }

        // Speichere Kontext für Versteigerung
        bankruptcyContext = new BankruptcyContext
        {
            bankruptPlayer = player,
            requiredAmount = requiredAmount,
            reason = reason,
            recipient = recipient,
            isActive = true
        };

        // Zeige Versteigerungs-UI
        uiManager.ShowBankruptcyAuction(player, missingAmount, reason);
    }

    /// <summary>
    /// Startet die Versteigerung eines Unternehmens
    /// </summary>
    public void StartAuctionForCompany(CompanyField field)
    {
        if (!bankruptcyContext.isActive)
        {
            Debug.LogError("StartAuctionForCompany: Kein aktiver Insolvenz-Kontext!");
            return;
        }

        var company = GetCompanyConfig(field.companyID);
        if (company == null)
        {
            Debug.LogError($"StartAuctionForCompany: Keine Company Config für ID {field.companyID} gefunden!");
            return;
        }

        // Versteigerungspreis = 50% der Gründungskosten
        int auctionPrice = company.costFound / 2;

        Debug.Log($"🔨 Versteigerung: {company.companyName} für {auctionPrice}€");

        // Verkaufe Unternehmen
        field.ownerID = -1; // Niemand besitzt es mehr
        field.level = CompanyLevel.None; // Zurücksetzen auf None
        bankruptcyContext.bankruptPlayer.companies.Remove(field.fieldIndex);

        // Spieler erhält Versteigerungspreis
        bankruptcyContext.bankruptPlayer.Money += auctionPrice;
        uiManager.UpdateMoneyDisplay();

        // Update Visuals
        if (boardVisuals != null)
            boardVisuals.UpdateFieldVisual(field);

        Debug.Log($"✅ {company.companyName} versteigert für {auctionPrice}€. Spieler {bankruptcyContext.bankruptPlayer.PlayerID} hat jetzt {bankruptcyContext.bankruptPlayer.Money}€");

        // Prüfe ob noch mehr versteigert werden muss
        CheckIfBankruptcyResolved();
    }

    /// <summary>
    /// Prüft ob die Insolvenz durch die Versteigerungen aufgelöst wurde
    /// </summary>
    private void CheckIfBankruptcyResolved()
    {
        if (!bankruptcyContext.isActive) return;

        int currentMoney = bankruptcyContext.bankruptPlayer.Money;
        int required = bankruptcyContext.requiredAmount;

        if (currentMoney >= required)
        {
            // Genug Geld vorhanden - Zahlung durchführen
            bankruptcyContext.bankruptPlayer.Money -= required;
            
            // Wenn es einen Empfänger gibt (z.B. bei Miete), erhält dieser das Geld
            if (bankruptcyContext.recipient != null)
            {
                bankruptcyContext.recipient.Money += required;
                Debug.Log($"✅ Insolvenz aufgelöst! Spieler {bankruptcyContext.bankruptPlayer.PlayerID} zahlt {required}€ an Spieler {bankruptcyContext.recipient.PlayerID} ({bankruptcyContext.reason})");
            }
            else
            {
                Debug.Log($"✅ Insolvenz aufgelöst! Spieler {bankruptcyContext.bankruptPlayer.PlayerID} zahlt {required}€ ({bankruptcyContext.reason})");
            }
            
            uiManager.UpdateMoneyDisplay();

            // Prüfe ob noch Unternehmen übrig sind
            if (bankruptcyContext.bankruptPlayer.companies.Count == 0)
            {
                Debug.Log($"⚠️ Spieler {bankruptcyContext.bankruptPlayer.PlayerID} hat keine Unternehmen mehr, bleibt aber im Spiel.");
            }

            bankruptcyContext = default;
            uiManager.HideBankruptcyAuction();
            EndTurn();
        }
        else
        {
            // Noch nicht genug - weitere Versteigerung nötig
            int stillMissing = required - currentMoney;
            Debug.Log($"⚠️ Noch {stillMissing}€ benötigt. Weitere Versteigerung nötig.");
            
            // Prüfe ob noch Unternehmen vorhanden sind
            if (bankruptcyContext.bankruptPlayer.companies.Count == 0)
            {
                Debug.LogWarning($"❌ Spieler {bankruptcyContext.bankruptPlayer.PlayerID} hat keine Unternehmen mehr, kann aber {stillMissing}€ nicht bezahlen. Zahlung wird nicht durchgeführt.");
                bankruptcyContext = default;
                uiManager.HideBankruptcyAuction();
                EndTurn();
            }
            else
            {
                // Zeige UI erneut für weitere Versteigerung
                uiManager.ShowBankruptcyAuction(bankruptcyContext.bankruptPlayer, stillMissing, bankruptcyContext.reason);
            }
        }
    }

    /// <summary>
    /// Wird aufgerufen wenn Spieler keine weiteren Unternehmen versteigern möchte/kann
    /// </summary>
    public void CancelBankruptcy()
    {
        if (!bankruptcyContext.isActive) return;

        Debug.LogWarning($"⚠️ Versteigerung abgebrochen. Spieler {bankruptcyContext.bankruptPlayer.PlayerID} kann {bankruptcyContext.requiredAmount}€ nicht vollständig bezahlen.");
        bankruptcyContext = default;
        uiManager.HideBankruptcyAuction();
        EndTurn();
    }

    /// <summary>
    /// Gibt alle Unternehmen zurück, die ein Spieler versteigern kann
    /// </summary>
    public List<CompanyField> GetAuctionableCompanies(PlayerData player)
    {
        var result = new List<CompanyField>();
        if (player == null || player.companies == null) return result;

        var allFields = gameInitiator.GetCompanyFields();
        foreach (var fieldIndex in player.companies)
        {
            var field = allFields.FirstOrDefault(f => f.fieldIndex == fieldIndex);
            if (field != null)
            {
                result.Add(field);
            }
        }

        return result;
    }
}