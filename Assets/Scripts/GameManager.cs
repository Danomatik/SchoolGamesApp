using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.AI;
using System.Linq; // Für .First() benötigt

public class GameManager : MonoBehaviour
{
    // ============================================================
    // 🟩 GAME MANAGER SETTINGS
    // ============================================================
    [Header("Game Settings")]
    public GameState CurrentGame;
    public List<PlayerCTRL> players;
    public FieldType[] boardLayout = new FieldType[40];
    private bool isTurnInProgress = false;

    [Header("Managers")]
    [SerializeField] private QuestionManager questionManager;
    [SerializeField] private BankCardManager bankCardManager;
    [SerializeField] private UIManager uiManager;
    [SerializeField] private BoardVisualsManager boardVisuals;


    private CompanyConfigCollection companyConfigs;
    public List<CompanyField> companyFields = new List<CompanyField>();


    [Header("Camera")]
    public CinemachineCamera cam;

    public CinemachineBrain camBrain;
    public float defaultLens = 3.55f;

    [Header("UI")]

    [SerializeField]
    private GameObject moveButton;

    [SerializeField]
    private GameObject moneyDisplay;

    // ============================================================
    // 🎲 DICE ROLLER SETTINGS
    // ============================================================
    [Header("Dice Roller Settings")]
    [SerializeField] private Rigidbody dice1;
    [SerializeField] private Rigidbody dice2;

    [SerializeField] private Transform spawnPos1;
    [SerializeField] private Transform spawnPos2;

    [SerializeField] public CinemachineTargetGroup diceTargetGroup;
    [SerializeField] public float diceLensSize;

    [SerializeField] private float throwForce = 8f;
    [SerializeField] private float torqueForce = 10f;

    private bool rolling = false;

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
    void Awake()
    {
        if (!questionManager) questionManager = GetComponent<QuestionManager>();
        if (!bankCardManager) bankCardManager = GetComponent<BankCardManager>();
        if (!uiManager) uiManager = GetComponent<UIManager>();
        if (!boardVisuals) boardVisuals = GetComponent<BoardVisualsManager>();
    }

    void Start()
    {
        LoadCompanyConfigs();

        CurrentGame = new GameState();

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
        PlayerData Player3 = new PlayerData { PlayerID = 3, Money = 2500, BoardPosition = 0 };
        CurrentGame.AllPlayers.Add(Player3);

        // Spieler 4
        PlayerData Player4 = new PlayerData { PlayerID = 4, Money = 2500, BoardPosition = 0 };
        CurrentGame.AllPlayers.Add(Player4);

        // Spieler 5
        PlayerData Player5 = new PlayerData { PlayerID = 5, Money = 2500, BoardPosition = 0 };
        CurrentGame.AllPlayers.Add(Player5);

        // Spieler 6
        PlayerData Player6 = new PlayerData { PlayerID = 6, Money = 2500, BoardPosition = 0 };
        CurrentGame.AllPlayers.Add(Player6);

        Debug.Log("Neues Spiel gestartet!");

        TestCurrencySystem();
    }

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

        if (companyConfigs?.companies == null || companyConfigs.companies.Count == 0)
        {
            Debug.LogError("Kein Unternehmen in JSON gefunden!");
            return;
        }

        // Create a dictionary to quickly lookup companies by ID
        var companyDict = companyConfigs.companies.ToDictionary(c => c.companyID);
        Debug.Log($"Companies in JSON: {string.Join(", ", companyConfigs.companies.Select(c => $"ID:{c.companyID}"))}");

        // Iterate through board layout
        for (int i = 0; i < boardLayout.Length; i++)
        {
            // Only process Company fields (skip Start, Bank, etc.)
            if (boardLayout[i] == FieldType.Company)
            {
                // Check if there's a company for this field index
                if (companyDict.ContainsKey(i))
                {
                    var company = companyDict[i];
                    companyFields.Add(new CompanyField
                    {
                        fieldIndex = i,
                        companyID = company.companyID,
                        ownerID = -1,
                        level = CompanyLevel.None
                    });
                    Debug.Log($"Company '{company.companyName}' (ID: {company.companyID}) assigned to field {i}");
                }
                else
                {
                    // This is a Company field but no company assigned
                    Debug.LogWarning($"Field {i} is Company type but no company found in JSON for ID {i}");
                }
            }
            else
            {
                Debug.Log($"Field {i} is {boardLayout[i]} (not Company)");
            }
        }

        Debug.Log($"Total company fields created: {companyFields.Count}");
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

