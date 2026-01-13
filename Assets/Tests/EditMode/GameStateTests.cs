using NUnit.Framework;
using System.Collections.Generic;

/// <summary>
/// EditMode Unit Tests für GameState Klasse
/// </summary>
public class GameStateTests
{
    [Test]
    public void GameState_Initialization_DefaultValues()
    {
        // Arrange & Act
        GameState gameState = new GameState();

        // Assert
        Assert.IsNotNull(gameState.AllPlayers, "AllPlayers Liste sollte nicht null sein");
        Assert.AreEqual(0, gameState.AllPlayers.Count, "AllPlayers sollte leer sein");
        Assert.AreEqual(0, gameState.CurrentPlayerTurnID, "CurrentPlayerTurnID sollte 0 sein");
    }

    [Test]
    public void GameState_AddPlayers()
    {
        // Arrange
        GameState gameState = new GameState();
        PlayerData player1 = new PlayerData { PlayerID = 1, PlayerName = "Spieler 1" };
        PlayerData player2 = new PlayerData { PlayerID = 2, PlayerName = "Spieler 2" };

        // Act
        gameState.AllPlayers.Add(player1);
        gameState.AllPlayers.Add(player2);

        // Assert
        Assert.AreEqual(2, gameState.AllPlayers.Count);
        Assert.AreEqual(player1, gameState.AllPlayers[0]);
        Assert.AreEqual(player2, gameState.AllPlayers[1]);
    }

    [Test]
    public void GameState_CurrentPlayerTurnID_Changes()
    {
        // Arrange
        GameState gameState = new GameState();
        gameState.AllPlayers.Add(new PlayerData { PlayerID = 1 });
        gameState.AllPlayers.Add(new PlayerData { PlayerID = 2 });
        gameState.AllPlayers.Add(new PlayerData { PlayerID = 3 });

        // Act & Assert
        gameState.CurrentPlayerTurnID = 0;
        Assert.AreEqual(0, gameState.CurrentPlayerTurnID);

        gameState.CurrentPlayerTurnID = 1;
        Assert.AreEqual(1, gameState.CurrentPlayerTurnID);

        gameState.CurrentPlayerTurnID = 2;
        Assert.AreEqual(2, gameState.CurrentPlayerTurnID);
    }

    [Test]
    public void GameState_FindPlayerByID()
    {
        // Arrange
        GameState gameState = new GameState();
        PlayerData player1 = new PlayerData { PlayerID = 1, PlayerName = "Spieler 1" };
        PlayerData player2 = new PlayerData { PlayerID = 2, PlayerName = "Spieler 2" };
        gameState.AllPlayers.Add(player1);
        gameState.AllPlayers.Add(player2);

        // Act
        PlayerData found = gameState.AllPlayers.Find(p => p.PlayerID == 2);

        // Assert
        Assert.IsNotNull(found);
        Assert.AreEqual(player2, found);
        Assert.AreEqual("Spieler 2", found.PlayerName);
    }
}
