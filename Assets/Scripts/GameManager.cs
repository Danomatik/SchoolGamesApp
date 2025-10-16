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

    public void AddMoney(int playerID, int amount)
    {
        PlayerData targetPlayer = CurrentGame.AllPlayers.Find(p => p.PlayerID == playerID);
        if (targetPlayer != null)
        {
            targetPlayer.Money += amount;
            Debug.Log($"Spieler {playerID} erhält {amount}€. Neuer Stand: {targetPlayer.Money}€");
        }
    }
    
    public bool RemoveMoney(int playerID, int amount)
    {
        PlayerData targetPlayer = CurrentGame.AllPlayers.Find(p => p.PlayerID == playerID);
        if (targetPlayer != null && targetPlayer.Money >= amount)
        {
            targetPlayer.Money -= amount;
            Debug.Log($"Spieler {playerID} bezahlt {amount}€. Neuer Stand: {targetPlayer.Money}€");
            return true;
        }
        Debug.LogWarning($"Spieler {playerID} hat zu wenig Geld, um {amount}€ zu bezahlen!");
        return false;
    }
    
    public void TestCurrencySystem()
    {
    Debug.Log("--- STARTE WÄHRUNGSSYSTEM-TEST ---");
    Debug.Log($"Anfangsgeld: {CurrentGame.AllPlayers[0].Money}€"); // Sollte 3000€ sein

    // Test 1: Geld hinzufügen
    Debug.Log("Test 1: Füge 400€ hinzu.");
    AddMoney(1, 400);
    Debug.Log($"Neuer Stand nach Hinzufügen: {CurrentGame.AllPlayers[0].Money}€"); // Sollte 3400€ sein

    // Test 2: Erfolgreich Geld abziehen
    Debug.Log("Test 2: Ziehe 500€ ab.");
    bool kaufErfolgreich = RemoveMoney(1, 500);
    Debug.Log($"Kauf war erfolgreich: {kaufErfolgreich}. Neuer Stand: {CurrentGame.AllPlayers[0].Money}€"); // Sollte 2900€ sein

    // Test 3: Fehlgeschlagenes Abziehen
    Debug.Log("Test 3: Versuche 5000€ abzuziehen.");
    bool kaufFehlgeschlagen = RemoveMoney(1, 5000);
    Debug.Log($"Kauf war erfolgreich: {kaufFehlgeschlagen}. Finaler Stand: {CurrentGame.AllPlayers[0].Money}€"); // Sollte 2900€ bleiben

    Debug.Log("--- WÄHRUNGSSYSTEM-TEST BEENDET ---");
}
}
