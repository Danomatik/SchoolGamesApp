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
            moneyManager.PayRent(current, owner, company, field);
            EndTurn();
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
}
