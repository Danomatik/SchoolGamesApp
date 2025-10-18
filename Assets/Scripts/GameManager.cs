using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class GameManager : MonoBehaviour
{
    public GameState CurrentGame;
    public List<PlayerCTRL> players;
    private bool isTurnInProgress = false;
    public FieldType[] boardLayout = new FieldType[40];

    void Start()
    {
        CurrentGame = new GameState();

        // SPieler 1
        PlayerData humanPlayer = new PlayerData { PlayerID = 1, Money = 2500, BoardPosition = 0 };
        CurrentGame.AllPlayers.Add(humanPlayer);

        //Spieler 2
        PlayerData botPlayer1 = new PlayerData { PlayerID = 2, Money = 2500, BoardPosition = 0 };
        CurrentGame.AllPlayers.Add(botPlayer1);

        Debug.Log("Neues Spiel mit 2 Spielern gestartet!");
        Debug.Log($"Neues Spiel gestartet! Spieler 1 hat {humanPlayer.Money} €");

        TestCurrencySystem();
    }

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
            TakeTurn();
        }
    }

    public void TakeTurn()
    {
        if (isTurnInProgress) return;
        isTurnInProgress = true;

        UpdateAgentPriorities();

        int diceRoll = Random.Range(2, 13);
        Debug.Log($"Player {GetCurrentPlayer().PlayerID} rolled a {diceRoll}!");

        //Find the Correct player in the scene
        PlayerCTRL activePlayer = players.Find(p => p.PlayerID == GetCurrentPlayer().PlayerID);

        if (activePlayer != null)
        {
            activePlayer.StartMove(diceRoll);
        }
    }

    public void PlayerFinishedMoving(int finalPosition)
    {
        Debug.Log($"GameManager knows the player is at position {finalPosition}. Triggering field logic now.");

        FieldType type = boardLayout[finalPosition];

        switch (type)
        {
            case FieldType.Start:
                Debug.Log("Feld-Logik: Startfeld!");
                break;
            case FieldType.Company:
                Debug.Log("Feld-Logik: Unternehmensfeld!");
                // Rufe hier die Logik für Unternehmensfelder auf.
                // LandedOnCompany(finalPosition); 
                break;
            case FieldType.Bank:
                Debug.Log("Feld-Logik: Bankfeld!");
                break;
            case FieldType.Action:
                Debug.Log("Feld-Logik: Aktionsfeld!");
                break;
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
