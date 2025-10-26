using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.AI;

public class GameManager : MonoBehaviour
{
    public GameState CurrentGame;
    public List<PlayerCTRL> players;
    private bool isTurnInProgress = false;
    public FieldType[] boardLayout = new FieldType[40];

    [SerializeField]
    private QuestionManager questionManager;

    [SerializeField]
    private DiceRoller diceRoller;

    public CinemachineCamera cam;

    void Start()
    {
        CurrentGame = new GameState();

<<<<<<< Updated upstream
        // SPieler 1
        PlayerData humanPlayer = new PlayerData { PlayerID = 1, Money = 2500, BoardPosition = 0 };
        CurrentGame.AllPlayers.Add(humanPlayer);

        //Spieler 2
        PlayerData botPlayer1 = new PlayerData { PlayerID = 2, Money = 2500, BoardPosition = 0 };
        CurrentGame.AllPlayers.Add(botPlayer1);

        Debug.Log("Neues Spiel mit 2 Spielern gestartet!");
        Debug.Log($"Neues Spiel gestartet! Spieler 1 hat {humanPlayer.Money} €");
=======
        // Initialize board layout - all fields are Company by default
        InitializeBoardLayout();     // <-- ZUERST das Layout setzen
        InitializeCompanyFields();   // <-- DANN die companyFields daraus bauen
        
        if (boardVisuals != null) boardVisuals.RefreshAll(companyFields);

        // Spieler 1
        PlayerData Player1 = new PlayerData { PlayerID = 1, Money = 2500, BoardPosition = 0 };
        CurrentGame.AllPlayers.Add(Player1);

        // Spieler 2
        PlayerData Player2 = new PlayerData { PlayerID = 2, Money = 2500, BoardPosition = 0 };
        CurrentGame.AllPlayers.Add(Player2);

        // Spieler 3
        PlayerData Player3 = new PlayerData { PlayerID = 1, Money = 2500, BoardPosition = 0 };
        CurrentGame.AllPlayers.Add(Player3);

        // Spieler 4
        PlayerData Player4 = new PlayerData { PlayerID = 2, Money = 2500, BoardPosition = 0 };
        CurrentGame.AllPlayers.Add(Player4);

        // Spieler 5
        PlayerData Player5 = new PlayerData { PlayerID = 1, Money = 2500, BoardPosition = 0 };
        CurrentGame.AllPlayers.Add(Player5);

        // Spieler 6
        PlayerData Player6 = new PlayerData { PlayerID = 2, Money = 2500, BoardPosition = 0 };
        CurrentGame.AllPlayers.Add(Player6);

        Debug.Log("Neues Spiel gestartet!");
>>>>>>> Stashed changes

        TestCurrencySystem();
    }

<<<<<<< Updated upstream
=======

    void LoadCompanyConfigs()
    {
        TextAsset jsonFile = Resources.Load<TextAsset>("Data/Schoolgames_Companies");
        if (jsonFile == null)
        {
            Debug.LogError("Schoolgames_Companies.json nicht in Assets/Resources/Data/ gefunden!");
            companyConfigs = new CompanyConfigCollection { companies = new List<CompanyConfigData>() };
            return;
        }
        companyConfigs = JsonUtility.FromJson<CompanyConfigCollection>(jsonFile.text);
        if (companyConfigs?.companies == null)
            companyConfigs = new CompanyConfigCollection { companies = new List<CompanyConfigData>() };

        Debug.Log($"Companies geladen: {companyConfigs.companies.Count}");
    }

    void InitializeCompanyFields()
    {
        companyFields.Clear();

        var takeda = companyConfigs?.companies?.FirstOrDefault();
        if (takeda == null)
        {
            Debug.LogError("Kein Unternehmen in JSON gefunden!");
            return;
        }

        for (int i = 0; i < boardLayout.Length; i++)
        {
            if (boardLayout[i] == FieldType.Company)
            {
                companyFields.Add(new CompanyField
                {
                    fieldIndex = i,
                    companyID = takeda.companyID,
                    ownerID = -1,
                    level = CompanyLevel.None
                });
            }
        }
    }

    CompanyConfigData GetCompanyConfig(int id)
    {
        return companyConfigs?.companies?.FirstOrDefault(c => c.companyID == id)
            ?? companyConfigs?.companies?.FirstOrDefault();
    }

    void HandleCompanyField(CompanyField field)
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
            var owner = CurrentGame.AllPlayers.FirstOrDefault(p => p.PlayerID == field.ownerID);
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



