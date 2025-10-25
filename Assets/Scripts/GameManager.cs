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

    public float smoothSpeed = 5f;
    public float defaultLens = 3.55f;

    public float targetOrthoSize;
    private float currentOrthoSize;

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

        if (cam == null)
            cam = GetComponent<CinemachineCamera>();

        currentOrthoSize = cam.Lens.OrthographicSize;
    }
    
    private void LateUpdate() {
        currentOrthoSize = Mathf.Lerp(currentOrthoSize, targetOrthoSize, Time.deltaTime * smoothSpeed);
        cam.Lens.OrthographicSize = currentOrthoSize;
    }

    public void SetTargetSize(float newSize)
    {
        targetOrthoSize = newSize;
    }


    private void InitializeBoardLayout()
    {
        // Set all fields to Bank by default
        for (int i = 0; i < boardLayout.Length; i++)
        {
            boardLayout[i] = FieldType.Bank;
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
        return CurrentGame.AllPlayers[CurrentGame.CurrentPlayerTurnID];
    }

    public void EndTurn()
    {
        CurrentGame.CurrentPlayerTurnID++;
        if (CurrentGame.CurrentPlayerTurnID >= CurrentGame.AllPlayers.Count)
            CurrentGame.CurrentPlayerTurnID = 0;

        UpdateAgentPriorities();
        Debug.Log($"Zug beendet. Spieler {GetCurrentPlayer().PlayerID} ist jetzt an der Reihe.");
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

            SetTargetSize(defaultLens);
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
                    }
                    break;
                    
                case FieldType.Bank:
                    Debug.Log("Player landed on Bank field!");
                    if (bankCardManager != null)
                    {
                        bankCardManager.PrintRandomBankCard();
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
        SetTargetSize(diceLensSize);


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
}