using NUnit.Framework;
using System.Collections.Generic;

/// <summary>
/// EditMode Unit Tests für PlayerData Klasse
/// Diese Tests laufen ohne Unity Runtime (schneller)
/// </summary>
public class PlayerDataTests
{
    [Test]
    public void PlayerData_Initialization_DefaultValues()
    {
        // Arrange & Act
        PlayerData player = new PlayerData();

        // Assert
        Assert.AreEqual(0, player.PlayerID, "PlayerID sollte 0 sein");
        Assert.AreEqual(0, player.Money, "Money sollte 0 sein");
        Assert.AreEqual(0, player.BoardPosition, "BoardPosition sollte 0 sein");
        Assert.IsNull(player.PlayerName, "PlayerName sollte null sein");
        Assert.IsFalse(player.hasToSkip, "hasToSkip sollte false sein");
        Assert.IsFalse(player.isEliminated, "isEliminated sollte false sein");
        Assert.IsNotNull(player.companies, "companies Liste sollte automatisch initialisiert sein");
        Assert.AreEqual(0, player.companies.Count, "companies Liste sollte leer sein");
    }

    [Test]
    public void PlayerData_Initialization_WithValues()
    {
        // Arrange & Act
        PlayerData player = new PlayerData
        {
            PlayerID = 1,
            Money = 1500,
            BoardPosition = 5,
            PlayerName = "Test Spieler",
            hasToSkip = true,
            isEliminated = false
        };
        player.companies = new List<int> { 1, 2, 3 };

        // Assert
        Assert.AreEqual(1, player.PlayerID);
        Assert.AreEqual(1500, player.Money);
        Assert.AreEqual(5, player.BoardPosition);
        Assert.AreEqual("Test Spieler", player.PlayerName);
        Assert.IsTrue(player.hasToSkip);
        Assert.IsFalse(player.isEliminated);
        Assert.AreEqual(3, player.companies.Count);
    }

    [Test]
    public void PlayerData_Companies_AddRemove()
    {
        // Arrange
        PlayerData player = new PlayerData();
        // companies wird automatisch initialisiert (siehe PlayerData.cs)

        // Act & Assert: Add
        player.companies.Add(1);
        player.companies.Add(2);
        Assert.AreEqual(2, player.companies.Count);
        Assert.Contains(1, player.companies);
        Assert.Contains(2, player.companies);

        // Act & Assert: Remove
        player.companies.Remove(1);
        Assert.AreEqual(1, player.companies.Count);
        Assert.IsFalse(player.companies.Contains(1));
        Assert.Contains(2, player.companies);
    }

    [Test]
    public void PlayerData_Elimination_State()
    {
        // Arrange
        PlayerData player = new PlayerData
        {
            Money = 100,
            companies = new List<int> { 1, 2 }
        };

        // Act
        player.isEliminated = true;

        // Assert
        Assert.IsTrue(player.isEliminated);
        // Geld und Unternehmen bleiben erhalten (werden extern verwaltet)
        Assert.AreEqual(100, player.Money);
        Assert.AreEqual(2, player.companies.Count);
    }
}
