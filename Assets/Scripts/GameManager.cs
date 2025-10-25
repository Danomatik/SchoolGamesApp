using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.AI;

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
    [SerializeField]
    private BankCardManager bankCardManager;

    [Header("Camera")]
    public CinemachineCamera cam;
    public float defaultLens = 3.55f;

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

    // ============================================================
    // 🏁 UNITY METHODS
    // ============================================================
    void Start()
    {
        CurrentGame = new GameState();

        // Initialize board layout - all fields are Company by default
        InitializeBoardLayout();

        // Spieler 1
        PlayerData humanPlayer = new PlayerData { PlayerID = 1, Money = 2500, BoardPosition = 0 };
        CurrentGame.AllPlayers.Add(humanPlayer);

        // Spieler 2
        PlayerData botPlayer1 = new PlayerData { PlayerID = 2, Money = 2500, BoardPosition = 0 };
        CurrentGame.AllPlayers.Add(botPlayer1);

        Debug.Log("Neues Spiel mit 2 Spielern gestartet!");
        Debug.Log($"Spieler 1 hat {humanPlayer.Money} € Startgeld");

        TestCurrencySystem();
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
        CurrentGame.CurrentPlayerTurnID++;
        if (CurrentGame.CurrentPlayerTurnID >= CurrentGame.AllPlayers.Count)
            CurrentGame.CurrentPlayerTurnID = 0;

        UpdateAgentPriorities();
        PlayerData nextPlayer = GetCurrentPlayer();
        if (nextPlayer != null)
        {
            Debug.Log($"Zug beendet. Spieler {nextPlayer.PlayerID} ist jetzt an der Reihe.");
        }
        else
        {
            Debug.LogError("EndTurn: Could not get next player!");
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
                    Debug.Log("Player landed on Company field!");
                    if (questionManager != null)
                    {
                        questionManager.PrintRandomQuestion();
                        questionManager.ShowQuestionInUI();
                    }
                    break;
                    
                case FieldType.Bank:
                    Debug.Log("Player landed on Bank field!");
                    PlayerData currentPlayer = GetCurrentPlayer();
                    if (currentPlayer != null)
                    {
                        Debug.Log($"Current player before bank card: {currentPlayer.PlayerID}");
                        if (bankCardManager != null)
                        {
                            bankCardManager.ExecuteRandomBankCard();
                            PlayerData playerAfter = GetCurrentPlayer();
                            if (playerAfter != null)
                            {
                                Debug.Log($"Current player after bank card: {playerAfter.PlayerID}");
                            }
                            // Check if the bank card action allows rolling again
                            if (bankCardManager.ShouldRollAgain())
                            {
                                // Don't end turn, player can roll again
                                isTurnInProgress = false;
                                return;
                            }
                        }
                    }
                    else
                    {
                        Debug.LogError("Bank field: Current player is null!");
                    }
                    break;
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
        Debug.Log($"Dice rolled: {rollValue}");

        // Return camera to player

        TakeTurn();

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
        Debug.Log($"Bank Card Action: Player {GetCurrentPlayer().PlayerID} skips their turn");
        EndTurn();
    }

    public void RollAgain()
    {
        Debug.Log($"Bank Card Action: Player {GetCurrentPlayer().PlayerID} gets to roll again!");
        
        // Don't end the turn, let the player roll again
        isTurnInProgress = false;
        
        // The player can now roll again by pressing Space or using the dice system
        Debug.Log("Player can now roll again!");
    }
}