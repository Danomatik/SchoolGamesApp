using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public GameState CurrentGame;
    public List<PlayerCTRL> players;
    private bool isTurnInProgress = false;

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
        if (isTurnInProgress)
            return;

        isTurnInProgress = true;

        int diceRoll = Random.Range(1, 7);
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
        // Hier kommt später die Logik rein, um die Aktion des Feldes auszulösen
        // For now, we just end the turn
        EndTurn();

        isTurnInProgress = false;
    }

    public void TestCurrencySystem()
    {
        Debug.Log("--- STARTE WÄHRUNGSSYSTEM-TEST ---");
        Debug.Log($"Anfangsgeld: {GetCurrentPlayer().Money}€");

        // Test 1: Geld hinzufügen
        AddMoney(400); // Ruft jetzt die neue Funktion auf

        // Test 2: Erfolgreich Geld abziehen
        RemoveMoney(500); // Ruft jetzt die neue Funktion auf

        // Test 3: Fehlgeschlagenes Abziehen
        RemoveMoney(5000); // Ruft jetzt die neue Funktion auf

        Debug.Log($"--- TEST BEENDET --- Finaler Kontostand: {GetCurrentPlayer().Money}€");
    }
}
