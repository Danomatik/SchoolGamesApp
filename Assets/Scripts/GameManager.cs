using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using System.Linq; // Für .First() benötigt


public class GameManager : MonoBehaviour
{
    // ============================================================
    // 🟩 GAME MANAGER SETTINGS
    // ============================================================
    public GameInitiator gameInitiator;
    public List<PlayerCTRL> players;

    private QuestionManager questionManager;
    private BankCardManager bankCardManager;
    private UIManager uiManager;
    private BoardVisualsManager boardVisuals;
    private DiceManager diceManager;
    private PlayerMovement playerMovement;
    private CameraManager cameraManager;



    [Header("UI")]

    [SerializeField]
    private GameObject moneyDisplay;

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
    private readonly Dictionary<int, int> _skipCounters = new Dictionary<int, int>();



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
    }

    void Start()
    {

        if (boardVisuals != null) boardVisuals.RefreshAll(gameInitiator.GetCompanyFields());

        TestCurrencySystem();
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
            PayRent(current, owner, company, field);
            EndTurn();
        }
    }

    void PayRent(PlayerData payer, PlayerData owner, CompanyConfigData company, CompanyField field)
    {
        int rent = 0;
        switch (field.level)
        {
            case CompanyLevel.Founded:  rent = company.revenueFound;  break;
            case CompanyLevel.Invested: rent = company.revenueInvest; break;
            case CompanyLevel.AG:       rent = company.revenueAG;     break;
        }
        if (rent <= 0) return;

        if (payer.Money >= rent)
        {
            payer.Money -= rent;
            owner.Money += rent;
            uiManager.UpdateMoneyDisplay();
            Debug.Log($"Spieler {payer.PlayerID} zahlt {rent}€ an Spieler {owner.PlayerID}");
        }
        else
        {
            Debug.LogWarning($"Spieler {payer.PlayerID} kann Miete {rent}€ nicht zahlen.");
        }
    }
    private bool IsUpgradeAllowed(CompanyLevel current, CompanyLevel target)
    {
        // Nur stufenweise:
        // None -> Founded -> Invested -> AG
        if (target == CompanyLevel.Founded)   return current == CompanyLevel.None;
        if (target == CompanyLevel.Invested)  return current == CompanyLevel.Founded;
        if (target == CompanyLevel.AG)        return current == CompanyLevel.Invested;
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
            case CompanyLevel.Founded:  cost = pending.company.costFound;  break;
            case CompanyLevel.Invested: cost = pending.company.costInvest; break;
            case CompanyLevel.AG:       cost = pending.company.costAG;     break;
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

        // NEU: Visuals
        if (boardVisuals != null)
            boardVisuals.UpdateFieldVisual(pending.field);

        Debug.Log($"Spieler {pending.player.PlayerID} hat {pending.company.companyName} → {pending.targetLevel} gekauft/aufgerüstet (−{cost}€).");
        pending = default;
        EndTurn();
    }


    // ============================================================
    // 💰 MONEY SYSTEM
    // ============================================================
    public void AddMoney(int amount)
    {
        PlayerData currentPlayer = GetCurrentPlayer();
        if (currentPlayer != null)
        {
            currentPlayer.Money += amount;
            uiManager.UpdateMoneyDisplay();
            Debug.Log($"Spieler {currentPlayer.PlayerID} erhält {amount}€. Neuer Stand: {currentPlayer.Money}€");
        }
    }

    public void AddMoney(int playerID, int amount)
    {
        PlayerData currentPlayer = gameInitiator.CurrentGame.AllPlayers.Find(p => p.PlayerID == playerID);
        if (currentPlayer != null)
        {
            currentPlayer.Money += amount;
            uiManager.UpdateMoneyDisplay();
            Debug.Log($"Spieler {currentPlayer.PlayerID} erhält {amount}€. Neuer Stand: {currentPlayer.Money}€");
        }
    }

    public bool RemoveMoney(int amount)
    {
        PlayerData currentPlayer = GetCurrentPlayer();
        if (currentPlayer != null && currentPlayer.Money >= amount)
        {
            currentPlayer.Money -= amount;
            uiManager.UpdateMoneyDisplay();
            Debug.Log($"Spieler {currentPlayer.PlayerID} bezahlt {amount}€. Neuer Stand: {currentPlayer.Money}€");
            return true;
        }
        Debug.LogWarning($"Spieler {currentPlayer.PlayerID} hat zu wenig Geld, um {amount}€ zu bezahlen!");
        return false;
    }

    public PlayerData GetCurrentPlayer()
    {
        if (gameInitiator.CurrentGame.AllPlayers == null || gameInitiator.CurrentGame.AllPlayers.Count == 0)
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

    public void EndTurn()
    {             
        // zum nächsten Index
        gameInitiator.CurrentGame.CurrentPlayerTurnID++;
        if (gameInitiator.CurrentGame.CurrentPlayerTurnID >= gameInitiator.CurrentGame.AllPlayers.Count)
            gameInitiator.CurrentGame.CurrentPlayerTurnID = 0;

        // Suche den nächsten, der NICHT aussetzt
        int safety = 0;
        while (safety < gameInitiator.CurrentGame.AllPlayers.Count)
        {
            var candidate = GetCurrentPlayer();
            if (candidate == null) break;

            bool mustSkip = _skipCounters.TryGetValue(candidate.PlayerID, out int cnt) && cnt > 0;
            if (!mustSkip)
                break; // dieser Spieler darf ziehen

            // Spieler setzt aus → Zähler dekrementieren, Log ausgeben
            _skipCounters[candidate.PlayerID] = cnt - 1;
            Debug.Log($"Player {candidate.PlayerID} skips this turn (remaining skips: {_skipCounters[candidate.PlayerID]}).");

            // gleich weiter zum nächsten Spieler
            gameInitiator.CurrentGame.CurrentPlayerTurnID++;
            if (gameInitiator.CurrentGame.CurrentPlayerTurnID >= gameInitiator.CurrentGame.AllPlayers.Count)
                gameInitiator.CurrentGame.CurrentPlayerTurnID = 0;

            safety++;
        }

        uiManager.UpdateMoneyDisplay(); 

        var next = GetCurrentPlayer();
        if (next != null)
            Debug.Log($"Zug beendet. Spieler {next.PlayerID} ist jetzt an der Reihe.");
        else
            Debug.LogError("EndTurn: Could not get next player!");

        playerMovement.setIsTurnInProgress(false);  // wichtig
           
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

    // ============================================================
    // 🧪 DEBUG & TESTING
    // ============================================================
    public void TestCurrencySystem()
    {
        Debug.Log("--- STARTE WÄHRUNGSSYSTEM-TEST ---");
        Debug.Log($"Anfangsgeld: {GetCurrentPlayer().Money}€");

        AddMoney(400);
        RemoveMoney(400);
        RemoveMoney(5000);

        Debug.Log($"--- TEST BEENDET --- Finaler Kontostand: {GetCurrentPlayer().Money}€");
    }

    // ============================================================
    // 🏃 MOVEMENT SYSTEM FOR BANK CARDS
    // ============================================================
    public void MovePlayer(int steps)
    {
        PlayerData currentPlayer = GetCurrentPlayer();
        PlayerCTRL activePlayer = players.Find(p => p.PlayerID == currentPlayer.PlayerID);
        
        Debug.Log($"MovePlayer called: Current player is {currentPlayer.PlayerID}, looking for PlayerCTRL with ID {currentPlayer.PlayerID}");
        Debug.Log($"Found PlayerCTRL: {(activePlayer != null ? "Yes" : "No")}");
        
        if (activePlayer != null)
        {
            Debug.Log($"Bank Card Action: Moving player {currentPlayer.PlayerID} {steps} steps forward");
            activePlayer.StartMove(steps);
        }
        else
        {
            Debug.LogError($"Could not find PlayerCTRL for player {currentPlayer.PlayerID}");
        }
    }

    public void MovePlayerToField(int fieldPosition)
    {
        PlayerData currentPlayer = GetCurrentPlayer();
        PlayerCTRL activePlayer = players.Find(p => p.PlayerID == currentPlayer.PlayerID);
        
        if (activePlayer != null)
        {
            int currentPos = activePlayer.currentPos;
            int stepsNeeded = (fieldPosition - currentPos + 40) % 40; // Handle wrap-around
            
            Debug.Log($"Bank Card Action: Moving player {currentPlayer.PlayerID} to field {fieldPosition} ({stepsNeeded} steps)");
            activePlayer.StartMove(stepsNeeded);
        }
    }

    public void SkipTurn()
    {
        var current = GetCurrentPlayer();
        if (current == null)
        {
            Debug.LogError("SkipTurn: no current player!");
            EndTurn();
            return;
        }

        // NÄCHSTEN eigenen Zug aussetzen (nicht den aktuellen)
        ScheduleSkipNextTurn(current.PlayerID, 1);

        Debug.Log($"Bank Card Action: Player {current.PlayerID} will skip their next turn.");
        EndTurn(); // aktueller Zug endet, nächster Spieler kommt dran
    }

    public void ScheduleSkipNextTurn(int playerId, int rounds = 1)
    {
        if (rounds <= 0) return;
        if (_skipCounters.TryGetValue(playerId, out var cnt))
            _skipCounters[playerId] = cnt + rounds;
        else
            _skipCounters[playerId] = rounds;

        Debug.Log($"Player {playerId} will skip the next {rounds} turn(s).");
    }


    public void RollAgain()
    {
        Debug.Log($"Bank Card Action: Player {GetCurrentPlayer().PlayerID} gets to roll again!");

        // Don't end the turn, let the player roll again
        playerMovement.setIsTurnInProgress(false);
        GameObject moveButton = playerMovement.getMoveButton();
        moveButton.SetActive(true);
        uiManager.UpdateMoneyDisplay();

        // The player can now roll again by pressing Space or using the dice system
        Debug.Log("Player can now roll again!");
    }

    public void AddMoneyFromBankCard(int amount)
    {
        PlayerData currentPlayer = GetCurrentPlayer();
        if (currentPlayer != null)
        {
            currentPlayer.Money += amount;
            Debug.Log($"Bank Card Action: Player {currentPlayer.PlayerID} receives {amount}€");
        }
        else
        {
            Debug.LogError("AddMoneyFromBankCard: Current player is null!");
        }
    }
}
