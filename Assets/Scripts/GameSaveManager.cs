using UnityEngine;
using System.IO;
using System;
using System.Collections.Generic;

/// <summary>
/// Manages saving and loading game state
/// </summary>
public class GameSaveManager : MonoBehaviour
{
    private const string SAVE_FILE_NAME = "game_save.json";
    private string SaveFilePath => Path.Combine(Application.persistentDataPath, SAVE_FILE_NAME);
    
    // Static flag to track if game is currently being loaded
    public static bool IsLoadingGame { get; private set; } = false;

    /// <summary>
    /// Saves the current game state
    /// </summary>
    public bool SaveGame(GameInitiator gameInitiator)
    {
        if (gameInitiator == null || gameInitiator.CurrentGame == null)
        {
            Debug.LogError("[GameSaveManager] Cannot save: GameInitiator or CurrentGame is null!");
            return false;
        }

        try
        {
            GameSaveData saveData = new GameSaveData
            {
                currentPlayerTurnID = gameInitiator.CurrentGame.CurrentPlayerTurnID,
                saveTimestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };

            // Get GameManager to access PlayerCTRL objects
            GameManager gameManager = FindFirstObjectByType<GameManager>();

            // Save all players
            foreach (var player in gameInitiator.CurrentGame.AllPlayers)
            {
                // Sync BoardPosition from PlayerCTRL.currentPos if available
                int boardPosition = player.BoardPosition;
                if (gameManager != null)
                {
                    var playerCTRL = gameManager.players?.Find(p => p.PlayerID == player.PlayerID);
                    if (playerCTRL != null)
                    {
                        boardPosition = playerCTRL.currentPos;
                        // Also update PlayerData to keep it in sync
                        player.BoardPosition = boardPosition;
                    }
                }

                saveData.players.Add(new PlayerSaveData
                {
                    PlayerID = player.PlayerID,
                    Money = player.Money,
                    BoardPosition = boardPosition,
                    PlayerName = player.PlayerName,
                    hasToSkip = player.hasToSkip,
                    companies = new List<int>(player.companies) // Copy list
                });
            }

            // Save all company fields
            saveData.companyFields = new System.Collections.Generic.List<CompanyField>(gameInitiator.GetCompanyFields());

            // Serialize to JSON
            string json = JsonUtility.ToJson(saveData, true);
            
            // Write to file
            File.WriteAllText(SaveFilePath, json);
            
            Debug.Log($"[GameSaveManager] ✅ Game saved successfully to: {SaveFilePath}");
            Debug.Log($"[GameSaveManager] Saved {saveData.players.Count} players and {saveData.companyFields.Count} company fields");
            
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[GameSaveManager] ❌ Failed to save game: {e.Message}");
            return false;
        }
    }

    /// <summary>
    /// Loads the saved game state
    /// </summary>
    public GameSaveData LoadGame()
    {
        if (!File.Exists(SaveFilePath))
        {
            Debug.Log("[GameSaveManager] No save file found. Starting new game.");
            IsLoadingGame = false;
            return null;
        }

        try
        {
            // Set loading flag
            IsLoadingGame = true;
            
            string json = File.ReadAllText(SaveFilePath);
            GameSaveData saveData = JsonUtility.FromJson<GameSaveData>(json);
            
            if (saveData == null)
            {
                Debug.LogError("[GameSaveManager] Failed to parse save file!");
                IsLoadingGame = false;
                return null;
            }

            Debug.Log($"[GameSaveManager] ✅ Game loaded successfully!");
            Debug.Log($"[GameSaveManager] Loaded {saveData.players.Count} players and {saveData.companyFields.Count} company fields");
            Debug.Log($"[GameSaveManager] Save timestamp: {saveData.saveTimestamp}");
            
            // Reset loading flag after a short delay to allow position updates to complete
            StartCoroutine(ResetLoadingFlagAfterDelay());
            
            return saveData;
        }
        catch (Exception e)
        {
            Debug.LogError($"[GameSaveManager] ❌ Failed to load game: {e.Message}");
            IsLoadingGame = false;
            return null;
        }
    }

    /// <summary>
    /// Resets the loading flag after a delay to allow all position updates to complete
    /// </summary>
    private System.Collections.IEnumerator ResetLoadingFlagAfterDelay()
    {
        // Wait for position updates to complete (2 seconds should be enough)
        yield return new WaitForSeconds(2f);
        IsLoadingGame = false;
        Debug.Log("[GameSaveManager] Loading flag reset - Start field bonuses are now active again.");
    }

    /// <summary>
    /// Checks if a save file exists
    /// </summary>
    public bool HasSaveFile()
    {
        return File.Exists(SaveFilePath);
    }

    /// <summary>
    /// Deletes the save file (for starting a new game)
    /// </summary>
    public void DeleteSaveFile()
    {
        if (File.Exists(SaveFilePath))
        {
            try
            {
                File.Delete(SaveFilePath);
                Debug.Log("[GameSaveManager] Save file deleted.");
            }
            catch (Exception e)
            {
                Debug.LogError($"[GameSaveManager] Failed to delete save file: {e.Message}");
            }
        }
    }

    /// <summary>
    /// Gets the save file path (for debugging)
    /// </summary>
    public string GetSaveFilePath()
    {
        return SaveFilePath;
    }
}


