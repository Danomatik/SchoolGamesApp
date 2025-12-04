using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Serializable data structure that holds all game state information
/// </summary>
[Serializable]
public class GameSaveData
{
    // Game state
    public List<PlayerSaveData> players = new List<PlayerSaveData>();
    public int currentPlayerTurnID;
    
    // Company fields state
    public List<CompanyField> companyFields = new List<CompanyField>();
    
    // Timestamp for when game was saved
    public string saveTimestamp;
}

/// <summary>
/// Serializable player data for saving
/// </summary>
[Serializable]
public class PlayerSaveData
{
    public int PlayerID;
    public int Money;
    public int BoardPosition;
    public string PlayerName;
    public bool hasToSkip;
    public List<int> companies = new List<int>();
}