    private void InitializeBoardLayout()
    {
        // Set all fields to Bank by default
        for (int i = 0; i < boardLayout.Length; i++)
        {
            boardLayout[i] = FieldType.Company;
        }

        // Set specific fields to other types
        boardLayout[0] = FieldType.Start; // Starting field
        // You can add more specific fields here if needed
        // boardLayout[10] = FieldType.Company; // Example company field
    }

    // ============================================================
    // 💰 MONEY SYSTEM
    // ============================================================
>>>>>>> Stashed changes
    public void AddMoney(int amount)
    {
        PlayerData currentPlayer = GetCurrentPlayer();
        if (currentPlayer != null)
        {
            currentPlayer.Money += amount;
            Debug.Log($"Spieler {currentPlayer.PlayerID} erhält {amount}€. Neuer Stand: {currentPlayer.Money}€");
        }
    }

    public void AddMoney(int playerID, int amount)
    {
        PlayerData currentPlayer = CurrentGame.AllPlayers.Find(p => p.PlayerID == playerID);
        if (currentPlayer != null)
        {
            currentPlayer.Money += amount;
            Debug.Log($"Spieler {currentPlayer.PlayerID} erhält {amount}€. Neuer Stand: {currentPlayer.Money}€");
        }
    }

    public bool RemoveMoney(int amount)
    {
        PlayerData currentPlayer = GetCurrentPlayer();
        if (currentPlayer != null && currentPlayer.Money >= amount)
        {
            currentPlayer.Money -= amount;
            Debug.Log($"Spieler {currentPlayer.PlayerID} bezahlt {amount}€. Neuer Stand: {currentPlayer.Money}€");
            return true;
        }
        Debug.LogWarning($"Spieler {currentPlayer.PlayerID} hat zu wenig Geld, um {amount}€ zu bezahlen!");
        return false;
    }

    public PlayerData GetCurrentPlayer()
    {
        return CurrentGame.AllPlayers[CurrentGame.CurrentPlayerTurnID];
    }

    public void EndTurn()
    {
        CurrentGame.CurrentPlayerTurnID++;

        if (CurrentGame.CurrentPlayerTurnID >= CurrentGame.AllPlayers.Count)
        {
            CurrentGame.CurrentPlayerTurnID = 0;
        }

        UpdateAgentPriorities();
        Debug.Log($"Zug beendet. Spieler {GetCurrentPlayer().PlayerID} ist jetzt an der Reihe.");

    }

    // ---------------------------------------------------------------------------------------------
    public void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            //TakeTurn();
        }
    }

    public void TakeTurn()
    {
        if (isTurnInProgress) return;
        isTurnInProgress = true;

        UpdateAgentPriorities();

        int diceRoll = diceRoller.GetAddedValue();
        Debug.Log($"Player {GetCurrentPlayer().PlayerID} rolled a {diceRoll}!");

        //Find the Correct player in the scene
        PlayerCTRL activePlayer = players.Find(p => p.PlayerID == GetCurrentPlayer().PlayerID);

        if (activePlayer != null)
        {
            // Get the first child transform of the player
            if (activePlayer.transform.childCount > 0)
            {
                Transform playerChild = activePlayer.transform.GetChild(0);
                cam.Follow = playerChild;
            }
            else
            {
                // Fallback to the player's own transform if no children exist
                cam.Follow = activePlayer.transform;
            }

            activePlayer.StartMove(diceRoll);
        }
    }

    public void PlayerFinishedMoving(int finalPosition)
    {
        // Check if there's a QuizField at this position
        if (questionManager != null)
        {
            questionManager.CheckForQuizField(finalPosition);
        }

        EndTurn();
        isTurnInProgress = false;
    }

    // ---------------------------------------------------------------------------------------------
    public void UpdateAgentPriorities()
    {
        PlayerData currentPlayer = GetCurrentPlayer();

        foreach (PlayerCTRL player in players)
        {
            NavMeshAgent agent = player.GetComponent<NavMeshAgent>();
            if (agent != null)
            {
                if (player.PlayerID == currentPlayer.PlayerID)
                {
                    agent.avoidancePriority = 50;
                }
                else
                {
                    agent.avoidancePriority = 51;
                }
            }
        }
    }


    public void TestCurrencySystem()
    {
        Debug.Log("--- STARTE WÄHRUNGSSYSTEM-TEST ---");
        Debug.Log($"Anfangsgeld: {GetCurrentPlayer().Money}€");

        // Test 1: Geld hinzufügen
        AddMoney(400);

        // Test 2: Erfolgreich Geld abziehen
        RemoveMoney(400);

        // Test 3: Fehlgeschlagenes Abziehen
        RemoveMoney(5000);

        Debug.Log($"--- TEST BEENDET --- Finaler Kontostand: {GetCurrentPlayer().Money}€");
    }
}