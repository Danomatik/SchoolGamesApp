using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Verwaltet den Spiel-Timer und beendet das Spiel nach der eingestellten Zeit
/// </summary>
public class GameTimerManager : MonoBehaviour
{
    [Header("Timer Settings")]
    [SerializeField] private bool timerEnabled = true;
    
    private GameManager gameManager;
    private PlayerSetupManager setupManager;
    
    private float gameDurationInSeconds;
    private float timeRemaining;
    private bool isTimerRunning = false;
    private bool gameEnded = false;

    private void Awake()
    {
        gameManager = GetComponent<GameManager>();
        if (gameManager == null)
        {
            Debug.LogError("[GameTimerManager] GameManager nicht gefunden!");
        }
    }

    private void Start()
    {
        // Lade Spiel-Dauer aus PlayerPrefs
        setupManager = FindFirstObjectByType<PlayerSetupManager>();
        if (setupManager == null)
        {
            GameObject tempObj = new GameObject("TempPlayerSetupManager");
            setupManager = tempObj.AddComponent<PlayerSetupManager>();
        }

        gameDurationInSeconds = setupManager.GetGameDuration() * 60f; // Konvertiere Minuten zu Sekunden
        timeRemaining = gameDurationInSeconds;
        
        Debug.Log($"[GameTimerManager] Timer initialisiert: {gameDurationInSeconds / 60f} Minuten ({gameDurationInSeconds} Sekunden)");
        
        if (timerEnabled && gameDurationInSeconds > 0)
        {
            StartTimer();
        }
        else if (gameDurationInSeconds <= 0)
        {
            Debug.LogWarning("[GameTimerManager] Timer ist deaktiviert oder Dauer ist 0. Timer wird nicht gestartet.");
        }
    }

    private void Update()
    {
        if (isTimerRunning && !gameEnded && timerEnabled)
        {
            timeRemaining -= Time.deltaTime;
            
            // Update UI
            if (gameManager != null && gameManager.uiManager != null)
            {
                gameManager.uiManager.UpdateTimerDisplay(timeRemaining);
            }
            
            // Prüfe ob Zeit abgelaufen
            if (timeRemaining <= 0)
            {
                timeRemaining = 0;
                EndGame();
            }
        }
    }

    /// <summary>
    /// Startet den Timer
    /// </summary>
    public void StartTimer()
    {
        if (gameDurationInSeconds <= 0)
        {
            Debug.LogWarning("[GameTimerManager] Kann Timer nicht starten: Dauer ist 0 oder negativ.");
            return;
        }
        
        isTimerRunning = true;
        gameEnded = false;
        timeRemaining = gameDurationInSeconds;
        Debug.Log($"[GameTimerManager] Timer gestartet: {gameDurationInSeconds / 60f} Minuten");
    }

    /// <summary>
    /// Stoppt den Timer
    /// </summary>
    public void StopTimer()
    {
        isTimerRunning = false;
        Debug.Log("[GameTimerManager] Timer gestoppt.");
    }

    /// <summary>
    /// Pausiert den Timer
    /// </summary>
    public void PauseTimer()
    {
        isTimerRunning = false;
        Debug.Log("[GameTimerManager] Timer pausiert.");
    }

    /// <summary>
    /// Setzt den Timer fort
    /// </summary>
    public void ResumeTimer()
    {
        if (timeRemaining > 0 && !gameEnded)
        {
            isTimerRunning = true;
            Debug.Log("[GameTimerManager] Timer fortgesetzt.");
        }
    }

    /// <summary>
    /// Beendet das Spiel und bestimmt den Gewinner
    /// </summary>
    private void EndGame()
    {
        if (gameEnded) return;
        
        gameEnded = true;
        isTimerRunning = false;
        
        Debug.Log("[GameTimerManager] ⏰ Spielzeit abgelaufen! Spiel wird beendet...");
        
        // Berechne Vermögenswerte aller Spieler
        List<PlayerRanking> rankings = CalculatePlayerRankings();
        
        // Zeige Game Over UI
        if (gameManager != null && gameManager.uiManager != null)
        {
            gameManager.uiManager.ShowGameOver(rankings);
        }
        
        // Stoppe alle Spiel-Aktivitäten
        if (gameManager != null)
        {
            if (gameManager.diceManager != null && gameManager.diceManager.moveButton != null)
            {
                gameManager.diceManager.moveButton.SetActive(false);
            }
        }
    }

    /// <summary>
    /// Berechnet die Vermögenswerte aller Spieler und sortiert sie
    /// </summary>
    private List<PlayerRanking> CalculatePlayerRankings()
    {
        List<PlayerRanking> rankings = new List<PlayerRanking>();
        
        if (gameManager == null || gameManager.gameInitiator == null)
        {
            Debug.LogError("[GameTimerManager] GameManager oder GameInitiator nicht gefunden!");
            return rankings;
        }
        
        foreach (var player in gameManager.gameInitiator.CurrentGame.AllPlayers)
        {
            int totalAssets = gameManager.moneyManager.CalculateTotalAssets(player);
            
            rankings.Add(new PlayerRanking
            {
                player = player,
                totalAssets = totalAssets,
                money = player.Money,
                companyCount = player.companies.Count
            });
        }
        
        // Sortiere nach Vermögenswert (absteigend)
        rankings = rankings.OrderByDescending(r => r.totalAssets).ToList();
        
        Debug.Log("[GameTimerManager] Spieler-Rankings:");
        for (int i = 0; i < rankings.Count; i++)
        {
            var ranking = rankings[i];
            string playerName = string.IsNullOrEmpty(ranking.player.PlayerName) 
                ? $"Spieler {ranking.player.PlayerID}" 
                : ranking.player.PlayerName;
            Debug.Log($"  {i + 1}. {playerName}: {ranking.totalAssets}€ (Bargeld: {ranking.money}€, Unternehmen: {ranking.companyCount})");
        }
        
        return rankings;
    }

    /// <summary>
    /// Gibt die verbleibende Zeit zurück (in Sekunden)
    /// </summary>
    public float GetTimeRemaining()
    {
        return timeRemaining;
    }

    /// <summary>
    /// Gibt die verbleibende Zeit in Minuten zurück
    /// </summary>
    public float GetTimeRemainingInMinutes()
    {
        return timeRemaining / 60f;
    }

    /// <summary>
    /// Prüft ob das Spiel beendet wurde
    /// </summary>
    public bool IsGameEnded()
    {
        return gameEnded;
    }
}

/// <summary>
/// Datenstruktur für Spieler-Rankings
/// </summary>
[System.Serializable]
public class PlayerRanking
{
    public PlayerData player;
    public int totalAssets;
    public int money;
    public int companyCount;
}

