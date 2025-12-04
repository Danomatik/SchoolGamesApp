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
            EndTurn();
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

        if (pending.player.Money < cost)
        {
            Debug.Log("Nicht genug Geld für Kauf/Upgrade.");
            pending = default;
            EndTurn();
            return;
        }

        pending.player.Money -= cost;
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
}