        // Corner fields (Start)
        boardLayout[0] = FieldType.Start;
        boardLayout[10] = FieldType.Start;
        boardLayout[20] = FieldType.Start;
        boardLayout[30] = FieldType.Start;
        
        // Bank fields (fields without companies in JSON): 5, 7, 13, 23, 27, 37
        boardLayout[5] = FieldType.Bank;
        boardLayout[7] = FieldType.Bank;
        boardLayout[13] = FieldType.Bank;
        boardLayout[23] = FieldType.Bank;
        boardLayout[27] = FieldType.Bank;
        boardLayout[37] = FieldType.Bank;
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
        PlayerData currentPlayer = CurrentGame.AllPlayers.Find(p => p.PlayerID == playerID);
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
        if (CurrentGame.AllPlayers == null || CurrentGame.AllPlayers.Count == 0)
        {
            Debug.LogError("GetCurrentPlayer: AllPlayers is null or empty!");
            return null;
        }
        
        if (CurrentGame.CurrentPlayerTurnID < 0 || CurrentGame.CurrentPlayerTurnID >= CurrentGame.AllPlayers.Count)
        {
            Debug.LogError($"GetCurrentPlayer: currentPlayerIndex {CurrentGame.CurrentPlayerTurnID} is out of bounds! AllPlayers count: {CurrentGame.AllPlayers.Count}");
            return null;
        }
        
