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
    private GameSaveManager saveManager;

    void Awake()
    {
        LoadCompanyConfigs();
        CurrentGame = new GameState();
        saveManager = FindFirstObjectByType<GameSaveManager>();
        if (saveManager == null)
        {
            GameObject saveManagerObj = new GameObject("GameSaveManager");
            saveManager = saveManagerObj.AddComponent<GameSaveManager>();
        }

        InitializeBoardLayout();
        InitializeCompanyFields();

        // Check if we should load saved game (set by LoadSavedGame script)
        int flagValue = PlayerPrefs.GetInt("LoadSavedGame", 0);
        bool shouldLoadSaved = flagValue == 1;
        GameSaveData savedData = null;
        
        Debug.Log($"[GameInitiator] LoadSavedGame Flag Wert: {flagValue}, shouldLoadSaved: {shouldLoadSaved}");
        
        if (shouldLoadSaved)
        {
            Debug.Log("[GameInitiator] 🔄 Versuche gespeichertes Spiel zu laden...");
            // Try to load saved game
            savedData = saveManager.LoadGame();
            if (savedData != null)
            {
                Debug.Log("[GameInitiator] ✅ Gespeichertes Spiel gefunden! Lade...");
                LoadSavedGame(savedData);
                // Reset the flag
                PlayerPrefs.SetInt("LoadSavedGame", 0);
                PlayerPrefs.Save();
                Debug.Log("[GameInitiator] ✅ Flag zurückgesetzt.");
            }
            else
            {
                Debug.LogWarning("⚠️ Sollte gespeichertes Spiel laden, aber keine Save-Datei gefunden. Starte neues Spiel.");
                StartNewGame();
                // Reset the flag
                PlayerPrefs.SetInt("LoadSavedGame", 0);
                PlayerPrefs.Save();
            }
        }
        else
        {
            Debug.Log("[GameInitiator] 🆕 Starte neues Spiel (Flag nicht gesetzt).");
            // Start new game (normal "Spiel starten" button)
            StartNewGame();
        }

        var gm = GetComponent<GameManager>();
        if (gm != null)
        {
            // If loaded from save, skip initiative
            if (savedData != null)
            {
                initiativeDone = true;
                gm.InitiativeInProgress = false;
                // Set camera to current player
                var currentPlayer = CurrentGame.AllPlayers[CurrentGame.CurrentPlayerTurnID];
                var activeCtrl = gm.players.Find(p => p.PlayerID == currentPlayer.PlayerID);
                if (activeCtrl != null)
                {
                    Transform playerChild = activeCtrl.transform.childCount > 0
                        ? activeCtrl.transform.GetChild(0)
                        : activeCtrl.transform;
                    gm.cameraManager.cam.Lens.OrthographicSize = gm.cameraManager.defaultLens;
                    gm.cameraManager.cam.Follow = playerChild;
                }
                if (gm.diceManager != null && gm.diceManager.moveButton != null)
                    gm.diceManager.moveButton.SetActive(true);
            }
            else if (useDefaultOrder)
            {
                ApplyDefaultStartingOrder(gm);
            }
            else
            {
                StartCoroutine(DetermineStartingOrder(gm));
            }
        }
    }

    private void StartNewGame()
    {
        CurrentGame.AllPlayers.Clear();

        // Versuche Spielerdaten aus PlayerPrefs zu laden (von Demo 3 Scene)
        PlayerSetupManager setupManager = FindFirstObjectByType<PlayerSetupManager>();
        if (setupManager == null)
        {
            // Erstelle temporären Manager zum Laden der Daten
            GameObject tempObj = new GameObject("TempPlayerSetupManager");
            setupManager = tempObj.AddComponent<PlayerSetupManager>();
        }

        int playerCount = setupManager.GetPlayerCount();
        Debug.Log($"[GameInitiator] Lade Spielerdaten: {playerCount} Spieler");

        // Erstelle Spieler basierend auf gespeicherten Daten
        for (int i = 1; i <= playerCount; i++)
        {
            string playerName = setupManager.GetPlayerName(i);
            PlayerData player = new PlayerData
            {
                PlayerID = i,
                Money = 2500,
                BoardPosition = 0,
                PlayerName = playerName,
                hasToSkip = false,
                companies = new List<int>()
            };
            CurrentGame.AllPlayers.Add(player);
            Debug.Log($"[GameInitiator] Spieler {i} erstellt: {playerName}");
        }

        // Falls keine Spielerdaten vorhanden, verwende Standard-Spieler (Fallback)
        if (CurrentGame.AllPlayers.Count == 0)
        {
            Debug.LogWarning("[GameInitiator] Keine Spielerdaten gefunden! Verwende Standard-Spieler.");
            CreateDefaultPlayers();
        }

        Debug.Log($"✅ Neues Spiel gestartet mit {CurrentGame.AllPlayers.Count} Spielern!");
        
        // Deaktiviere nicht verwendete PlayerCTRL GameObjects
        StartCoroutine(DeactivateUnusedPlayers());

        // Aktualisiere Spielernamen
        StartCoroutine(UpdatePlayerNames());
    }

    /// <summary>
    /// Aktualisiert die Namen der PlayerCTRL GameObjects basierend auf PlayerData
    /// </summary>
    private IEnumerator UpdatePlayerNames()
    {
        // Warte einen Frame, damit GameManager initialisiert ist
        yield return null;
        
        GameManager gameManager = GetComponent<GameManager>();
        if (gameManager == null || gameManager.players == null)
        {
            Debug.LogWarning("[GameInitiator] GameManager oder players nicht gefunden. Kann Spielernamen nicht aktualisieren.");
            yield break;
        }
        
        // Aktualisiere jeden PlayerCTRL mit dem Namen aus PlayerData
        foreach (var playerData in CurrentGame.AllPlayers)
        {
            var playerCTRL = gameManager.players.Find(p => p.PlayerID == playerData.PlayerID);
            if (playerCTRL != null)
            {
                // Setze den Namen auf dem PlayerCTRL GameObject
                playerCTRL.gameObject.name = $"Player_{playerData.PlayerID}_{playerData.PlayerName}";
                
                // Falls PlayerCTRL ein playerName Feld hat, setze es hier
                // playerCTRL.playerName = playerData.PlayerName; // ⚠️ Uncomment wenn PlayerCTRL ein playerName Feld hat
                
                Debug.Log($"[GameInitiator] PlayerCTRL für Spieler {playerData.PlayerID} Name gesetzt: {playerData.PlayerName}");
            }
        }
        
        Debug.Log($"[GameInitiator] ✅ Spielernamen aktualisiert.");
    }

    /// <summary>
    /// Deaktiviert PlayerCTRL GameObjects für Spieler, die nicht im Spiel sind
    /// </summary>
    private IEnumerator DeactivateUnusedPlayers()
    {
        // Warte einen Frame, damit GameManager initialisiert ist
        yield return null;
        
        GameManager gameManager = GetComponent<GameManager>();
        if (gameManager == null || gameManager.players == null)
        {
            Debug.LogWarning("[GameInitiator] GameManager oder players nicht gefunden. Kann nicht verwendete Spieler nicht deaktivieren.");
            yield break;
        }
        
        // Erstelle eine Liste der aktiven PlayerIDs
        HashSet<int> activePlayerIDs = new HashSet<int>();
        foreach (var playerData in CurrentGame.AllPlayers)
        {
            activePlayerIDs.Add(playerData.PlayerID);
        }
        
        // Deaktiviere alle PlayerCTRL GameObjects, die nicht in der aktiven Liste sind
        foreach (var playerCTRL in gameManager.players)
        {
            if (playerCTRL != null)
            {
                if (!activePlayerIDs.Contains(playerCTRL.PlayerID))
                {
                    playerCTRL.gameObject.SetActive(false);
                    Debug.Log($"[GameInitiator] PlayerCTRL für Spieler {playerCTRL.PlayerID} deaktiviert (nicht im Spiel).");
                }
                else
                {
                    playerCTRL.gameObject.SetActive(true);
                    Debug.Log($"[GameInitiator] PlayerCTRL für Spieler {playerCTRL.PlayerID} aktiviert.");
                }
            }
        }
        
        Debug.Log($"[GameInitiator] ✅ Nicht verwendete Spieler deaktiviert. Aktive Spieler: {CurrentGame.AllPlayers.Count}");
    }

    /// <summary>
    /// Erstellt Standard-Spieler als Fallback
    /// </summary>
    private void CreateDefaultPlayers()
    {
        string[] defaultNames = { "Hanx", "Momo", "Simoan", "Chidi", "Dan", "Mußbacher" };
        
        for (int i = 0; i < 6; i++)
        {
            PlayerData player = new PlayerData
            {
                PlayerID = i + 1,
                Money = 2500,
                BoardPosition = 0,
                PlayerName = defaultNames[i],
                hasToSkip = false,
                companies = new List<int>()
            };
            CurrentGame.AllPlayers.Add(player);
        }
    }

    private void LoadSavedGame(GameSaveData saveData)
    {
        Debug.Log("🔄 Lade gespeichertes Spiel...");

        // Load players
        CurrentGame.AllPlayers.Clear();
        foreach (var playerSave in saveData.players)
        {
            CurrentGame.AllPlayers.Add(new PlayerData
            {
                PlayerID = playerSave.PlayerID,
                Money = playerSave.Money,
                BoardPosition = playerSave.BoardPosition,
                PlayerName = playerSave.PlayerName,
                hasToSkip = playerSave.hasToSkip,
                companies = new List<int>(playerSave.companies)
            });
        }

        // Load current turn
        CurrentGame.CurrentPlayerTurnID = saveData.currentPlayerTurnID;

        // Load company fields
        companyFields.Clear();
        companyFields.AddRange(saveData.companyFields);

        Debug.Log($"✅ Spiel geladen! Aktueller Spieler: {CurrentGame.AllPlayers[CurrentGame.CurrentPlayerTurnID].PlayerName}");
        Debug.Log($"   Gespeichert am: {saveData.saveTimestamp}");

        // Deaktiviere nicht verwendete PlayerCTRL GameObjects
        StartCoroutine(DeactivateUnusedPlayers());
        
        // Update visual player positions (PlayerCTRL objects)
        StartCoroutine(UpdatePlayerPositionsAfterLoad());

         
        // Aktualisiere Spielernamen
        StartCoroutine(UpdatePlayerNames());
{
    // ... existing code ...

    // Deaktiviere nicht verwendete PlayerCTRL GameObjects
    StartCoroutine(DeactivateUnusedPlayers());
    
    // Update visual player positions (PlayerCTRL objects)
    StartCoroutine(UpdatePlayerPositionsAfterLoad());
    
    // ✅ NEU: Aktualisiere Spielernamen
    StartCoroutine(UpdatePlayerNames());
}
    }

    /// <summary>
    /// Updates the visual positions of PlayerCTRL objects after loading saved game
    /// </summary>
    private IEnumerator UpdatePlayerPositionsAfterLoad()
    {
        // Wait one frame to ensure GameManager is initialized
        yield return null;

        GameManager gameManager = GetComponent<GameManager>();
        if (gameManager == null || gameManager.players == null)
        {
            Debug.LogWarning("[GameInitiator] GameManager oder players nicht gefunden. Kann Spieler-Positionen nicht aktualisieren.");
            yield break;
        }

        // Update each player's visual position
        foreach (var playerData in CurrentGame.AllPlayers)
        {
            var playerCTRL = gameManager.players.Find(p => p.PlayerID == playerData.PlayerID);
            if (playerCTRL != null && playerCTRL.route != null && playerCTRL.route.childNodeList != null)
            {
                // Set currentPos to match saved BoardPosition
                playerCTRL.currentPos = playerData.BoardPosition;

                // Move player visually to the correct position
                if (playerCTRL.currentPos < playerCTRL.route.childNodeList.Count)
                {
                    Vector3 targetPosition = playerCTRL.route.childNodeList[playerCTRL.currentPos].position;
                    playerCTRL.transform.position = targetPosition;
                    Debug.Log($"✅ Spieler {playerData.PlayerName} (ID: {playerData.PlayerID}) auf Position {playerData.BoardPosition} gesetzt.");
                }
                else
                {
                    Debug.LogWarning($"⚠️ Spieler {playerData.PlayerName}: Position {playerCTRL.currentPos} außerhalb des Routes ({playerCTRL.route.childNodeList.Count} Felder)!");
                }
            }
            else
            {
                Debug.LogWarning($"⚠️ PlayerCTRL für Spieler {playerData.PlayerName} (ID: {playerData.PlayerID}) nicht gefunden oder Route nicht zugewiesen!");
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
        boardLayout[10] = FieldType.Action;
        boardLayout[13] = FieldType.Action;
        boardLayout[20] = FieldType.Action;
        boardLayout[27] = FieldType.Action;
        boardLayout[37] = FieldType.Action;

        boardLayout[7] = FieldType.Bank;
        boardLayout[23] = FieldType.Bank;

        // boardLayout[7] = FieldType.Action;
        // boardLayout[23] = FieldType.Action;
    }
    
    
     public List<CompanyField> GetCompanyFields()
    {
        return companyFields;
    }
}