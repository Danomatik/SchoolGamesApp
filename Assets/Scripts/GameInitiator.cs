using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine.AI;
using System.Linq; 

public class GameInitiator : MonoBehaviour
{
    public GameState CurrentGame;

    public CompanyConfigCollection companyConfigs;

    public FieldType[] boardLayout = new FieldType[40];

    private List<CompanyField> companyFields = new List<CompanyField>();


// ✅ Add this
    [Header("Initiative / Turn Order")]
    [Tooltip("If enabled, skips the roll-off and uses default order 1,2,3,4,5,6.")]
    [SerializeField] private bool useDefaultOrder = false;

    // ------------------------------------------------------------

    void Awake()
    {
        LoadCompanyConfigs();
        CurrentGame = new GameState();

        InitializeBoardLayout();
        InitializeCompanyFields();

        // Players (1..6 in default order)
        PlayerData Player1 = new PlayerData { PlayerID = 1, Money = 2500, BoardPosition = 0, PlayerName = "Hanx", hasToSkip = false, companies = new List<int>() };
        CurrentGame.AllPlayers.Add(Player1);
        PlayerData Player2 = new PlayerData { PlayerID = 2, Money = 2500, BoardPosition = 0, PlayerName = "Momo", hasToSkip = false, companies = new List<int>() };
        CurrentGame.AllPlayers.Add(Player2);
        PlayerData Player3 = new PlayerData { PlayerID = 3, Money = 2500, BoardPosition = 0, PlayerName = "Simoan", hasToSkip = false, companies = new List<int>() };
        CurrentGame.AllPlayers.Add(Player3);
        PlayerData Player4 = new PlayerData { PlayerID = 4, Money = 2500, BoardPosition = 0, PlayerName = "Chidi", hasToSkip = false, companies = new List<int>() };
        CurrentGame.AllPlayers.Add(Player4);
        PlayerData Player5 = new PlayerData { PlayerID = 5, Money = 2500, BoardPosition = 0, PlayerName = "Dan", hasToSkip = false, companies = new List<int>() };
        CurrentGame.AllPlayers.Add(Player5);
        PlayerData Player6 = new PlayerData { PlayerID = 6, Money = 2500, BoardPosition = 0, PlayerName = "Mußbacher", hasToSkip = false, companies = new List<int>() };
        CurrentGame.AllPlayers.Add(Player6);

        Debug.Log("Neues Spiel gestartet!");

        var gm = GetComponent<GameManager>();
        if (gm != null)
        {
            if (useDefaultOrder)
            {
                ApplyDefaultStartingOrder(gm);
            }
            else
            {
                StartCoroutine(DetermineStartingOrder(gm));
            }
        }
    }

    // ✅ New helper: apply default order 1..6 and start game immediately
    private void ApplyDefaultStartingOrder(GameManager gm)
    {
        // Sort strictly by PlayerID to guarantee 1..6 order
        CurrentGame.AllPlayers = CurrentGame.AllPlayers
            .OrderBy(p => p.PlayerID)
            .ToList();

        CurrentGame.CurrentPlayerTurnID = 0;
        initiativeDone = true;
        gm.InitiativeInProgress = false;

        // Point camera to Player 1 (like in your roll coroutine)
        var currentPlayer = CurrentGame.AllPlayers[0];
        var activeCtrl = gm.players.Find(p => p.PlayerID == currentPlayer.PlayerID);
        if (activeCtrl != null)
        {
            Transform playerChild = activeCtrl.transform.childCount > 0
                ? activeCtrl.transform.GetChild(0)
                : activeCtrl.transform;
            gm.cameraManager.cam.Lens.OrthographicSize = gm.cameraManager.defaultLens;
            gm.cameraManager.cam.Follow = playerChild;
        }

        // Re-enable the move button
        if (gm.diceManager != null && gm.diceManager.moveButton != null)
            gm.diceManager.moveButton.SetActive(true);

        Debug.Log($"Initiative skipped. Default order applied: {string.Join(", ", CurrentGame.AllPlayers.Select(p => p.PlayerID))}. Start: Player {CurrentGame.AllPlayers[0].PlayerID}");
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

    // ============================================================
    // 🎲 INITIATIVE SEQUENCE (RUNS ONCE AT GAME START)
    // ============================================================
    private bool initiativeDone = false;
   private IEnumerator DetermineStartingOrder(GameManager gm)
    {
        // ✅ Early-out if inspector checkbox is turned on (safety if called accidentally)
        if (useDefaultOrder)
        {
            ApplyDefaultStartingOrder(gm);
            yield break;
        }

        if (initiativeDone) yield break;

        gm.InitiativeInProgress = true;
        Debug.Log("Initiative (Initiator): Starting initial roll-off phase...");
        if (gm.diceManager != null && gm.diceManager.moveButton != null)
            gm.diceManager.moveButton.SetActive(false);

        var playersById = CurrentGame.AllPlayers.ToDictionary(p => p.PlayerID);
        var rolls = new List<(int playerId, int roll)>();

        for (int i = 0; i < CurrentGame.AllPlayers.Count; i++)
        {
            CurrentGame.CurrentPlayerTurnID = i;

            var currentPlayer = gm.GetCurrentPlayer();
            var activeCtrl = gm.players.Find(p => p.PlayerID == currentPlayer.PlayerID);
            if (activeCtrl != null)
            {
                Transform playerChild = activeCtrl.transform.childCount > 0
                    ? activeCtrl.transform.GetChild(0)
                    : activeCtrl.transform;
                gm.cameraManager.cam.Lens.OrthographicSize = gm.cameraManager.defaultLens;
                gm.cameraManager.cam.Follow = playerChild;
            }

            int result = 0;
            yield return StartCoroutine(gm.diceManager.RollForInitiative(val => result = val));
            rolls.Add((currentPlayer.PlayerID, result));
            Debug.Log($"Initiative (Initiator): Player {currentPlayer.PlayerID} rolled {result}");

            if (gm.uiManager != null)
            {
                var label = string.IsNullOrEmpty(currentPlayer.PlayerName) ? $"Spieler {currentPlayer.PlayerID}" : currentPlayer.PlayerName;
                gm.uiManager.ShowInitiativeRoll(label, result);
                yield return new WaitForSeconds(1.2f);
                gm.uiManager.HideInitiative();
            }
        }

        var ordered = rolls.OrderByDescending(r => r.roll).ToList();
        var reordered = new List<PlayerData>();
        foreach (var entry in ordered)
        {
            if (playersById.TryGetValue(entry.playerId, out var pdata))
                reordered.Add(pdata);
        }
        CurrentGame.AllPlayers = reordered;

        CurrentGame.CurrentPlayerTurnID = 0;
        initiativeDone = true;
        gm.InitiativeInProgress = false;
        Debug.Log($"Initiative (Initiator): Completed. Order: {string.Join(", ", CurrentGame.AllPlayers.Select(p=>p.PlayerID))}. Start: Player {CurrentGame.AllPlayers[0].PlayerID}");

        if (gm.diceManager != null && gm.diceManager.moveButton != null)
            gm.diceManager.moveButton.SetActive(true);
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

        // Bank/Action fields (fields without companies in JSON): 5, 7, 13, 23, 27, 37
        boardLayout[5] = FieldType.Action;
        boardLayout[13] = FieldType.Action;
        boardLayout[27] = FieldType.Action;
        boardLayout[37] = FieldType.Action;

        boardLayout[7] = FieldType.Bank;
        boardLayout[23] = FieldType.Bank;
    }
    
    
     public List<CompanyField> GetCompanyFields()
    {
        return companyFields;
    }
}