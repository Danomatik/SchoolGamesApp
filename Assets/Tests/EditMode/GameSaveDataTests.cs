using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// EditMode Unit Tests für GameSaveData Datenstrukturen
/// Testet die Serialisierung und Datenintegrität
/// </summary>
public class GameSaveDataTests
{
    [Test]
    public void GameSaveData_Initialization_DefaultValues()
    {
        // Arrange & Act
        GameSaveData saveData = new GameSaveData();

        // Assert
        Assert.IsNotNull(saveData.players, "players Liste sollte nicht null sein");
        Assert.AreEqual(0, saveData.players.Count, "players Liste sollte leer sein");
        Assert.AreEqual(0, saveData.currentPlayerTurnID, "currentPlayerTurnID sollte 0 sein");
        Assert.IsNotNull(saveData.companyFields, "companyFields Liste sollte nicht null sein");
        Assert.AreEqual(0, saveData.companyFields.Count, "companyFields Liste sollte leer sein");
        Assert.IsNull(saveData.saveTimestamp, "saveTimestamp sollte null sein");
    }

    [Test]
    public void PlayerSaveData_Initialization_DefaultValues()
    {
        // Arrange & Act
        PlayerSaveData playerData = new PlayerSaveData();

        // Assert
        Assert.AreEqual(0, playerData.PlayerID);
        Assert.AreEqual(0, playerData.Money);
        Assert.AreEqual(0, playerData.BoardPosition);
        Assert.IsNull(playerData.PlayerName);
        Assert.IsFalse(playerData.hasToSkip);
        Assert.IsNotNull(playerData.companies, "companies Liste sollte nicht null sein");
        Assert.AreEqual(0, playerData.companies.Count, "companies Liste sollte leer sein");
    }

    [Test]
    public void GameSaveData_AddPlayers_StoresCorrectly()
    {
        // Arrange
        GameSaveData saveData = new GameSaveData();
        PlayerSaveData player1 = new PlayerSaveData
        {
            PlayerID = 1,
            Money = 1500,
            BoardPosition = 5,
            PlayerName = "Spieler 1",
            hasToSkip = false
        };
        player1.companies.Add(1);
        player1.companies.Add(2);

        PlayerSaveData player2 = new PlayerSaveData
        {
            PlayerID = 2,
            Money = 2000,
            BoardPosition = 10,
            PlayerName = "Spieler 2",
            hasToSkip = true
        };

        // Act
        saveData.players.Add(player1);
        saveData.players.Add(player2);
        saveData.currentPlayerTurnID = 1;
        saveData.saveTimestamp = "2024-01-01 12:00:00";

        // Assert
        Assert.AreEqual(2, saveData.players.Count);
        Assert.AreEqual(1, saveData.currentPlayerTurnID);
        Assert.AreEqual("2024-01-01 12:00:00", saveData.saveTimestamp);
        Assert.AreEqual(1, saveData.players[0].PlayerID);
        Assert.AreEqual(1500, saveData.players[0].Money);
        Assert.AreEqual(2, saveData.players[0].companies.Count);
        Assert.AreEqual(2, saveData.players[1].PlayerID);
        Assert.IsTrue(saveData.players[1].hasToSkip);
    }

    [Test]
    public void GameSaveData_AddCompanyFields_StoresCorrectly()
    {
        // Arrange
        GameSaveData saveData = new GameSaveData();
        CompanyField field1 = new CompanyField
        {
            fieldIndex = 5,
            companyID = 1,
            ownerID = 1,
            level = CompanyLevel.Founded
        };
        CompanyField field2 = new CompanyField
        {
            fieldIndex = 10,
            companyID = 2,
            ownerID = 2,
            level = CompanyLevel.Invested
        };

        // Act
        saveData.companyFields.Add(field1);
        saveData.companyFields.Add(field2);

        // Assert
        Assert.AreEqual(2, saveData.companyFields.Count);
        Assert.AreEqual(5, saveData.companyFields[0].fieldIndex);
        Assert.AreEqual(CompanyLevel.Founded, saveData.companyFields[0].level);
        Assert.AreEqual(10, saveData.companyFields[1].fieldIndex);
        Assert.AreEqual(CompanyLevel.Invested, saveData.companyFields[1].level);
    }

    [Test]
    public void GameSaveData_Serialization_WorksWithJsonUtility()
    {
        // Arrange
        GameSaveData originalData = new GameSaveData();
        originalData.currentPlayerTurnID = 2;
        originalData.saveTimestamp = "2024-01-01 12:00:00";

        PlayerSaveData player = new PlayerSaveData
        {
            PlayerID = 1,
            Money = 1000,
            BoardPosition = 5,
            PlayerName = "Test Spieler",
            hasToSkip = false
        };
        player.companies.Add(1);
        player.companies.Add(2);
        originalData.players.Add(player);

        CompanyField field = new CompanyField
        {
            fieldIndex = 5,
            companyID = 1,
            ownerID = 1,
            level = CompanyLevel.Founded
        };
        originalData.companyFields.Add(field);

        // Act: Serialize
        string json = JsonUtility.ToJson(originalData, true);
        Assert.IsNotNull(json, "JSON sollte nicht null sein");
        Assert.IsNotEmpty(json, "JSON sollte nicht leer sein");

        // Act: Deserialize
        GameSaveData loadedData = JsonUtility.FromJson<GameSaveData>(json);

        // Assert
        Assert.IsNotNull(loadedData, "Geladene Daten sollten nicht null sein");
        Assert.AreEqual(originalData.currentPlayerTurnID, loadedData.currentPlayerTurnID);
        Assert.AreEqual(originalData.saveTimestamp, loadedData.saveTimestamp);
        Assert.AreEqual(1, loadedData.players.Count);
        Assert.AreEqual(1, loadedData.players[0].PlayerID);
        Assert.AreEqual(1000, loadedData.players[0].Money);
        Assert.AreEqual(2, loadedData.players[0].companies.Count);
        Assert.AreEqual(1, loadedData.companyFields.Count);
        Assert.AreEqual(5, loadedData.companyFields[0].fieldIndex);
    }

    [Test]
    public void PlayerSaveData_Companies_AddRemove()
    {
        // Arrange
        PlayerSaveData player = new PlayerSaveData();

        // Act & Assert: Add
        player.companies.Add(1);
        player.companies.Add(2);
        player.companies.Add(3);
        Assert.AreEqual(3, player.companies.Count);
        Assert.Contains(1, player.companies);
        Assert.Contains(2, player.companies);
        Assert.Contains(3, player.companies);

        // Act & Assert: Remove
        player.companies.Remove(2);
        Assert.AreEqual(2, player.companies.Count);
        Assert.IsFalse(player.companies.Contains(2));
    }
}