        return CurrentGame.AllPlayers[CurrentGame.CurrentPlayerTurnID];
    }

    public void EndTurn()
    {
        // zum nächsten Index
        CurrentGame.CurrentPlayerTurnID++;
        if (CurrentGame.CurrentPlayerTurnID >= CurrentGame.AllPlayers.Count)
            CurrentGame.CurrentPlayerTurnID = 0;

        // Suche den nächsten, der NICHT aussetzt
        int safety = 0;
        while (safety < CurrentGame.AllPlayers.Count)
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
            CurrentGame.CurrentPlayerTurnID++;
            if (CurrentGame.CurrentPlayerTurnID >= CurrentGame.AllPlayers.Count)
                CurrentGame.CurrentPlayerTurnID = 0;

            safety++;
        }

        uiManager.UpdateMoneyDisplay();
        UpdateAgentPriorities();

        var next = GetCurrentPlayer();
        if (next != null)
            Debug.Log($"Zug beendet. Spieler {next.PlayerID} ist jetzt an der Reihe.");
        else
            Debug.LogError("EndTurn: Could not get next player!");

        isTurnInProgress = false;  // wichtig
        
        // Kamera auf nächsten Spieler setzen
        PlayerCTRL activePlayer = players.Find(p => p.PlayerID == next.PlayerID);
        if (activePlayer != null)
        {
            Transform playerChild = activePlayer.transform.childCount > 0
                ? activePlayer.transform.GetChild(0)
                : activePlayer.transform;

            cam.Lens.OrthographicSize = defaultLens;
            cam.Follow = playerChild;
        }

        if (camBrain.IsBlending && camBrain.ActiveBlend != null)
            {
                moveButton.SetActive(false);
                moneyDisplay.SetActive(false);
            }
            else
            {
                uiManager.UpdateMoneyDisplay();
                moveButton.SetActive(true);
                moneyDisplay.SetActive(true);
            }
    }

    // ============================================================
    // 👣 PLAYER MOVEMENT & CAMERA
    // ============================================================
    public void TakeTurn()
    {
        if (isTurnInProgress) return;
        isTurnInProgress = true;

        UpdateAgentPriorities();

        int diceRoll = GetAddedValue();
        Debug.Log($"Player {GetCurrentPlayer().PlayerID} rolled a {diceRoll}!");

        PlayerCTRL activePlayer = players.Find(p => p.PlayerID == GetCurrentPlayer().PlayerID);
        if (activePlayer != null)
        {
            Transform playerChild = activePlayer.transform.childCount > 0
                ? activePlayer.transform.GetChild(0)
                : activePlayer.transform;

            cam.Lens.OrthographicSize = defaultLens;
            cam.Follow = playerChild;
            if (camBrain.IsBlending && camBrain.ActiveBlend != null)
            {
                moveButton.SetActive(false);
                moneyDisplay.SetActive(false);
            }
            else
            {
                uiManager.UpdateMoneyDisplay();
                moveButton.SetActive(true);
                moneyDisplay.SetActive(true);
            }

            activePlayer.StartMove(diceRoll);
        }
    }

    public void PlayerFinishedMoving(int finalPosition)
    {
        // Check field type from board layout
        if (finalPosition < boardLayout.Length)
        {
            FieldType fieldType = boardLayout[finalPosition];
            
            switch (fieldType)
            {
                case FieldType.Start:
                    Debug.Log("Player landed on Start field!");
                    break;

                case FieldType.Company:
                {
                    Debug.Log($"Player landed on Company field! Field {finalPosition}");
                    Debug.Log($"BoardLayout[{finalPosition}] = {boardLayout[finalPosition]}");
                    Debug.Log($"Total companyFields: {companyFields.Count}");
                    var field = companyFields.FirstOrDefault(f => f.fieldIndex == finalPosition);
                    if (field == null)
                    {
                        Debug.LogError($"Kein CompanyField für Position {finalPosition} gefunden.");
                        Debug.Log($"Company fields available: {string.Join(", ", companyFields.Select(f => f.fieldIndex))}");
                        EndTurn();
                        return;
                    }
                    HandleCompanyField(field);
                    return; // wichtig: kein EndTurn() hier; Flow entscheidet
                }

                    
                case FieldType.Bank:
                {
                    Debug.Log("Player landed on Bank field!");
                    var currentPlayer = GetCurrentPlayer();
                    if (currentPlayer != null && bankCardManager != null)
                    {
                        bankCardManager.ShowRandomBankCard();

                        // WICHTIG: Wir warten jetzt auf den Klick im Popup (BankCardManager beendet/fortsetzt den Turn)
                        return;
                    }
                    else
                    {
                        Debug.LogError("Bank field: Current player or BankCardManager is null!");
                    }
                    break;
                }

            }
        }

        EndTurn();
        isTurnInProgress = false;
    }

    public void UpdateAgentPriorities()
    {
        PlayerData currentPlayer = GetCurrentPlayer();
        if (currentPlayer == null)
        {
            Debug.LogError("UpdateAgentPriorities: Current player is null!");
            return;
        }
        
        foreach (PlayerCTRL player in players)
        {
            NavMeshAgent agent = player.GetComponent<NavMeshAgent>();
            if (agent != null)
                agent.avoidancePriority = (player.PlayerID == currentPlayer.PlayerID) ? 50 : 51;
        }
    }

    // ============================================================
    // 🎲 DICE ROLLING SYSTEM
    // ============================================================
    public void RollDice()
    {
        if (rolling) return;
        StartCoroutine(RollRoutine());
    }

    private IEnumerator RollRoutine()
    {   
        moveButton.SetActive(false);
        rolling = true;

        // Reset dice positions
        ResetDice(dice1, spawnPos1);
        ResetDice(dice2, spawnPos2);

        // Apply force & torque
        ThrowDice(dice1);
        ThrowDice(dice2);

        // Focus camera on dice
        cam.Follow = diceTargetGroup.transform;
        cam.Lens.OrthographicSize = diceLensSize;


        // Wait until both dice stop moving
        yield return new WaitUntil(() => dice1.IsSleeping() && dice2.IsSleeping());
        yield return new WaitForSeconds(1f);

        int rollValue = GetAddedValue();
        // Debug.Log($"Dice rolled: {rollValue}");

        // Return camera to player

        TakeTurn();
        moveButton.SetActive(false);
        rolling = false;
    }

    void ResetDice(Rigidbody rb, Transform startPos)
    {
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.transform.position = startPos.position;
        rb.transform.rotation = Random.rotation;
    }

    void ThrowDice(Rigidbody rb)
    {
        rb.AddForce(Vector3.down * throwForce, ForceMode.Impulse);
        rb.AddTorque(Random.insideUnitSphere * torqueForce, ForceMode.Impulse);
    }

    int GetDiceValue(Rigidbody dice)
    {
        Vector3[] directions = {
            dice.transform.up,
            -dice.transform.up,
            dice.transform.right,
            -dice.transform.right,
            dice.transform.forward,
            -dice.transform.forward
        };

        int[] faceValues = { 1, 6, 3, 4, 2, 5 };

        float maxDot = -1f;
        int bestIndex = 0;

        for (int i = 0; i < directions.Length; i++)
        {
            float dot = Vector3.Dot(Vector3.up, directions[i]);
            if (dot > maxDot)
            {
                maxDot = dot;
                bestIndex = i;
            }
        }

        return faceValues[bestIndex];
    }

    public int GetAddedValue()
    {
        int val1 = GetDiceValue(dice1);
        int val2 = GetDiceValue(dice2);
        return val1 + val2;
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
        isTurnInProgress = false;
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
