using System;
using UnityEngine;

public class StartField : MonoBehaviour
{
    [SerializeField]
    private GameManager gameManager;

    void OnTriggerEnter(Collider other)
    {
        PlayerCTRL triggeredPiece = other.GetComponentInParent<PlayerCTRL>();
        if (triggeredPiece == null) return;

        // Prüfe ob wir gerade ein Spiel laden - dann keinen Bonus geben
        if (IsLoadingGame())
        {
            Debug.Log($"[StartField] Ignoriere Start-Bonus für Spieler {triggeredPiece.PlayerID} (Spiel wird geladen)");
            return;
        }

        gameManager.moneyManager.AddMoney(triggeredPiece.PlayerID, 400); 
        
        Debug.Log($"Spieler {triggeredPiece.PlayerID} überquert das Startfeld und erhält Geld.");
    }

    /// <summary>
    /// Prüft ob gerade ein Spiel geladen wird
    /// </summary>
    private bool IsLoadingGame()
    {
        // Prüfe ob GameInitiator existiert und ob gerade geladen wird
        GameInitiator gameInitiator = FindFirstObjectByType<GameInitiator>();
        if (gameInitiator == null) return false;

        // Prüfe ob ein Save-Flag gesetzt ist (wird beim Laden gesetzt)
        int loadFlag = PlayerPrefs.GetInt("LoadSavedGame", 0);
        if (loadFlag == 1) return true;

        // Alternative: Prüfe ob GameSaveManager existiert und ob gerade geladen wird
        // Wir können auch einen statischen Flag verwenden
        return GameSaveManager.IsLoadingGame;
    }
}