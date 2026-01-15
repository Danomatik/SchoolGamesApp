using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Verwaltet die Spieler-Einstellungen (Anzahl, Namen) und speichert sie in PlayerPrefs
/// Wird in Demo 3 Scene verwendet
/// </summary>
public class PlayerSetupManager : MonoBehaviour
{
    private const string PLAYER_COUNT_KEY = "PlayerCount";
    private const string PLAYER_NAME_PREFIX = "PlayerName_";
    private const string GAME_DURATION_KEY = "GameDuration";

    /// <summary>
    /// Speichert die Spieleranzahl
    /// </summary>
    public void SetPlayerCount(int count)
    {
        PlayerPrefs.SetInt(PLAYER_COUNT_KEY, count);
        PlayerPrefs.Save();
        Debug.Log($"[PlayerSetupManager] Spieleranzahl gespeichert: {count}");
    }

    /// <summary>
    /// Gibt die gespeicherte Spieleranzahl zurück (Standard: 6)
    /// </summary>
    public int GetPlayerCount()
    {
        return PlayerPrefs.GetInt(PLAYER_COUNT_KEY, 0);
    }

    /// <summary>
    /// Speichert den Namen eines Spielers
    /// </summary>
    public void SetPlayerName(int playerID, string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            name = $"Spieler {playerID}"; // Fallback-Name
        }
        PlayerPrefs.SetString($"{PLAYER_NAME_PREFIX}{playerID}", name);
        PlayerPrefs.Save();
        Debug.Log($"[PlayerSetupManager] Spieler {playerID} Name gespeichert: {name}");
    }

    /// <summary>
    /// Gibt den gespeicherten Namen eines Spielers zurück
    /// </summary>
    public string GetPlayerName(int playerID)
    {
        string key = $"{PLAYER_NAME_PREFIX}{playerID}";
        if (PlayerPrefs.HasKey(key))
        {
            return PlayerPrefs.GetString(key);
        }
        return $"Spieler {playerID}"; // Fallback-Name
    }

    /// <summary>
    /// Gibt alle gespeicherten Spielernamen zurück
    /// </summary>
    public List<string> GetAllPlayerNames()
    {
        List<string> names = new List<string>();
        int count = GetPlayerCount();
        
        for (int i = 1; i <= count; i++)
        {
            names.Add(GetPlayerName(i));
        }
        
        return names;
    }

    /// <summary>
    /// Löscht alle gespeicherten Spielerdaten
    /// </summary>
    public void ClearPlayerData()
    {
        PlayerPrefs.DeleteKey(PLAYER_COUNT_KEY);
        for (int i = 1; i <= 6; i++)
        {
            PlayerPrefs.DeleteKey($"{PLAYER_NAME_PREFIX}{i}");
        }
        PlayerPrefs.Save();
        Debug.Log("[PlayerSetupManager] Alle Spielerdaten gelöscht");
    }

    /// <summary>
    /// Prüft ob Spielerdaten vorhanden sind
    /// </summary>
    public bool HasPlayerData()
    {
        return PlayerPrefs.HasKey(PLAYER_COUNT_KEY);
    }

    /// <summary>
    /// Speichert die Spiel Dauer (in Minuten)
    /// </summary>
    public void SetGameDuration(float durationInMinutes)
    {
        PlayerPrefs.SetFloat(GAME_DURATION_KEY, durationInMinutes);
        PlayerPrefs.Save();
        Debug.Log($"[PlayerSetupManager] Spiel Dauer gespeichert: {durationInMinutes} Minuten");
    }

    /// <summary>
    /// Gibt die gespeicherte Spiel Dauer zurück (Standard: 5 Minuten, Max: 30 Minuten)
    /// </summary>
    public float GetGameDuration()
    {
        float duration = PlayerPrefs.GetFloat(GAME_DURATION_KEY, 5f); // Standard: 5 Minuten
        return Mathf.Clamp(duration, 0f, 30f); // Max: 30 Minuten
    }
}

