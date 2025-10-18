using UnityEngine;

public class GameManager : MonoBehaviour
{
    public GameState CurrentGame;

    void Start()
    {
        CurrentGame = new GameState();

        PlayerData humanPlayer = new PlayerData { PlayerID = 1, Money = 3000, BoardPosition = 0 };
        CurrentGame.AllPlayers.Add(humanPlayer);

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
    
// In GameManager.cs
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
