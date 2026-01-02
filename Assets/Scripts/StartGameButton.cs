using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Script für den "Spiel starten" Button in Demo 3 Scene
/// Lädt die MainScene und startet das Spiel mit den konfigurierten Spielern
/// </summary>
public class StartGameButton : MonoBehaviour
{
    [Header("Scene Settings")]
    [Tooltip("Scene die geladen wird (normalerweise MainScene)")]
    public string gameScene = "MainScene";

    [Header("Validation")]
    [Tooltip("Prüft ob mindestens 2 Spieler konfiguriert sind")]
    public bool requireMinimumPlayers = true;
    public int minimumPlayers = 2;

    /// <summary>
    /// Wird vom Button aufgerufen - startet das Spiel
    /// </summary>
    public void StartGame()
    {
        // Prüfe ob Spielerdaten vorhanden sind
        PlayerSetupManager setupManager = FindFirstObjectByType<PlayerSetupManager>();
        if (setupManager == null)
        {
            Debug.LogError("[StartGameButton] PlayerSetupManager nicht gefunden!");
            // Erstelle einen temporären Manager
            GameObject tempObj = new GameObject("TempPlayerSetupManager");
            setupManager = tempObj.AddComponent<PlayerSetupManager>();
        }

        // Prüfe ob genug Spieler konfiguriert sind
        if (requireMinimumPlayers)
        {
            int playerCount = setupManager.GetPlayerCount();
            if (playerCount < minimumPlayers)
            {
                Debug.LogWarning($"[StartGameButton] Zu wenige Spieler! Benötigt: {minimumPlayers}, Vorhanden: {playerCount}");
                // Optional: Zeige Fehlermeldung im UI
                return;
            }
        }

        // Stelle sicher, dass kein gespeichertes Spiel geladen wird (neues Spiel starten)
        PlayerPrefs.SetInt("LoadSavedGame", 0);
        PlayerPrefs.Save();

        Debug.Log($"[StartGameButton] Starte neues Spiel - Lade Scene: {gameScene}");
        Debug.Log($"[StartGameButton] Spieleranzahl: {setupManager.GetPlayerCount()}");

        // Lade die Game-Scene
        if (!string.IsNullOrEmpty(gameScene))
        {
            SceneManager.LoadScene(gameScene);
        }
        else
        {
            Debug.LogError("[StartGameButton] gameScene ist nicht gesetzt!");
        }
    }
}

